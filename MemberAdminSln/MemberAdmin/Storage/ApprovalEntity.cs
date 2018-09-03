using System;
using System.Collections.Generic;
using System.Text;
using MemberAdmin.Models;
using Microsoft.WindowsAzure.Storage.Table;

namespace MemberAdmin.Storage
{
    public class ApprovalEntity:TableEntity
    {
        public string EventName { get; }

        public ApprovalEntity(OrchestrationParameter parameter, string approvalType, int challengeCode, string eventName)
        {
            this.EventName = eventName;
            this.PartitionKey = approvalType;
            this.RowKey = challengeCode.ToString();
            this.OrchestrationId = parameter.OrchestrationId;
        }

        public ApprovalEntity(){}

        public string OrchestrationId { get; set; }
    }
}
