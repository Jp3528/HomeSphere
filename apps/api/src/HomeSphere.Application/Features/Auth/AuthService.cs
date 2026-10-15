using HomeSphere.Application.Common.Interfaces;
using HomeSphere.Application.Features.Auth.DTOs;
using HomeSphere.Domain.Entities;
using HomeSphere.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HomeSphere.Application.Features.Auth;

public class AuthService : IAuthService
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        ILogger<AuthService> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _logger = logger;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizeEmail(request.Email);

        var existingUser = await _context.Users
            .AnyAsync(u => u.Email == normalizedEmail, cancellationToken);

        if (existingUser)
        {
            throw new ConflictException($"The email '{normalizedEmail}' is already registered.");
        }

        var customerRole = await _context.Roles
            .FirstOrDefaultAsync(r => r.Name == Role.Customer, cancellationToken)
            ?? throw new NotFoundException($"Default role '{Role.Customer}' not configured in database.");

        var user = new User
        {
            Email = normalizedEmail,
            PasswordHash = _passwordHasher.HashPassword(request.Password),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            PhoneNumber = request.PhoneNumber?.Trim(),
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        user.AddRole(customerRole);
        _context.Users.Add(user);

        var roles = new[] { Role.Customer };
        var (accessToken, expiresAt) = _jwtTokenGenerator.GenerateAccessToken(user, roles);
        var refreshToken = _jwtTokenGenerator.GenerateRefreshToken(user);

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("New user registered successfully with ID {UserId} and email {Email}", user.Id, normalizedEmail);

        return CreateAuthResponse(user, roles, accessToken, expiresAt, refreshToken.Token);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizeEmail(request.Email);

        var user = await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

        if (user == null || !_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            throw new BusinessRuleValidationException("Invalid email or password.");
        }

        if (!user.IsActive)
        {
            _logger.LogWarning("Login attempt for deactivated user ID {UserId}", user.Id);
            throw new BusinessRuleValidationException("Account is inactive. Please contact support.");
        }

        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
        var (accessToken, expiresAt) = _jwtTokenGenerator.GenerateAccessToken(user, roles);
        var refreshToken = _jwtTokenGenerator.GenerateRefreshToken(user);

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User {UserId} logged in successfully", user.Id);

        return CreateAuthResponse(user, roles, accessToken, expiresAt, refreshToken.Token);
    }

    public async Task<AuthResponse> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var tokenRecord = await _context.RefreshTokens
            .Include(t => t.User)
                .ThenInclude(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(t => t.Token == refreshToken, cancellationToken);

        if (tokenRecord == null || !tokenRecord.IsActive)
        {
            _logger.LogWarning("Invalid or inactive refresh token attempted");
            throw new BusinessRuleValidationException("Invalid or expired refresh token.");
        }

        if (!tokenRecord.User.IsActive)
        {
            throw new BusinessRuleValidationException("User account is inactive.");
        }

        var roles = tokenRecord.User.UserRoles.Select(ur => ur.Role.Name).ToList();
        var (newAccessToken, expiresAt) = _jwtTokenGenerator.GenerateAccessToken(tokenRecord.User, roles);
        var newRefreshToken = _jwtTokenGenerator.GenerateRefreshToken(tokenRecord.User);

        // Rotate: revoke old token and link to new token
        tokenRecord.Revoke(newRefreshToken.Token);
        _context.RefreshTokens.Add(newRefreshToken);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Refresh token rotated for user {UserId}", tokenRecord.UserId);

        return CreateAuthResponse(tokenRecord.User, roles, newAccessToken, expiresAt, newRefreshToken.Token);
    }

    public async Task RevokeTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var tokenRecord = await _context.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == refreshToken, cancellationToken);

        if (tokenRecord != null && tokenRecord.IsActive)
        {
            tokenRecord.Revoke();
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Refresh token revoked successfully");
        }
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private static AuthResponse CreateAuthResponse(
        User user,
        IEnumerable<string> roles,
        string accessToken,
        DateTime expiresAt,
        string refreshToken)
    {
        var userDto = new UserDto(
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            user.FullName,
            roles.ToList()
        );

        return new AuthResponse(accessToken, refreshToken, expiresAt, userDto);
    }
}
