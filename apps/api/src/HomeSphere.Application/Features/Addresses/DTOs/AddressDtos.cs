namespace HomeSphere.Application.Features.Addresses.DTOs;

public record AddressDto(
    Guid Id,
    Guid UserId,
    string Label,
    string RecipientName,
    string PhoneNumber,
    string Line1,
    string? Line2,
    string District,
    string City,
    string Region,
    string? Reference,
    bool IsDefault,
    DateTime CreatedAtUtc
);

public record CreateAddressRequest(
    string Label,
    string RecipientName,
    string PhoneNumber,
    string Line1,
    string? Line2,
    string District,
    string City,
    string Region,
    string? Reference,
    bool IsDefault = false
);

public record UpdateAddressRequest(
    string Label,
    string RecipientName,
    string PhoneNumber,
    string Line1,
    string? Line2,
    string District,
    string City,
    string Region,
    string? Reference,
    bool IsDefault
);
