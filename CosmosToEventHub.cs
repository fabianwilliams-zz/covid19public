using System;
using System.Collections.Generic;
using Microsoft.Azure.Documents;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace aspa
{
    public static class CosmosToEventHub
    {
        [FunctionName("CosmosToEventHub")]
        [return: EventHub("c19", Connection = "EventHubAppSetting")]
        public static IReadOnlyList<Document> Run([CosmosDBTrigger(
            databaseName: "YOURDBHERE",
            collectionName: "YOURCOLLECTIONAKATABLEHERE",
            ConnectionStringSetting = "YOURCONNSTRINGHERE",
            LeaseCollectionName = "YOURLEASECOLLNAME")]IReadOnlyList<Document> input, ILogger log)
        {
            if (input != null && input.Count > 0)
            {
                log.LogInformation($"Sending JSON to EventHub");
                log.LogInformation("Documents modified " + input.Count);
                log.LogInformation("First document Id " + input[0].Id);
            }
            return input;
        }
    }
}
