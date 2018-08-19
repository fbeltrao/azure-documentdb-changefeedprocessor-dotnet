//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  Licensed under the MIT license.
//----------------------------------------------------------------

namespace Microsoft.Azure.Documents.ChangeFeedProcessor.Reader
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Azure.Documents.ChangeFeedProcessor.FeedProcessing;

    /// <summary>
    /// Result of a <see cref="IPartitionReader.ReadAsync"/>
    /// </summary>
    public class PartitionDocument
    {
        private IReadOnlyList<IChangeFeedObserverContext> contexts;

        /// <summary>
        /// Initializes a new instance of the <see cref="PartitionDocument"/> class.
        /// </summary>
        public PartitionDocument()
        {
            this.Docs = new Document[0];
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PartitionDocument"/> class.
        /// </summary>
        /// <param name="context">Context</param>
        /// <param name="docs">Documents</param>
        public PartitionDocument(IReadOnlyList<Document> docs, IChangeFeedObserverContext context)
        {
            this.Docs = docs;
            this.contexts = new IChangeFeedObserverContext[] { context };
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PartitionDocument"/> class.
        /// </summary>
        /// <param name="contexts">Contexts</param>
        /// <param name="docs">Documents</param>
        public PartitionDocument(IReadOnlyList<Document> docs, IReadOnlyList<IChangeFeedObserverContext> contexts)
        {
            this.Docs = docs;
            this.contexts = contexts;
        }

        /// <summary>
        /// Gets the documents
        /// </summary>
        public IReadOnlyList<Document> Docs { get; private set; }

        /// <summary>
        /// Updates checkpoint
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task CheckpointAsync()
        {
            if (this.contexts != null)
            {
                var tasks = new List<Task>();
                foreach (var item in this.contexts)
                {
                    tasks.Add(item.CheckpointAsync());
                }

                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
        }

        internal static PartitionDocument Combine(IEnumerable<PartitionDocument> collection)
        {
            var docs = new List<Document>();
            var contexts = new List<IChangeFeedObserverContext>();
            foreach (var item in collection)
            {
                docs.AddRange(item.Docs);
                contexts.AddRange(item.contexts);
            }

            return new PartitionDocument(docs, contexts);
        }
    }
}
