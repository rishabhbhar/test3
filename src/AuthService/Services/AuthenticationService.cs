using AuthService.Data;
using AuthService.DTOs;
using AuthService.Models;
using Common.Middleware;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AuthService.Services
{
    public interface IAuthenticationService
    {
        Task<AuthResponseDto> RegisterAsync(RegisterRequestDto dto);
        Task<AuthResponseDto> LoginAsync(LoginRequestDto dto);
        Task<UserResponseDto> GetCurrentUserAsync(Guid userId);
    }

    public class AuthenticationService : IAuthenticationService
    {
        private readonly AuthDbContext _context;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ITokenService _tokenService;
        private readonly ILogger<AuthenticationService> _logger;

        public AuthenticationService(
            AuthDbContext context,
            IPasswordHasher passwordHasher,
            ITokenService tokenService,
            ILogger<AuthenticationService> logger)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _tokenService = tokenService;
            _logger = logger;
        }

        public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto dto)
        {
            var usernameNormalized = dto.Username.Trim();

            var exists = await _context.Users.AnyAsync(u => u.Username == usernameNormalized);
            if (exists)
            {
                throw new BusinessRuleException($"Username '{usernameNormalized}' is already taken.");
            }

            var user = new User
            {
                UserId = Guid.NewGuid(),
                Username = usernameNormalized,
                PasswordHash = _passwordHasher.Hash(dto.Password),
                Role = string.IsNullOrWhiteSpace(dto.Role) ? Common.Constants.Roles.User : dto.Role.ToUpperInvariant(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Registered new user {Username} with role {Role}", user.Username, user.Role);

            return BuildAuthResponse(user);
        }

        public async Task<AuthResponseDto> LoginAsync(LoginRequestDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == dto.Username.Trim());

            if (user is null || !_passwordHasher.Verify(dto.Password, user.PasswordHash))
            {
                _logger.LogWarning("Failed login attempt for username {Username}", dto.Username);
                throw new UnauthorizedAccessException("Invalid username or password.");
            }

            if (!user.IsActive)
            {
                throw new ForbiddenException("This account has been deactivated.");
            }

            _logger.LogInformation("User {Username} logged in", user.Username);

            return BuildAuthResponse(user);
        }

        public async Task<UserResponseDto> GetCurrentUserAsync(Guid userId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user is null)
            {
                throw new NotFoundException("User not found.");
            }

            return ToResponseDto(user);
        }

        private AuthResponseDto BuildAuthResponse(User user)
        {
            var (token, expiresAtUtc) = _tokenService.GenerateToken(user);
            return new AuthResponseDto
            {
                Token = token,
                ExpiresAtUtc = expiresAtUtc,
                User = ToResponseDto(user)
            };
        }

        private static UserResponseDto ToResponseDto(User user) => new()
        {
            UserId = user.UserId,
            Username = user.Username,
            Role = user.Role,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt
        };
    }
}
