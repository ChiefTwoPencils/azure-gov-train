using Contoso.Events.Models;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Documents.Linq;

namespace Contoso.Events.Data
{
    public class RegistrationContext
    {
        protected CosmosSettings CosmosSettings { get; set; }
        protected Database Database { get; set; }
        protected DocumentCollection Collection { get; set; }
        protected DocumentClient Client { get; set; }

        public RegistrationContext(IOptions<CosmosSettings> cosmosSettings)
        {
            CosmosSettings = cosmosSettings.Value;
            Client = new DocumentClient(
                new Uri(CosmosSettings.EndpointUrl), 
                CosmosSettings.AuthorizationKey);
        }

        public async Task ConfigureConnectionAsync()
        {
            Database = await Client.CreateDatabaseIfNotExistsAsync(
                new Database { Id = CosmosSettings.DatabaseId });
            Collection = await Client.CreateDocumentCollectionIfNotExistsAsync(
                Database.SelfLink, new DocumentCollection { Id = CosmosSettings.CollectionId });
        }

        public async Task<int> GetRegistrantCountAsync()
        {
            var options = new FeedOptions { EnableCrossPartitionQuery = true };
            var query = Client.CreateDocumentQuery<int>(
                Collection.SelfLink, "SELECT VALUE COUNT(1) FROM registrants", options)
                .AsDocumentQuery();
            int count = 0;
            while (query.HasMoreResults)
            {
                var results = await query.ExecuteNextAsync<int>();
                count += results.Sum();
            }
            return count;
        }

        public async Task<List<string>> GetRegistrantsForEvent(string eventKey)
        {
            var query = Client.CreateDocumentQuery<GeneralRegistration>(Collection.SelfLink)
                .Where(r => r.EventKey == eventKey)
                .AsDocumentQuery();
            var registrants = new List<string>();
            while (query.HasMoreResults)
            {
                var results = await query.ExecuteNextAsync<GeneralRegistration>();
                var resultNames = results.Select(r => $"{r.FirstName} {r.LastName}");
                registrants.AddRange(resultNames);
            }
            return registrants;
        }

        public async Task<string> UploadEventRegistrationAsync(dynamic registration)
        {
            return (await Client.CreateDocumentAsync(Collection.SelfLink, registration))
                .Resource.Id;
        }
    }
}