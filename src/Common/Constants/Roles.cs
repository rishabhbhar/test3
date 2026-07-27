namespace Common.Constants
{
   
    public static class Roles
    {
        public const string Admin = "ADMIN";
        public const string User = "USER";
    }

   
    public static class InternalAuthDefaults
    {
        public const string HeaderName = "X-Internal-Api-Key";
        public const string PolicyName = "AdminOrInternalService";
    }
}
