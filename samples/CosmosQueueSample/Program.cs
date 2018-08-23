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
    class Program
    {
        private const string DbName = "QueueDB";
        private static string[] Activities = new string[]
        {
            "Buy milk",
            "Walk the dog",
            "Do the dishes",
            "Take the garbage out",
            "Cook lunch",
            "Water the plants"
        };

        private static Random randomizer = new Random();

        static async Task Main(string[] args)
        {
            var dbUri = "https://localhost:8081/";
            var key = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";
            var collectionName = "Queue";

            await SetupEnvironmentAsync(dbUri, key, collectionName);

            var queueClient1 = await CreateQueueAsync(dbUri, key, collectionName, "queueClient1");
            var queueClient2 = await CreateQueueAsync(dbUri, key, collectionName, "queueClient2");

            Console.WriteLine("Press ENTER to enqueue, d1 and d2 to dequeue, da1 and da2 to dequeue and abandon, exit to stop");
            var input = Console.ReadLine();
            while (!input.Equals("exit", StringComparison.InvariantCultureIgnoreCase))
            {
                if (input.Equals("d1", StringComparison.InvariantCultureIgnoreCase) || input.Equals("d2", StringComparison.InvariantCultureIgnoreCase))
                {
                    var queueClient = input.Equals("d1", StringComparison.InvariantCultureIgnoreCase) ? queueClient1 : queueClient2;
                    var messages = await queueClient.Dequeue();
                    Console.WriteLine($"Dequeued {messages.Count} document(s)");

                    foreach (var message in messages)
                    {
                        var queueItem = message.GetData<QueueItem>();
                        Console.WriteLine($"Dequeued message {message.Id}, {queueItem.Activity}");
                    }

                   await messages.Complete();
                    
                }
                else if (input.Equals("da1", StringComparison.InvariantCultureIgnoreCase) || input.Equals("da2", StringComparison.InvariantCultureIgnoreCase))
                {
                    var queueClient = input.Equals("da1", StringComparison.InvariantCultureIgnoreCase) ? queueClient1 : queueClient2;

                    var messages = await queueClient.Dequeue();
                    Console.WriteLine($"Dequeued {messages.Count} document(s)");

                    foreach (var message in messages)
                    {
                        var queueItem = message.GetData<QueueItem>();
                        Console.WriteLine($"Dequeued message {message.Id}, {queueItem.Activity}");

                        await queueClient.Abandon(message, queueItem.PartitionKey);
                        Console.WriteLine($"Abandoned message {message.Id}, {queueItem.Activity}");
                    }

                    await messages.Complete();
                }
                else
                {
                    var queueItem = new QueueItem()
                    {
                        Id = Guid.NewGuid().ToString(),
                        PartitionKey = Guid.NewGuid().ToString(),
                        Activity = GetRandomActivity()
                    };
                    var newDocumentId = await queueClient1.Enqueue(queueItem);
                    Console.WriteLine($"Enqueued document {newDocumentId}, {queueItem.Activity}");

                }

                input = Console.ReadLine();
            }

            Console.WriteLine("Stopped");
            Console.ReadLine();
        }

        private static string GetRandomActivity() => Activities[randomizer.Next(Activities.Length)];

        private static async Task SetupEnvironmentAsync(string dbUri, string key, string collectionName)
        {
            var client = new DocumentClient(new Uri(dbUri), key);
            var database = new Database() { Id = DbName };
            await client.CreateDatabaseIfNotExistsAsync(database);
            await client.CreateDocumentCollectionIfNotExistsAsync(UriFactory.CreateDatabaseUri(DbName), 
                new DocumentCollection()
                {
                    Id = collectionName,
                    PartitionKey = new PartitionKeyDefinition
                    {
                        Paths = new System.Collections.ObjectModel.Collection<string>(new string[] { "/PartitionKey" }),
                    }
                });
            await client.CreateDocumentCollectionIfNotExistsAsync(UriFactory.CreateDatabaseUri(DbName), new DocumentCollection() { Id = $"{collectionName}.Lease.ConsoleApp" });
        }

        private static async Task<CosmosDBQueue> CreateQueueAsync(string uri, string key, string collection, string hostName)
        {
            var builder = new ChangeFeedReaderBuilder()
                 .WithHostName(hostName)
                 .WithFeedCollection(new DocumentCollectionInfo()
                 {
                     Uri = new Uri(uri),
                     MasterKey = key,
                     CollectionName = collection,
                     DatabaseName = DbName
                 })
                 .WithProcessorOptions(new ChangeFeedProcessorOptions
                 {
                     MaxItemCount = 1,
                     StartFromBeginning = true,
                 })
                 .WithLeaseCollection(new DocumentCollectionInfo()
                 {
                     CollectionName = $"{collection}.Lease.ConsoleApp",
                     DatabaseName = DbName,
                     Uri = new Uri(uri),
                     MasterKey = key
                 });

            var reader = await builder.BuildAsync();

            await reader.StartAsync().ConfigureAwait(false);

            var queue = new CosmosDBQueue(reader, builder.GetFeedDocumentClient(), builder.GetFeedCollectionInfo());
            return queue;
        }
    }
}
