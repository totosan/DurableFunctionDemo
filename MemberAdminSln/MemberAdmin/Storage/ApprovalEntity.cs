using System;
using System.Collections.Generic;
using System.Text;
using MemberAdmin.Models;
using Microsoft.WindowsAzure.Storage.Table;

namespace MemberAdmin.Storage
{
    public class ApprovalEntity:TableEntity
    {

        public ApprovalEntity(string orchestrationId, string approvalType, int challengeCode, string eventName)
        {
            this.PartitionKey = approvalType;
            this.RowKey = challengeCode.ToString();
            this.OrchestrationId = orchestrationId;
            this.EventName = eventName;
        }

        public ApprovalEntity(){}

        public string OrchestrationId { get; set; }
        public string EventName { get; set; }
    }
}
