namespace Common.Constants
{
    /// <summary>
    /// Role names, must match the CHECK constraint values in auth_db (users.role).
    /// </summary>
    public static class Roles
    {
        public const string Admin = "ADMIN";
        public const string User = "USER";
    }

    /// <summary>
    /// Header used for trusted service-to-service calls (e.g. OrderService -> InventoryService)
    /// that must bypass end-user role checks (a placing User must still trigger a stock
    /// reduction, even though direct external stock mutation is Admin-only).
    /// </summary>
    public static class InternalAuthDefaults
    {
        public const string HeaderName = "X-Internal-Api-Key";
        public const string PolicyName = "AdminOrInternalService";
    }
}
