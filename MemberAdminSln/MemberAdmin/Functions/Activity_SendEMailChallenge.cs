using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using MemberAdmin.Models;
using MemberAdmin.Storage;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Table;
using RestSharp;

namespace MemberAdmin.Functions
{
    public static class Activity_SendEMailChallenge
    {
        public const string EventNameEMail = "MailResponse";

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

            var entity = new ApprovalEntity(eMail.OrchestrationId, "NewMember", challengeCode, EventNameEMail);
            await table.AddToTableStorageASync(entity);

            var client = new RestClient(uriFlow);
            var request = new RestRequest(Method.POST);
            request.AddHeader("Cache-Control", "no-cache");
            request.AddHeader("Content-Type", "application/json");
            request.AddParameter("undefined", SimpleJson.SimpleJson.SerializeObject(valueObject), ParameterType.RequestBody);
            IRestResponse response = client.Execute(request);
            if (!response.IsSuccessful)
            {
                log.LogError(new EventId(1, "EMail not sent"), $"EMail to receiver {valueObject.emailadress} could not be sent. Error: {response.Content}");
            }

            return challengeCode;
        }

    }
}
