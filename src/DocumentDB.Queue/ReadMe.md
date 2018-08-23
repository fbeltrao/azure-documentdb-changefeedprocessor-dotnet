# Cosmos DB Change Feed Reader

This sample repository extends the CosmosDB Change Feed enabling a polling topology to the feed consumption.

The default implementation provided by the library sends all notifications to a ```IChangeFeedObserver``` that receives captured document changes.

Polling allows a finer control over the pace in which document changes are received. It also allows a explicit checkpoint saving (optimize for speed vs resilence).


## Polling example

Following the sample principle of the observer implementation we start by creating a reader:

```c#
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
            MaxItemCount = 10,
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

await reader.StartAsync();
```

The initialization will start the reader, negotiating read partitions with active readers. If the this reader is the only active instance all partitions will be leased.

Reading the change feed can be done like this:
```c#
var changeFeed = await reader.ReadAsync();
Console.WriteLine($"Read {changeFeed.Docs.Count} documents");

// Save checkpoint in lease collection so that a crash in the reader won't receive documents that have already been processed
await changeFeed.SaveCheckpointAsync();
```

However, the sample implementation has the following **known limitations**:

- Since a reader can own multiple leases calling ```ChangeFeedDocumentChanges.SaveCheckpointAsync()``` will update all partitions
- If you specify that you want to return a limit of 1 document per read, **1 document per partition** will actually be returned. To actually return 1 document the implementation would have to change to a round-robin read distribution between owned partitions.
-  Calling ```ChangeFeedDocumentChanges.SaveCheckpointAsync()``` after reading N documents will move the cursor forward, even if that is no the intent (i.e. processing the previous ReadAsync results failed). The responsibility of moving forward is in the application hands.


## Queue Based on Change Feed Reader

The change feed reader allows building a simple (and limited) queue based on a CosmosDB collection, where the operations are implemented in the following way:

|Operation|Implementation|
|-|-|
|Adding item to queue|Add document to collection|
|Dequeue item|Read next  change feed|
|Complete queue item|Save reader checkpoint|
|Abandon queue item|Update the document, causing it to appear later in the change feed|

To have multiple consumers the collection must have a partition key, since the change feed processor library supports 1 active reader per partition.

An example would look like:

```c#
var builder = new ChangeFeedReaderBuilder()
...
var reader = await builder.BuildAsync();
await reader.StartAsync();

var queue = new CosmosDBQueue(reader, builder.GetFeedDocumentClient(), builder.GetFeedCollectionInfo());

var queueItem = new MyQueueItem()
{
    Id = Guid.NewGuid().ToString(),
    PartitionKey = Guid.NewGuid().ToString(),
    Activity = "Make C# example app"
};

var newDocumentId = await queue.Enqueue(queueItem);
Console.WriteLine($"Enqueued document {newDocumentId}, {queueItem.Activity}");

var messages = await queue.Dequeue();
Console.WriteLine($"Dequeued {messages.Count} document(s)");

// process first item
var myQueueItem = messages.First().GetData<MyQueueItem>();
Console.WriteLine($"Dequeued message {message.Id}, {myQueueItem.Activity}");

// abandon the first, complete the others
await queue.Abandon(messages.First(), myQueueItem.PartitionKey);
await messages.Complete();
```