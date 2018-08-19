using System.Threading.Tasks;

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
