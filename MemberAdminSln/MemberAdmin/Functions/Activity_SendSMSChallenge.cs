using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using MemberAdmin.Models;
using MemberAdmin.Storage;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Table;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace MemberAdmin.Functions
{
    public class Activity_SendSMSChallenge
    {
        public const string EventNameSms = "SmsResponse";


        [FunctionName("A1_SendSmsChallenge")]
        public static async Task<int> SendSmsChallenge(
            [ActivityTrigger] OrchestrationParameter phone,
            [Microsoft.Azure.WebJobs.Table("approval", "AzureWebJobsStorage")]CloudTable table,
            ILogger log,
            [TwilioSms(AccountSidSetting = "TwilioAccountSid", AuthTokenSetting = "TwilioAuthToken", From = "%TwilioPhoneNumber%")]
            CreateMessageOptions message)
        {
            // Get a random number generator with a random seed (not time-based)
            var rand = new Random(Guid.NewGuid().GetHashCode());
            int challengeCode = rand.Next(10000);

            log.LogInformation($"Sending verification code {challengeCode} to {phone.Payload}.");

            var entity = new ApprovalEntity(phone.OrchestrationId, "NewMember", challengeCode, EventNameSms);
            log.LogInformation(SimpleJson.SimpleJson.SerializeObject(entity));

            await table.AddToTableStorageASync(entity);
            message = new CreateMessageOptions(new PhoneNumber(phone.Payload));

            message.Body = $"Your verification code is {challengeCode:0000}";

            return challengeCode;
        }
    }
}
