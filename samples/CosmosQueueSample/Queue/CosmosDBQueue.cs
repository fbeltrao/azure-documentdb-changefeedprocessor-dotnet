//using Microsoft.Azure.Documents;
//using Microsoft.Azure.Documents.ChangeFeedProcessor.Reader;
//using Microsoft.Azure.Documents.Client;
//using System;
//using System.Collections.Generic;
//using System.Text;
//using System.Threading.Tasks;

//namespace CosmosQueueSample.Queue
//{
//    public class CosmosDBQueueMessageCompleter
//    {
//        private readonly PartitionDocument result;
//        private bool completed;

//        public CosmosDBQueueMessageCompleter(PartitionDocument result)
//        {
//            this.result = result;
//        }
//        public async Task Complete()
//        {
//            if (!this.completed)
//            {
//                this.completed = true;
//                await result.CheckpointAsync();
//            }
//        }


//    }

//    public class CosmosDBQueue
//    {
//        private readonly IChangeFeedReader changeFeedReader;
//        Microsoft.Azure.Documents.ChangeFeedProcessor.DataAccess.IChangeFeedDocumentClient documentClient;
//        private Uri collectionLink;

//        public CosmosDBQueue()
//        {

//        }

//        internal CosmosDBQueue(IChangeFeedReader changeFeedReader, Microsoft.Azure.Documents.ChangeFeedProcessor.DataAccess.IChangeFeedDocumentClient changeFeedDocumentClient, Microsoft.Azure.Documents.ChangeFeedProcessor.DocumentCollectionInfo documentCollectionInfo)
//        {
//            this.changeFeedReader = changeFeedReader;
//            this.documentClient = changeFeedDocumentClient;
//            this.collectionLink = UriFactory.CreateDocumentCollectionUri(documentCollectionInfo.DatabaseName, documentCollectionInfo.CollectionName);
//        }
//        public async Task<IReadOnlyList<CosmosDBQueueMessage>> Dequeue()
//        {
//            var docs = await changeFeedReader.ReadAsync().ConfigureAwait(false);
//            var completer = new CosmosDBQueueMessageCompleter(docs);

//            var result = new List<CosmosDBQueueMessage>();
//            foreach (var doc in docs.Docs)
//            {
//                result.Add(new CosmosDBQueueMessage(doc, completer));
//            }

//            return result;
//        }

//        public async Task<string> Enqueue(object messageData)
//        {
//            var response = await documentClient.CreateDocumentAsync(this.collectionLink.ToString(), messageData).ConfigureAwait(false);
//            return response.Resource.Id;
//        }

//        public async Task Complete(CosmosDBQueueMessage cosmosDBQueueMessage)
//        {
//            await cosmosDBQueueMessage.Complete().ConfigureAwait(false);

//            // TODO: remove document?
//        }
//    }

//    public class CosmosDBQueueMessage
//    {
//        private CosmosDBQueueMessageCompleter completer;

//        public CosmosDBQueueMessage(Document doc, CosmosDBQueueMessageCompleter completer)
//        {
//            this.Data = doc;
//            this.completer = completer;
//        }

//        internal async Task Complete()
//        {
//            await this.completer.Complete().ConfigureAwait(false);
//        }

//        public Document Data { get; private set; }

//        public string Id => this.Data?.Id ?? string.Empty;
//    }
//}
