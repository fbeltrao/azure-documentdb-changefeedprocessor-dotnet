using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DocumentDB.Queue
{
    /// <summary>
    /// Result of a <see cref="CosmosDBQueue.Dequeue"/>
    /// </summary>
    public class CosmosDBQueueMessageCollection : IReadOnlyList<CosmosDBQueueMessage>
    {
        private readonly IReadOnlyList<CosmosDBQueueMessage> messages;
        private readonly CosmosDBQueueMessageCompleter completer;

        public int Count => this.messages.Count;

        public CosmosDBQueueMessage this[int index] => this.messages[index];

        /// <summary>
        /// Creates a new instance of <see cref="CosmosDBQueueMessageCollection"/>
        /// </summary>
        /// <param name="messages"></param>
        /// <param name="completer"></param>
        public CosmosDBQueueMessageCollection(IReadOnlyList<CosmosDBQueueMessage> messages, CosmosDBQueueMessageCompleter completer)
        {
            this.messages = messages;
            this.completer = completer;
        }

        /// <summary>
        /// Complete all messages in the collection
        /// </summary>
        /// <returns></returns>
        public async Task Complete() => await completer.Complete();

        public IEnumerator<CosmosDBQueueMessage> GetEnumerator() => this.messages.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => this.messages.GetEnumerator();
    }
}
