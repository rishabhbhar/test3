namespace AuthService.Models
{
    public class User
    {
        public Guid UserId { get; set; }

        public string Username { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;

        /// <summary>"ADMIN" or "USER" - matches the chk_user_role CHECK constraint.</summary>
        public string Role { get; set; } = "USER";

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; }
    }
}
