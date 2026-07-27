using System.ComponentModel.DataAnnotations;

namespace AuthService.DTOs
{
    public class RegisterRequestDto
    {
        [Required, MinLength(3), MaxLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required, MinLength(6)]
        public string Password { get; set; } = string.Empty;

        
        [RegularExpression("^(ADMIN|USER)$", ErrorMessage = "Role must be either ADMIN or USER.")]
        public string? Role { get; set; }
    }

    public class LoginRequestDto
    {
        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }

    public class AuthResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAtUtc { get; set; }
        public UserResponseDto User { get; set; } = null!;
    }

    public class UserResponseDto
    {
        public Guid UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
