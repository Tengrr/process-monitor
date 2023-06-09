﻿namespace ProcesssMonitor.FunctionApp
{
    using Microsoft.Azure.Cosmos;
    using System.Threading.Tasks;
    using System;
    public static class CosmosDatabaseHandler
    {

        private static string connectionString = "AccountEndpoint=https://fsyaccount.documents.azure.com:443/;AccountKey=************************************";
        private static string databaseId = "OperationDb";
        private static string containerId = "Operations";

        public static async Task UpdateOperationItemAsync(string id, string processList)
        {
            var cosmosClient = new CosmosClient(connectionString);

            var database = cosmosClient.GetDatabase(databaseId);

            var container = database.GetContainer(containerId);

            var sqlQueryText = $"SELECT * FROM c WHERE c.id = '{id}'";

            Console.WriteLine("Running query: {0}\n", sqlQueryText);

            QueryDefinition queryDefinition = new(sqlQueryText);
            FeedIterator<Operation> queryResultSetIterator = container.GetItemQueryIterator<Operation>(queryDefinition);


            while (queryResultSetIterator.HasMoreResults)
            {
                FeedResponse<Operation> currentResultSet = await queryResultSetIterator.ReadNextAsync();
                foreach (Operation operation in currentResultSet)
                {
                    operation.processList = processList;
                    await container.ReplaceItemAsync<Operation>(operation, id, new PartitionKey(operation.name));
                    break;
                }
            }

            cosmosClient.Dispose();

        }
    }
}
