namespace HomeSphere.Application.Features.Users.DTOs;

public record UserProfileDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string FullName,
    string? PhoneNumber,
    IReadOnlyList<string> Roles,
    DateTime CreatedAtUtc
);

public record UpdateProfileRequest(
    string FirstName,
    string LastName,
    string? PhoneNumber
);
