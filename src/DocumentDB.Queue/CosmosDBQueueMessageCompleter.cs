using Microsoft.Azure.Documents.ChangeFeedProcessor.Reader;
using System.Threading.Tasks;

namespace DocumentDB.Queue
{
    public class CosmosDBQueueMessageCompleter
    {
        private readonly PartitionDocument result;
        private bool completed;

        public CosmosDBQueueMessageCompleter(PartitionDocument result)
        {
            this.result = result;
        }
        public async Task Complete()
        {
            if (!this.completed)
            {
                this.completed = true;
                await result.CheckpointAsync();
            }
        }


    }
}
