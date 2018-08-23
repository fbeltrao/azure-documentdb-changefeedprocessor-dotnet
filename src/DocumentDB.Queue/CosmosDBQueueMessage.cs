using Microsoft.Azure.Documents;
using System;
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
            if (this.completer != null)
                await this.completer.Complete().ConfigureAwait(false);
        }

        public Document Data { get; private set; }

        public string Id => this.Data?.Id ?? string.Empty;

        public T GetData<T>() where T: class
        {
            // TODO: find if there is a better way to do it
            return (dynamic)this.Data;
        }
    }
}
