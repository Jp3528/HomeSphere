using HomeSphere.Application.Common.Interfaces;
using HomeSphere.Application.Features.Users;
using HomeSphere.Application.Features.Users.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeSphere.WebApi.Controllers;

[ApiController]
[Authorize]
[Route("api/me")]
[Produces("application/json")]
public class ProfileController : ControllerBase
{
    private readonly IProfileService _profileService;
    private readonly ICurrentUserService _currentUserService;

    public ProfileController(IProfileService profileService, ICurrentUserService currentUserService)
    {
        _profileService = profileService;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetProfile(CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId == null)
        {
            return Unauthorized();
        }

        var profile = await _profileService.GetProfileAsync(userId.Value, cancellationToken);
        return Ok(profile);
    }

    [HttpPut]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId == null)
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation Error",
                Detail = "First Name and Last Name are required."
            });
        }

        var updated = await _profileService.UpdateProfileAsync(userId.Value, request, cancellationToken);
        return Ok(updated);
    }
}
