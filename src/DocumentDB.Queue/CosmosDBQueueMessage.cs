using Microsoft.Azure.Documents;
using System.Threading.Tasks;

namespace DocumentDB.Queue
{
    public class CosmosDBQueueMessage
    {
        private CosmosDBQueueMessageCompleter completer;

        public CosmosDBQueueMessage(Document doc, CosmosDBQueueMessageCompleter completer)
        {
            this.Data = doc;
            this.completer = completer;
        }

        internal async Task Complete()
        {
            await this.completer.Complete().ConfigureAwait(false);
        }

        public Document Data { get; private set; }

        public string Id => this.Data?.Id ?? string.Empty;
    }
}
