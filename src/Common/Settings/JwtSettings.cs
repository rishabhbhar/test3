namespace Common.Settings
{
    /// <summary>
    /// JWT settings. Must be IDENTICAL (Secret, Issuer, Audience) across
    /// AuthService (issuer), InventoryService and OrderService (resource servers)
    /// so tokens minted by AuthService validate correctly on the other two.
    /// </summary>
    public class JwtSettings
    {
        public const string SectionName = "Jwt";

        public string Secret { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public int ExpiryMinutes { get; set; } = 60;
    }
}
