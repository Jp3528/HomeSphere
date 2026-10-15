using HomeSphere.Application.Common.Interfaces;
using HomeSphere.Application.Features.Addresses;
using HomeSphere.Application.Features.Addresses.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeSphere.WebApi.Controllers;

[ApiController]
[Authorize]
[Route("api/addresses")]
[Produces("application/json")]
public class AddressesController : ControllerBase
{
    private readonly IAddressService _addressService;
    private readonly ICurrentUserService _currentUserService;

    public AddressesController(IAddressService addressService, ICurrentUserService currentUserService)
    {
        _addressService = addressService;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AddressDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyAddresses(CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId == null)
        {
            return Unauthorized();
        }

        var addresses = await _addressService.GetUserAddressesAsync(userId.Value, cancellationToken);
        return Ok(addresses);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AddressDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAddressById(Guid id, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId == null)
        {
            return Unauthorized();
        }

        var address = await _addressService.GetAddressByIdAsync(id, userId.Value, cancellationToken);
        return Ok(address);
    }

    [HttpPost]
    [ProducesResponseType(typeof(AddressDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateAddress([FromBody] CreateAddressRequest request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId == null)
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.RecipientName) ||
            string.IsNullOrWhiteSpace(request.Line1) ||
            string.IsNullOrWhiteSpace(request.City) ||
            string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation Error",
                Detail = "Recipient Name, Phone Number, Line1 and City are required."
            });
        }

        var address = await _addressService.CreateAddressAsync(userId.Value, request, cancellationToken);
        return CreatedAtAction(nameof(GetAddressById), new { id = address.Id }, address);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(AddressDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateAddress(Guid id, [FromBody] UpdateAddressRequest request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId == null)
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.RecipientName) ||
            string.IsNullOrWhiteSpace(request.Line1) ||
            string.IsNullOrWhiteSpace(request.City) ||
            string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation Error",
                Detail = "Recipient Name, Phone Number, Line1 and City are required."
            });
        }

        var updated = await _addressService.UpdateAddressAsync(id, userId.Value, request, cancellationToken);
        return Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAddress(Guid id, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId == null)
        {
            return Unauthorized();
        }

        await _addressService.DeleteAddressAsync(id, userId.Value, cancellationToken);
        return NoContent();
    }

    [HttpPatch("{id:guid}/default")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetDefaultAddress(Guid id, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId == null)
        {
            return Unauthorized();
        }

        await _addressService.SetDefaultAddressAsync(id, userId.Value, cancellationToken);
        return NoContent();
    }
}
