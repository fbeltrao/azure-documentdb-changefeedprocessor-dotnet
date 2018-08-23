using Microsoft.Azure.Documents.ChangeFeedProcessor.Reader;
using Microsoft.Azure.Documents.Client;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DocumentDB.Queue
{
    public class CosmosDBQueue
    {
        private readonly IChangeFeedReader changeFeedReader;
        DocumentClient documentClient;
        private readonly string databaseId;
        private readonly Uri collectionLink;
        private readonly string collectionName;

        public bool DeleteDocumentOnComplete { get; set; }

        public CosmosDBQueue()
        {
        }

        public CosmosDBQueue(IChangeFeedReader changeFeedReader, DocumentClient documentClient, Microsoft.Azure.Documents.ChangeFeedProcessor.DocumentCollectionInfo documentCollectionInfo)
        {
            this.changeFeedReader = changeFeedReader;
            this.documentClient = documentClient;
            this.databaseId = documentCollectionInfo.DatabaseName;
            this.collectionName = documentCollectionInfo.CollectionName;
            this.collectionLink = UriFactory.CreateDocumentCollectionUri(this.databaseId, this.collectionName);
        }
        public async Task<CosmosDBQueueMessageCollection> Dequeue()
        {
            var docs = await changeFeedReader.ReadAsync().ConfigureAwait(false);
            var completer = new CosmosDBQueueMessageCompleter(docs);

            var result = new List<CosmosDBQueueMessage>();
            foreach (var doc in docs.Docs)
            {
                result.Add(new CosmosDBQueueMessage(doc, completer));
            }

            return new CosmosDBQueueMessageCollection(result, completer);
        }

        public async Task<string> Enqueue(object messageData)
        {
            var response = await documentClient.CreateDocumentAsync(this.collectionLink.ToString(), messageData).ConfigureAwait(false);
            return response.Resource.Id;
        }

        //public async Task Complete(CosmosDBQueueMessage cosmosDBQueueMessage, string partitionKey = null)
        //{
        //    await cosmosDBQueueMessage.Complete().ConfigureAwait(false);

        //    // TODO: remove document?
        //    if (this.DeleteDocumentOnComplete)
        //    {
        //        RequestOptions requestOptions = new RequestOptions
        //        {
        //            AccessCondition = new Microsoft.Azure.Documents.Client.AccessCondition
        //            {
        //                Condition = cosmosDBQueueMessage.Data.ETag,
        //                Type = AccessConditionType.IfMatch
        //            }
        //        };

        //        if (!string.IsNullOrEmpty(partitionKey))
        //            requestOptions.PartitionKey = new Microsoft.Azure.Documents.PartitionKey(partitionKey);

        //        await documentClient.DeleteDocumentAsync(
        //          UriFactory.CreateDocumentUri(this.databaseId, this.collectionName, cosmosDBQueueMessage.Data.Id),
        //                requestOptions).ConfigureAwait(false);
        //    }
        //}

        /// <summary>
        /// Abandons a <see cref="CosmosDBQueueMessage"/>, making it appear in a future <see cref="IChangeFeedReader.ReadAsync"/>
        /// </summary>
        /// <param name="queueMessage">Queue message</param>
        /// <param name="partitionKey">Queue message partition key (if one was defined in the Cosmos DB collection)</param>
        /// <returns></returns>
        public async Task Abandon(CosmosDBQueueMessage queueMessage, string partitionKey = null)
        {
            // Option 1: just don't forward the reader, and reset the continuation value.
            // The problem lies in having other messages that were already dequeued, which saved the cursor ahead

            // Option 2: save document change, making it appear a second time
            var dequeueCount = queueMessage.Data.GetPropertyValue<int?>("dequeueCount");
            queueMessage.Data.SetPropertyValue("dequeueCount", 1 + (dequeueCount ?? 0));

            RequestOptions requestOptions = new RequestOptions
            {
                AccessCondition = new Microsoft.Azure.Documents.Client.AccessCondition
                {
                    Condition = queueMessage.Data.ETag,
                    Type = AccessConditionType.IfMatch
                }
            };
            if (!string.IsNullOrEmpty(partitionKey))
                requestOptions.PartitionKey = new Microsoft.Azure.Documents.PartitionKey(partitionKey);

            await documentClient.ReplaceDocumentAsync(UriFactory.CreateDocumentUri(this.databaseId, this.collectionName, queueMessage.Data.Id), queueMessage.Data,
                requestOptions).ConfigureAwait(false);
        }
    }
}
