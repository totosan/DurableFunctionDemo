
//using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Threading.Tasks;
using MemberAdmin.Storage;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;


namespace MemberAdmin
{
    public static class Approval
    {
        [FunctionName("ApproveMemberByCode")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "approve/{id}")]HttpRequest req,
            [OrchestrationClient] DurableOrchestrationClient client,
            [Table("approval","NewMember","{id}",Connection = "AzureWebJobsStorage")] ApprovalEntity approvalEntity,
            TraceWriter log, 
            string id)
        {

            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            await client.RaiseEventAsync(approvalEntity.OrchestrationId, "OrderApprovalResult", requestBody);
            return new OkResult();

        }
    }
}
