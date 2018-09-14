// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MemberAdmin.Models;
using MemberAdmin.Storage;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage.Table;
using RestSharp;
#if NETSTANDARD2_0
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;
#else
using Twilio;
#endif

namespace MemberAdmin
{
    [StorageAccount("AzureWebJobsStorage")]
    public static class PhoneVerification
    {
        private const string _eventNameEMail = "MailResponse";
        private const string _eventNameSms = "SmsResponse";
        private const int _expirationValue = 90;

        [FunctionName("O_EMailPhoneVerification")]
        public static async Task<bool> Run(
            [OrchestrationTrigger] DurableOrchestrationContext context,
            ILogger log)
        {
            string input = context.GetInput<string>();
            string orchestrationId = context.InstanceId;

            if (!context.IsReplaying)
                log.LogInformation($"This ist the input value: {input}");

            var splitString = input.Split(',');
            if (splitString.Count() != 2)
            {
                throw new ArgumentException("To few arguments! Expected is phoneNumber and eMail");
            }
            var phoneNumber = splitString[0];
            var emailAddress = splitString[1];

            if (string.IsNullOrEmpty(phoneNumber))
            {
                throw new ArgumentNullException(
                    nameof(phoneNumber),
                    "A phone number input is required.");
            }

            var phoneParameter = new OrchestrationParameter
            {
                OrchestrationId = orchestrationId,
                Payload = phoneNumber
            };

            var emailParameter = new OrchestrationParameter
            {
                OrchestrationId = orchestrationId,
                Payload = emailAddress
            };

            var codes = new int[2];
            //codes[0] = await context.CallActivityAsync<int>("A1_SendSmsChallenge", phoneParameter);
            codes[1] = await context.CallActivityAsync<int>("A2_SendEmailChallenge", emailParameter);

            using (var timeoutCts = new CancellationTokenSource())
            {
                // The user has 90 seconds to respond with the code they received in the SMS message or an eMail.
                DateTime expiration = context.CurrentUtcDateTime.AddSeconds(_expirationValue);
                Task timeoutTask = context.CreateTimer(expiration, timeoutCts.Token);

                bool authorized = false;
                for (int retryCount = 0; retryCount <= 3; retryCount++)
                {
                    Task<int> smsResponseTask = context.WaitForExternalEvent<int>(_eventNameSms);
                    Task<int> mailResponseTask = context.WaitForExternalEvent<int>(_eventNameEMail);

                    Task winner = await Task.WhenAny(smsResponseTask, mailResponseTask, timeoutTask);
                    if (winner != timeoutTask)
                    {
                        // We got back a response! Compare it to the challenge code.
                        if (codes.Contains(((Task<int>)winner).Result))
                        {
                            authorized = true;
                            break;
                        }
                    }
                    else
                    {
                        // Timeout expired
                        break;
                    }
                }

                if (!timeoutTask.IsCompleted)
                {
                    // All pending timers must be complete or canceled before the function exits.
                    timeoutCts.Cancel();
                }

                return authorized;
            }
        }

        [FunctionName("A1_SendSmsChallenge")]
        public static int SendSmsChallenge(
            [ActivityTrigger] OrchestrationParameter phone,
            ILogger log,
            [TwilioSms(AccountSidSetting = "TwilioAccountSid", AuthTokenSetting = "TwilioAuthToken", From = "%TwilioPhoneNumber%")]
#if NETSTANDARD2_0
                out CreateMessageOptions message)
#else
                out SMSMessage message)
#endif
        {
            // Get a random number generator with a random seed (not time-based)
            var rand = new Random(Guid.NewGuid().GetHashCode());
            int challengeCode = rand.Next(10000);

            log.LogInformation($"Sending verification code {challengeCode} to {phone.Payload}.");

#if NETSTANDARD2_0
            message = new CreateMessageOptions(new PhoneNumber(phone.Payload));
#else
            message = new SMSMessage { To = phone.Payload};
#endif
            message.Body = $"Your verification code is {challengeCode:0000}";

            return challengeCode;
        }

        [FunctionName("A2_SendEmailChallenge")]
        public static async Task<int> SendEMailVerification(
            [ActivityTrigger]OrchestrationParameter eMail,
            [Microsoft.Azure.WebJobs.Table("approval", "AzureWebJobsStorage")]CloudTable table,
            ILogger log, Microsoft.Azure.WebJobs.ExecutionContext context)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(context.FunctionAppDirectory)
                .AddJsonFile("local.settings.json", true, true)
                .AddEnvironmentVariables()
                .Build();

            string uriFlow = config["flowRESTTarget"];
            log.LogInformation(config.ToString());

            // Get a random number generator with a random seed (not time-based)
            var rand = new Random(Guid.NewGuid().GetHashCode());
            int challengeCode = rand.Next(10000);
            var valueObject = new SendMail
            {
                emailadress = eMail.Payload,
                emailSubject = "Confirmation of membership - " + challengeCode,
                emailBody = $"http://{Environment.GetEnvironmentVariable("WEBSITE_HOSTNAME")}/api/approve/{challengeCode}"
            };
            var entity = new ApprovalEntity(eMail.OrchestrationId, "NewMember", challengeCode, _eventNameEMail);
            log.LogInformation(SimpleJson.SimpleJson.SerializeObject(entity));


            var operation = TableOperation.Insert(entity);
            var result = await table.ExecuteAsync(operation);

            if (result.HttpStatusCode >= 300)
            {
                throw new HttpRequestException("HttpStatusCode of Tabel storage is " + result.HttpStatusCode);
            }

            var client = new RestClient(uriFlow);
            var request = new RestRequest(Method.POST);
            //request.AddHeader("Postman-Token", "414d87e7-e9a1-4cc9-9501-c6db9c96f130");
            request.AddHeader("Cache-Control", "no-cache");
            request.AddHeader("Content-Type", "application/json");
            request.AddParameter("undefined", SimpleJson.SimpleJson.SerializeObject(valueObject), ParameterType.RequestBody);
            IRestResponse response = client.Execute(request);

            return challengeCode;
        }
    }
}