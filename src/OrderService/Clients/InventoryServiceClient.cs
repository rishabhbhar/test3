using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Common.Constants;
using Common.Middleware;
using Common.Settings;
using Microsoft.Extensions.Options;

namespace OrderService.Clients
{
    public interface IInventoryServiceClient
    {
        /// <summary>Fetch product details, forwarding the caller's own JWT (any authenticated user may read).</summary>
        Task<ProductDto?> GetProductAsync(Guid productId, string callerBearerToken);

        /// <summary>Reduce stock as a trusted internal caller (used when confirming an order).</summary>
        Task<ProductDto> ReduceStockAsync(Guid productId, int quantity);

        /// <summary>Restore stock as a trusted internal caller (used on order cancellation / saga rollback).</summary>
        Task<ProductDto> RestoreStockAsync(Guid productId, int quantity);
    }

    public class InventoryServiceClient : IInventoryServiceClient
    {
        private readonly HttpClient _httpClient;
        private readonly InternalApiSettings _internalApiSettings;
        private readonly ILogger<InventoryServiceClient> _logger;

        public InventoryServiceClient(
            HttpClient httpClient,
            IOptions<InternalApiSettings> internalApiSettings,
            ILogger<InventoryServiceClient> logger)
        {
            _httpClient = httpClient;
            _internalApiSettings = internalApiSettings.Value;
            _logger = logger;
        }

        public async Task<ProductDto?> GetProductAsync(Guid productId, string callerBearerToken)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/products/{productId}");
            // Only set the Authorization header when a non-empty token is provided to avoid sending an empty/invalid header
            if (!string.IsNullOrWhiteSpace(callerBearerToken))
            {
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", callerBearerToken);
            }
            request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            var requestUri = _httpClient.BaseAddress is null ? request.RequestUri?.ToString() : new Uri(_httpClient.BaseAddress, request.RequestUri).ToString();

            // Mask token for logging
            static string MaskToken(string? t)
            {
                if (string.IsNullOrEmpty(t)) return "<none>";
                if (t.Length <= 10) return "***";
                return t.Substring(0, 6) + "..." + t.Substring(t.Length - 4);
            }

            _logger.LogInformation("Calling Inventory {Uri} with token {Token}", requestUri, MaskToken(callerBearerToken));

            try
            {
                using var response = await _httpClient.SendAsync(request);
                var body = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("Inventory returned {Status} for {Uri} with body length {Length}", (int)response.StatusCode, requestUri, body?.Length ?? 0);

                if (response.StatusCode == HttpStatusCode.NotFound) return null;

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Inventory call failed: {Status} {Reason}. Body: {Body}", (int)response.StatusCode, response.ReasonPhrase, body);
                    throw new HttpRequestException($"Inventory returned {(int)response.StatusCode} {response.ReasonPhrase}: {body}");
                }

                // Content was already read into 'body' above. Deserialize from the string to avoid re-reading the response stream.
                try
                {
                    if (string.IsNullOrWhiteSpace(body)) return null;
                    var product = JsonSerializer.Deserialize<ProductDto>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return product;
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Failed to deserialize inventory response for {Uri}", requestUri);
                    throw new HttpRequestException("Invalid JSON returned from inventory service", ex);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Inventory call exception for {Uri}", requestUri);
                throw;
            }
        }

        public async Task ReduceStockAsync(Guid productId, int quantity, string? callerBearerToken = null)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/products/{productId}/reduce");
            if (!string.IsNullOrWhiteSpace(callerBearerToken))
            {
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", callerBearerToken);
            }
            request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            request.Content = JsonContent.Create(new { quantity });

            var requestUri = _httpClient.BaseAddress is null ? request.RequestUri?.ToString() : new Uri(_httpClient.BaseAddress, request.RequestUri).ToString();

            static string MaskToken(string? t)
            {
                if (string.IsNullOrEmpty(t)) return "<none>";
                if (t.Length <= 10) return "***";
                return t.Substring(0, 6) + "..." + t.Substring(t.Length - 4);
            }

            _logger.LogInformation("Calling Inventory reduce stock {Uri} with token {Token}", requestUri, MaskToken(callerBearerToken));

            try
            {
                using var response = await _httpClient.SendAsync(request);
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Inventory reduce returned {Status} for {Uri} with body length {Length}", (int)response.StatusCode, requestUri, body?.Length ?? 0);

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    throw new NotFoundException($"Product '{productId}' was not found.");
                }

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Inventory reduce failed: {Status} {Reason}. Body: {Body}", (int)response.StatusCode, response.ReasonPhrase, body);
                    throw new HttpRequestException($"Inventory reduce returned {(int)response.StatusCode} {response.ReasonPhrase}: {body}");
                }

                // success, nothing to return
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Inventory reduce exception for {Uri}", requestUri);
                throw;
            }
        }

        public async Task<ProductDto> ReduceStockAsync(Guid productId, int quantity)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/products/{productId}/reduce_stock")
            {
                Content = JsonContent.Create(new StockAdjustmentRequest { Quantity = quantity })
            };
            AddInternalKey(request);

            using var response = await _httpClient.SendAsync(request);

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                // Insufficient stock or inactive product - surface as a business rule violation
                var detail = await TryReadProblemDetail(response);
                throw new BusinessRuleException(detail ?? $"Unable to reduce stock for product {productId}.");
            }

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                throw new NotFoundException($"Product '{productId}' was not found.");
            }

            EnsureSuccess(response, $"reducing stock for product {productId}");
            return (await response.Content.ReadFromJsonAsync<ProductDto>())!;
        }

        public async Task<ProductDto> RestoreStockAsync(Guid productId, int quantity)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/products/{productId}/restore_stock")
            {
                Content = JsonContent.Create(new StockAdjustmentRequest { Quantity = quantity })
            };
            AddInternalKey(request);

            using var response = await _httpClient.SendAsync(request);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                // Product no longer exists - log and swallow, since the order is already
                // being cancelled and there is nothing further to compensate.
                _logger.LogWarning("Could not restore stock: product {ProductId} not found.", productId);
                return new ProductDto { ProductId = productId };
            }

            EnsureSuccess(response, $"restoring stock for product {productId}");
            return (await response.Content.ReadFromJsonAsync<ProductDto>())!;
        }

        private void AddInternalKey(HttpRequestMessage request)
        {
            request.Headers.Add(InternalAuthDefaults.HeaderName, _internalApiSettings.ApiKey);
        }

        private void EnsureSuccess(HttpResponseMessage response, string action)
        {
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("InventoryService call failed while {Action}. Status: {StatusCode}", action, response.StatusCode);
                throw new BusinessRuleException($"Inventory service call failed while {action} (status {(int)response.StatusCode}).");
            }
        }

        private static async Task<string?> TryReadProblemDetail(HttpResponseMessage response)
        {
            try
            {
                var problem = await response.Content.ReadFromJsonAsync<ProblemDetailPayload>();
                return problem?.detail;
            }
            catch
            {
                return null;
            }
        }

        private class ProblemDetailPayload
        {
            public string? detail { get; set; }
        }
    }
}
