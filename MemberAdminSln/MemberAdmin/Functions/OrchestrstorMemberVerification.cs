using MemberAdmin.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MemberAdmin.Functions;

namespace MemberAdmin
{
	[StorageAccount("AzureWebJobsStorage")]
	public static class OrchestrstorMemberVerification
	{
		private const int _expirationValue = 90;

		[FunctionName("O_EMailPhoneVerification")]
		public static async Task<object> Run(
			[OrchestrationTrigger] DurableOrchestrationContext context,
			ILogger log)
		{
			string input = context.GetInput<string>();
			string orchestrationId = context.InstanceId;

			if (!context.IsReplaying)
				log.LogInformation($"This is the input value: {input}");

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

			var phoneParameter = new VerificationParameter
			{
				OrchestrationId = orchestrationId,
				Payload = phoneNumber
			};

			var emailParameter = new VerificationParameter
			{
				OrchestrationId = orchestrationId,
				Payload = emailAddress
			};


			var fanOuts = new Task<ActivityResult>[2];

			fanOuts[0] = context.CallActivityAsync<ActivityResult>("A1_SendSmsChallenge", phoneParameter);
			fanOuts[1] = context.CallActivityAsync<ActivityResult>("A2_SendEmailChallenge", emailParameter);

			var resultsAll = Task.WhenAll(fanOuts);
			try
			{
				resultsAll.Wait();
			}catch(Exception ex)
			{

			}
			var resultList = resultsAll.Result.ToList();

			var codes = resultList.Where(x => !x.HasError).Select(x => (int)x.Value).ToList();
			log.LogWarning($"There are errors on sending varification: {resultsAll.Result.Where(x => x.HasError).Select(x => x.Value)}");
			context.SetCustomStatus(new { Codes = codes });

			using (var timeoutCts = new CancellationTokenSource())
			{
				// The user has 90 seconds to respond with the code they received in the SMS message or an eMail.
				DateTime expiration = context.CurrentUtcDateTime.AddSeconds(_expirationValue);
				Task timeoutTask = context.CreateTimer(expiration, timeoutCts.Token);

				bool authorized = false;
				for (int retryCount = 0; retryCount <= 3; retryCount++)
				{
					context.SetCustomStatus(new { message = $"Retrynumber:{retryCount}" });
					Task<int> smsResponseTask = context.WaitForExternalEvent<int>(Activity_SendSMSChallenge.EventNameSms);
					Task<int> mailResponseTask = context.WaitForExternalEvent<int>(Activity_SendEMailChallenge.EventNameEMail);

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

				return new { reason = input, isAuthorized = authorized };
			}
		}

	}
}