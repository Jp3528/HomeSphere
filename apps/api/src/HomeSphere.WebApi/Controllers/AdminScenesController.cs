using HomeSphere.Application.Features.Scenes;
using HomeSphere.Application.Features.Scenes.DTOs;
using HomeSphere.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeSphere.WebApi.Controllers;

[ApiController]
[Route("api/admin/scenes")]
[Authorize(Roles = $"{Role.Admin},{Role.Manager}")]
public class AdminScenesController : ControllerBase
{
    private readonly ISceneService _sceneService;

    public AdminScenesController(ISceneService sceneService)
    {
        _sceneService = sceneService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<RoomSceneListDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetScenes(
        [FromQuery] bool onlyActive = false,
        CancellationToken cancellationToken = default)
    {
        var scenes = await _sceneService.GetScenesAsync(onlyActive, cancellationToken);
        return Ok(scenes);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(RoomSceneDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var scene = await _sceneService.GetSceneByIdAsync(id, cancellationToken);
        return Ok(scene);
    }

    [HttpPost]
    [ProducesResponseType(typeof(RoomSceneDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateRoomSceneRequest request,
        CancellationToken cancellationToken)
    {
        var created = await _sceneService.CreateSceneAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(RoomSceneDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateRoomSceneRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await _sceneService.UpdateSceneAsync(id, request, cancellationToken);
        return Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _sceneService.DeleteSceneAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/hotspots")]
    [ProducesResponseType(typeof(SceneHotspotDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddHotspot(
        Guid id,
        [FromBody] CreateSceneHotspotRequest request,
        CancellationToken cancellationToken)
    {
        var hotspot = await _sceneService.AddHotspotAsync(id, request, cancellationToken);
        return Created(string.Empty, hotspot);
    }

    [HttpPut("{id:guid}/hotspots/{hotspotId:guid}")]
    [ProducesResponseType(typeof(SceneHotspotDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateHotspot(
        Guid id,
        Guid hotspotId,
        [FromBody] UpdateSceneHotspotRequest request,
        CancellationToken cancellationToken)
    {
        var hotspot = await _sceneService.UpdateHotspotAsync(id, hotspotId, request, cancellationToken);
        return Ok(hotspot);
    }

    [HttpDelete("{id:guid}/hotspots/{hotspotId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteHotspot(
        Guid id,
        Guid hotspotId,
        CancellationToken cancellationToken)
    {
        await _sceneService.DeleteHotspotAsync(id, hotspotId, cancellationToken);
        return NoContent();
    }
}
