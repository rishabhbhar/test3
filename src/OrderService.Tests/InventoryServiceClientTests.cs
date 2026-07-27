using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using OrderService.Clients;
using Xunit;

namespace OrderService.Tests
{
    public class InventoryServiceClientTests
    {
        [Fact(Skip = "Integration test - run against local inventory service")]
        public async Task GetProduct_Returns_Product_When_Available()
        {
         
            var httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:5002/") };
            var client = new InventoryServiceClient(httpClient, new NullLogger<InventoryServiceClient>());

          
            var product = await client.GetProductAsync(Guid.Parse("879906b6-ff4a-4066-a6d8-c1f508163bc4"), "your-token-here");

            
            Assert.NotNull(product);
        }
    }
}
