﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using RestSharp;
#if NETSTANDARD2_0
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;
#else
using Twilio;
#endif

namespace MemberAdmin
{
	public static class PhoneVerification
	{
		[FunctionName("E4_SmsPhoneVerification")]
		public static async Task<bool> Run(
			[OrchestrationTrigger] DurableOrchestrationContext context, ILogger log)
		{
			string input = context.GetInput<string>();
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

			var codes = new int[2];
			codes[0] = await context.CallActivityAsync<int>("E4_SendSmsChallenge", phoneNumber);
			codes[1] = await context.CallActivityAsync<int>("E4_SendEmailChallenge", emailAddress);

			using (var timeoutCts = new CancellationTokenSource())
			{
				// The user has 90 seconds to respond with the code they received in the SMS message.
				DateTime expiration = context.CurrentUtcDateTime.AddSeconds(90);
				Task timeoutTask = context.CreateTimer(expiration, timeoutCts.Token);

				bool authorized = false;
				for (int retryCount = 0; retryCount <= 3; retryCount++)
				{
					Task<int> challengeSmsResponseTask = context.WaitForExternalEvent<int>("SmsChallengeResponse");
					Task<int> challengeMailResponseTask = context.WaitForExternalEvent<int>("MailChallengeResponse");

					Task winner = await Task.WhenAny(challengeSmsResponseTask, challengeMailResponseTask, timeoutTask);
					if (winner != timeoutTask)
					{
						// We got back a response! Compare it to the challenge code.
						if (codes.Contains(challengeSmsResponseTask.Result))
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

		[FunctionName("E4_SendSmsChallenge")]
		public static int SendSmsChallenge(
			[ActivityTrigger] string phoneNumber,
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

			log.LogInformation($"Sending verification code {challengeCode} to {phoneNumber}.");

#if NETSTANDARD2_0
			message = new CreateMessageOptions(new PhoneNumber(phoneNumber));
#else
            message = new SMSMessage { To = phoneNumber };
#endif
			message.Body = $"Your verification code is {challengeCode:0000}";

			return challengeCode;
		}

		[FunctionName("E4_SendEmailChallenge")]
		public static int SendEMailVerification(
			[ActivityTrigger]string eMail,
			ILogger log, Microsoft.Azure.WebJobs.ExecutionContext context)
		{
			var config = new ConfigurationBuilder()
				.SetBasePath(context.FunctionAppDirectory)
				.AddJsonFile("local.settings.json", true, true)
				.AddEnvironmentVariables()
				.Build();

			string uriFlow = config["Values:flowRESTTarget"];

			// Get a random number generator with a random seed (not time-based)
			var rand = new Random(Guid.NewGuid().GetHashCode());
			int challengeCode = rand.Next(10000);
			var valueObject = new SendMail
			{
				emailadress = eMail,
				emailSubject = "Confirmation of membership - " + challengeCode,
				emailBody = "Click this"
			};

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