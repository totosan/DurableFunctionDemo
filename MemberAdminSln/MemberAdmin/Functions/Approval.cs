using MemberAdmin.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.WindowsAzure.Storage.Table;
using System.Threading.Tasks;


namespace MemberAdmin
{
    /*
     * On time writing this Approval method, the Azure Table Storage binding was not fully implemented
     * So I used direct access to  Table.
     *
     */
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
                var status = await client.GetStatusAsync(approvalEntity.OrchestrationId);
                if (status.RuntimeStatus == OrchestrationRuntimeStatus.Running)
                {
                    await client.RaiseEventAsync(approvalEntity.OrchestrationId, approvalEntity.EventName,
                        int.Parse(code));
                    return new OkResult();
                }
                else
                {
                    return new NotFoundResult();
                }
            }
            else
            {
                return new BadRequestResult();
            }

        }
    }
}
