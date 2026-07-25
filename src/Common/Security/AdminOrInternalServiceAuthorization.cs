using Common.Constants;
using Common.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Common.Security
{
    /// <summary>
    /// Requirement satisfied when the caller is either an authenticated ADMIN user,
    /// or a trusted internal service presenting the shared internal API key.
    /// Used on InventoryService's stock-mutation endpoints so OrderService can
    /// reduce/restore stock on behalf of a regular USER placing/cancelling an order,
    /// while direct external callers still need the ADMIN role.
    /// </summary>
    public class AdminOrInternalServiceRequirement : IAuthorizationRequirement
    {
    }

    public class AdminOrInternalServiceHandler : AuthorizationHandler<AdminOrInternalServiceRequirement>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly InternalApiSettings _internalApiSettings;

        public AdminOrInternalServiceHandler(
            IHttpContextAccessor httpContextAccessor,
            IOptions<InternalApiSettings> internalApiSettings)
        {
            _httpContextAccessor = httpContextAccessor;
            _internalApiSettings = internalApiSettings.Value;
        }

        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            AdminOrInternalServiceRequirement requirement)
        {
            // 1) Authenticated ADMIN user (via JWT role claim)
            if (context.User.IsInRole(Roles.Admin))
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            // 2) Trusted internal service call (OrderService -> InventoryService)
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext is not null &&
                httpContext.Request.Headers.TryGetValue(InternalAuthDefaults.HeaderName, out var providedKey) &&
                !string.IsNullOrEmpty(_internalApiSettings.ApiKey) &&
                string.Equals(providedKey.ToString(), _internalApiSettings.ApiKey, StringComparison.Ordinal))
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}
