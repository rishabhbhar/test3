namespace Common.Settings
{
    /// <summary>
    /// Shared secret used for trusted service-to-service calls between OrderService
    /// and InventoryService. In production this would be replaced with mTLS, a
    /// service-mesh identity, or client-credentials OAuth tokens - a static shared
    /// key is used here to keep the sample self-contained.
    /// </summary>
    public class InternalApiSettings
    {
        public const string SectionName = "InternalApi";

        public string ApiKey { get; set; } = string.Empty;
    }
}
