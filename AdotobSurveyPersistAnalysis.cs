using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Threading;
using Microsoft.Azure.CognitiveServices.Language.TextAnalytics;
using Microsoft.Azure.CognitiveServices.Language.TextAnalytics.Models;
using Microsoft.Rest;
using Microsoft.AspNetCore.Mvc;

namespace aspa
{
    public static class AdotobSurveyPersistAnalysis
    {
        [FunctionName("aspa")]
        public static async Task<SurveyPollForCovid19> RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            SurveyPollForCovid19 cv19pollinstance = context.GetInput<SurveyPollForCovid19>();

            var outputs = new List<string>();

            outputs.Add(await context.CallActivityAsync<string>("aspa_FanOutTextAnalysis", cv19pollinstance.HowIsYourMentalHealthToday));
            outputs.Add(await context.CallActivityAsync<string>("aspa_FanOutTextAnalysis", cv19pollinstance.HowIsYourPhysicalHealthToday));
            outputs.Add(await context.CallActivityAsync<string>("aspa_FanOutTextAnalysis", cv19pollinstance.YourFeelingsOnWorkFromHomeToday));
            outputs.Add(await context.CallActivityAsync<string>("aspa_FanOutTextAnalysis", cv19pollinstance.YourFeelingsOnHomeSchoolingToday));
            outputs.Add(await context.CallActivityAsync<string>("aspa_FanOutTextAnalysis", cv19pollinstance.ThoughtsOnSocialDistancingOnSoftware));


            cv19pollinstance.HowIsYourMentalHealthTodaySentiment = Convert.ToDouble(outputs[0]);
            cv19pollinstance.HowIsYourPhysicalHealthTodaySentiment = Convert.ToDouble(outputs[1]);
            cv19pollinstance.YourFeelingsOnWorkFromHomeTodaySentiment = Convert.ToDouble(outputs[2]);
            cv19pollinstance.YourFeelingsOnHomeSchoolingTodaySentiment = Convert.ToDouble(outputs[3]);
            cv19pollinstance.ThoughtsOnSocialDistancingOnSoftwareSentiment = Convert.ToDouble(outputs[4]);

            SurveyPollForCovid19 currResults = await context.CallActivityAsync<SurveyPollForCovid19>(
            "aspa_CosmosDBPersist", cv19pollinstance);

            return cv19pollinstance;
        }

        [FunctionName("aspa_FanOutTextAnalysis")]
        public static async Task<string> CallToTextAnalyticsAsync([ActivityTrigger] string item, ILogger log)
        {
            using (var client = new HttpClient())
            {
                log.LogInformation($"The Text being sent over is: {item}");
                string urlToBeSent = "https://YOURFUNCTIONHERE.azurewebsites.net/api/AnalyzeText?Text=" + item;
                HttpResponseMessage response = await client.GetAsync(urlToBeSent);
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                return responseBody;
            }
        }

        [FunctionName("aspa_CosmosDBPersist")]
        public static async Task<string> SurveytoCosmosDbAsync([ActivityTrigger] SurveyPollForCovid19 name, ILogger log,
            [CosmosDB(
            databaseName: "YOURDBHERE",
            collectionName: "YOURCOLLECTIONAKATABLEHERE",
            ConnectionStringSetting = "YOURCONNSTRINGHERE")] IAsyncCollector<SurveyPollForCovid19> createSurveyEntry)
        {
            log.LogInformation($"Currently will be working on:  {name}.");


            SurveyPollForCovid19 curritem = new SurveyPollForCovid19()
            {
                Id = name.Id,
                DateCreated = DateTime.Now.ToString("MM/dd/yyyy"),
                AreYouCurrentlyEmployed = name.AreYouCurrentlyEmployed,
                Covid19Unemployment = name.Covid19Unemployment,
                MoreOrLessAnxietyToday = name.MoreOrLessAnxietyToday,
                HowIsYourMentalHealthToday = name.HowIsYourMentalHealthToday,
                HowIsYourMentalHealthTodaySentiment = name.HowIsYourMentalHealthTodaySentiment,
                HowIsYourPhysicalHealthToday = name.HowIsYourPhysicalHealthToday,
                HowIsYourPhysicalHealthTodaySentiment = name.HowIsYourPhysicalHealthTodaySentiment,
                YourFeelingsOnWorkFromHomeToday = name.YourFeelingsOnWorkFromHomeToday,
                YourFeelingsOnWorkFromHomeTodaySentiment = name.YourFeelingsOnWorkFromHomeTodaySentiment,
                YourFeelingsOnHomeSchoolingToday = name.YourFeelingsOnHomeSchoolingToday,
                YourFeelingsOnHomeSchoolingTodaySentiment = name.YourFeelingsOnHomeSchoolingTodaySentiment,
                ThoughtsOnOpennessToDistanceLearning = name.ThoughtsOnOpennessToDistanceLearning,
                ThoughtsOnSocialDistancingOnSoftware = name.ThoughtsOnSocialDistancingOnSoftware,
                ThoughtsOnSocialDistancingOnSoftwareSentiment = name.ThoughtsOnSocialDistancingOnSoftwareSentiment,
                City = name.City,
                Region = name.Region,
                Country = name.Country,
                OverallSentiment = sumofall,
                HealthSentiment = sumofhealth,
                MaxSentiment = 1,
                Lon = name.Lon,
                Lat = name.Lat
            };

            await createSurveyEntry.AddAsync(curritem);
            return $"Item Created is: {name}!";
        }

        [FunctionName("aspa_HttpStart")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            var jsonContent = await req.Content.ReadAsStringAsync();
            SurveyPollForCovid19 form = JsonConvert.DeserializeObject<SurveyPollForCovid19>(jsonContent);
            // Function input comes from the request content.
            string instanceId = await starter.StartNewAsync("aspa", form);
            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }
    }

    public class SurveyPollForCovid19
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; } = Guid.NewGuid().ToString("n");
        [JsonProperty(PropertyName = "datecreated")]
        public string DateCreated { get; set; }
        [JsonProperty(PropertyName = "areyoucurrentlyemployed")]
        public string AreYouCurrentlyEmployed { get; set; }
        [JsonProperty(PropertyName = "covid19unemployment")]
        public string Covid19Unemployment { get; set; }
        [JsonProperty(PropertyName = "moreorlessanxietytoday")]
        public string MoreOrLessAnxietyToday { get; set; }
        [JsonProperty(PropertyName = "howisyourmentalhealthtoday")]
        public string HowIsYourMentalHealthToday { get; set; }
        [JsonProperty(PropertyName = "howisyourmentalhealthtodaysentiment")]
        public double HowIsYourMentalHealthTodaySentiment { get; set; }
        [JsonProperty(PropertyName = "howisyourphysicalhealthtoday")]
        public string HowIsYourPhysicalHealthToday { get; set; }
        [JsonProperty(PropertyName = "howisyourphysicalhealthtodaysentiment")]
        public double HowIsYourPhysicalHealthTodaySentiment { get; set; }
        [JsonProperty(PropertyName = "yourfeelingsonworkfromhometoday")]
        public string YourFeelingsOnWorkFromHomeToday { get; set; }
        [JsonProperty(PropertyName = "yourfeelingsonworkfromhometodaysentiment")]
        public double YourFeelingsOnWorkFromHomeTodaySentiment { get; set; }
        [JsonProperty(PropertyName = "yourfeelingsonhomeschoolingtoday")]
        public string YourFeelingsOnHomeSchoolingToday { get; set; }
        [JsonProperty(PropertyName = "yourfeelingsonhomeschoolingtodaysentiment")]
        public double YourFeelingsOnHomeSchoolingTodaySentiment { get; set; }
        [JsonProperty(PropertyName = "thoughtsonopennesstodistancelearning")]
        public string ThoughtsOnOpennessToDistanceLearning { get; set; }
        [JsonProperty(PropertyName = "thoughtsonsocialdistancingonsoftware")]
        public string ThoughtsOnSocialDistancingOnSoftware { get; set; }
        [JsonProperty(PropertyName = "thoughtsonsocialdistancingonsoftwaresentiment")]
        public double ThoughtsOnSocialDistancingOnSoftwareSentiment { get; set; }
        [JsonProperty(PropertyName = "city")]
        public string City { get; set; }
        [JsonProperty(PropertyName = "region")]
        public string Region { get; set; }
        [JsonProperty(PropertyName = "country")]
        public string Country { get; set; }
        [JsonProperty(PropertyName = "overallsentiment")]
        public double OverallSentiment { get; set; }
        [JsonProperty(PropertyName = "healthsentiment")]
        public double HealthSentiment { get; set; }
        [JsonProperty(PropertyName = "maxsentiment")]
        public double MaxSentiment { get; set; }
        [JsonProperty(PropertyName = "lon")]
        public string Lon { get; set; }
        [JsonProperty(PropertyName = "lat")]
        public string Lat { get; set; }
        
    }

}