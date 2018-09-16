using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Table;

namespace MemberAdmin.Storage
{
    public static class TabelStorageExtensions
    {
        public static async Task AddToTableStorageASync(this CloudTable table, ApprovalEntity entity)
        {
            var operation = TableOperation.Insert(entity);
            var result = await table.ExecuteAsync(operation);

            if (result.HttpStatusCode >= 300)
            {
                throw new HttpRequestException("HttpStatusCode of Tabel storage is " + result.HttpStatusCode);
            }
        }
    }
}
