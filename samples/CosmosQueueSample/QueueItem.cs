using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace CosmosQueueSample
{
    public class QueueItem
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        public string PartitionKey { get; set; }
        public string Activity { get; set; }
    }
}
