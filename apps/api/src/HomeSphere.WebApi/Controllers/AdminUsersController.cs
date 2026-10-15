using HomeSphere.Application.Common.Interfaces;
using HomeSphere.Application.Common.Models;
using HomeSphere.Application.Features.Users.DTOs;
using HomeSphere.Domain.Entities;
using HomeSphere.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HomeSphere.WebApi.Controllers;

[ApiController]
[Authorize(Roles = Role.Admin)]
[Route("api/admin/users")]
[Produces("application/json")]
public class AdminUsersController : ControllerBase
{
    private readonly IApplicationDbContext _context;

    public AdminUsersController(IApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedList<UserProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLowerInvariant();
            query = query.Where(u => u.Email.Contains(s) || u.FirstName.ToLower().Contains(s) || u.LastName.ToLower().Contains(s));
        }

        var total = await query.CountAsync(cancellationToken);

        var users = await query
            .OrderByDescending(u => u.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new UserProfileDto(
                u.Id,
                u.Email,
                u.FirstName,
                u.LastName,
                u.FullName,
                u.PhoneNumber,
                u.UserRoles.Select(ur => ur.Role.Name).ToList(),
                u.CreatedAtUtc
            ))
            .ToListAsync(cancellationToken);

        var result = new PagedList<UserProfileDto>(users, total, page, pageSize);
        return Ok(result);
    }

    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ToggleUserStatus(Guid id, [FromBody] bool isActive, CancellationToken cancellationToken)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (user == null)
        {
            throw new NotFoundException("User", id);
        }

        user.IsActive = isActive;
        user.UpdatedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return NoContent();
    }
}
