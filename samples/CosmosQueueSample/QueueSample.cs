using DocumentDB.Queue;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.ChangeFeedProcessor;
using Microsoft.Azure.Documents.ChangeFeedProcessor.Reader;
using Microsoft.Azure.Documents.Client;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CosmosQueueSample
{
    class QueueSample
    {
        private const string DbName = "QueueDB";

        internal static async Task Run()
        {
            var dbUri = "https://localhost:8081/";
            var key = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";
            var collectionName = "Queue";

            await SetupEnvironmentAsync(dbUri, key, collectionName);

            var queue = await CreateQueueAsync(dbUri, key, collectionName);

            Console.WriteLine("Running...[Press ENTER to read, exit to stop]");
            var input = Console.ReadLine();
            while (!input.Equals("exit", StringComparison.InvariantCultureIgnoreCase))
            {
                if (input.Equals("q", StringComparison.InvariantCultureIgnoreCase))
                {
                    var newDocumentId = await queue.Enqueue(new { id = Guid.NewGuid().ToString() });
                    Console.WriteLine($"Enqueued document {newDocumentId}");
                }
                else
                {
                    var messages = await queue.Dequeue();
                    Console.WriteLine($"Dequeued {messages.Count} document(s)");

                    foreach (var message in messages)
                    {
                        Console.WriteLine($"Dequeued message {message.Id}");
                        await queue.Complete(message);
                    }
                }
               
                input = Console.ReadLine();
            }

            Console.WriteLine("Stopping...");
            Console.WriteLine("Stopped");
            Console.ReadLine();
        }


        private static async Task SetupEnvironmentAsync(string dbUri, string key, string collectionName)
        {
            var client = new DocumentClient(new Uri(dbUri), key);
            var database = new Database() { Id = DbName };
            await client.CreateDatabaseIfNotExistsAsync(database);
            await client.CreateDocumentCollectionIfNotExistsAsync(UriFactory.CreateDatabaseUri(DbName), new DocumentCollection() { Id = collectionName });
            await client.CreateDocumentCollectionIfNotExistsAsync(UriFactory.CreateDatabaseUri(DbName), new DocumentCollection() { Id = $"{collectionName}.Lease.ConsoleApp" });
        }

        private static Task StartFeedingDataAsync(string dbUri, string key, string collectionName, CancellationToken ctsToken)
        {
            var client = new DocumentClient(new Uri(dbUri), key);
            Uri collectionUri = UriFactory.CreateDocumentCollectionUri(DbName, collectionName);

            async Task FeedDocumentsAsync()
            {
                while (!ctsToken.IsCancellationRequested)
                {
                    await client.CreateDocumentAsync(collectionUri, new { type = "event", id = Guid.NewGuid().ToString() });
                    await Task.Delay(1000 * 5);
                }
            }

            return Task.Run(FeedDocumentsAsync, ctsToken);
        }

        private static async Task<IChangeFeedReader> RunChangeFeedProcessorAsync(string uri, string key, string collection)
        {
            var builder = new ChangeFeedReaderBuilder()
                 .WithHostName("console_app_host")
                 .WithFeedCollection(new DocumentCollectionInfo()
                 {
                     Uri = new Uri(uri),
                     MasterKey = key,
                     CollectionName = collection,
                     DatabaseName = DbName
                 })
                 .WithProcessorOptions(new ChangeFeedProcessorOptions
                 {
                     MaxItemCount = 1
                 })
                 .WithLeaseCollection(new DocumentCollectionInfo()
                 {
                     CollectionName = $"{collection}.Lease.ConsoleApp",
                     DatabaseName = DbName,
                     Uri = new Uri(uri),
                     MasterKey = key
                 });

            var processor = await builder.BuildAsync();

            await processor.StartAsync().ConfigureAwait(false);
            return processor;
        }

        private static async Task<CosmosDBQueue> CreateQueueAsync(string uri, string key, string collection)
        {
            var builder = new ChangeFeedReaderBuilder()
                 .WithHostName("console_app_host")
                 .WithFeedCollection(new DocumentCollectionInfo()
                 {
                     Uri = new Uri(uri),
                     MasterKey = key,
                     CollectionName = collection,
                     DatabaseName = DbName
                 })
                 .WithProcessorOptions(new ChangeFeedProcessorOptions
                 {
                     MaxItemCount = 1
                 })
                 .WithLeaseCollection(new DocumentCollectionInfo()
                 {
                     CollectionName = $"{collection}.Lease.ConsoleApp",
                     DatabaseName = DbName,
                     Uri = new Uri(uri),
                     MasterKey = key
                 });

            var processor = await builder.BuildAsync();

            await processor.StartAsync().ConfigureAwait(false);

            var queue = new CosmosDBQueue(processor, builder.GetFeedDocumentClient(), builder.GetFeedCollectionInfo());
            return queue;
        }


    }
}
