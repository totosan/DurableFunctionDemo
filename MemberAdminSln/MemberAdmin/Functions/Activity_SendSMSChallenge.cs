using MemberAdmin.Models;
using MemberAdmin.Storage;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace MemberAdmin.Functions
{
    public class Activity_SendSMSChallenge
    {
        public const string EventNameSms = "SmsResponse";


        [FunctionName("A1_SendSmsChallenge")]
        public static ActivityResult SendSmsChallenge(
            [ActivityTrigger] VerificationParameter phone,
            [Microsoft.Azure.WebJobs.Table("approval", "AzureWebJobsStorage")]CloudTable table,
            ILogger log,
            [TwilioSms(AccountSidSetting = "TwilioAccountSid", AuthTokenSetting = "TwilioAuthToken", From = "%TwilioPhoneNumber%")]
            out CreateMessageOptions message)
        {
            try
            {
                int challengeCode = GetChallengeCode();

                log.LogInformation($"Sending verification code {challengeCode} to {phone.Payload}.");

                var entity = new ApprovalEntity(phone.OrchestrationId, "NewMember", challengeCode, EventNameSms);
                log.LogInformation(SimpleJson.SimpleJson.SerializeObject(entity));

                table.AddToTableStorageASync(entity).GetAwaiter().GetResult();
                message = new CreateMessageOptions(new PhoneNumber(phone.Payload));

                message.Body = $"Your verification code is {challengeCode:0000}";

                return new ActivityResult { HasError = false, Value = challengeCode };
            }
            catch (Exception ex)
            {
                message = new CreateMessageOptions(new PhoneNumber(phone.Payload));
                message.Body = $"Your verification cannot be obtained, because of an error. (Tell Admin:-))";

                return new ActivityResult { HasError=true, Value = ex.Message};
            }
        }

        private static int GetChallengeCode()
        {
            // Get a random number generator with a random seed (not time-based)
            var rand = new Random(Guid.NewGuid().GetHashCode());
            int challengeCode = rand.Next(10000);
            return challengeCode;
        }
    }
}
