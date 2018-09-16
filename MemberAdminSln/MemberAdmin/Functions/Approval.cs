
//using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MemberAdmin.Storage;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Build.Framework;
using Microsoft.WindowsAzure.Storage.Table;


namespace MemberAdmin
{
    public static class Approval
    {
        [FunctionName("ApproveMemberByChallengeCode")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "approve/{code}")]HttpRequest req,
            [OrchestrationClient] DurableOrchestrationClient client,
            [Table("approval", Connection = "AzureWebJobsStorage")] CloudTable table,
            string code)
        {

            var ops = TableOperation.Retrieve<ApprovalEntity>("NewMember", code);
            var tabresult = await table.ExecuteAsync(ops);

            if (tabresult.Result != null)
            {
                var approvalEntity = (ApprovalEntity)tabresult.Result;
                await client.RaiseEventAsync(approvalEntity.OrchestrationId, approvalEntity.EventName, int.Parse(code));
                return new OkResult();
            }
            else
            {
                return new BadRequestResult();
            }

        }
    }
}
