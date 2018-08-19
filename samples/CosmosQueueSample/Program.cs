using System;
using System.Threading;
using System.Threading.Tasks;
using CosmosQueueSample.Queue;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.ChangeFeedProcessor;
using Microsoft.Azure.Documents.ChangeFeedProcessor.PartitionManagement;
using Microsoft.Azure.Documents.ChangeFeedProcessor.Reader;
using Microsoft.Azure.Documents.Client;

namespace CosmosQueueSample
{
    class Program
    {

        static async Task Main(string[] args)
        {
            //await FeedReaderSample.Run();
            await QueueSample.Run();
        }
    }
}
