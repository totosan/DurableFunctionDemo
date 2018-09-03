using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace MemberAdmin.Storage
{
    public class TableStorage
    {
        public static async Task<CloudTable> CreateTableIfNotExists(string connectionString, string TableName)
        {
            // Retrieve the storage account from the connection string.
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);

            // Create the table client.
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

            // Retrieve a reference to the table.
            CloudTable table = tableClient.GetTableReference(TableName);

            // Create the table if it doesn't exist.
            if( await table.CreateIfNotExistsAsync())
            {
                return table;
            }
            else
            {
                return null;
            }
        }
    }
}
