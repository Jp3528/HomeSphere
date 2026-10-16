using HomeSphere.Application.Features.Scenes;
using HomeSphere.Application.Features.Scenes.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace HomeSphere.WebApi.Controllers;

[ApiController]
[Route("api/scenes")]
public class ScenesController : ControllerBase
{
    private readonly ISceneService _sceneService;

    public ScenesController(ISceneService sceneService)
    {
        _sceneService = sceneService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<RoomSceneListDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetScenes(
        [FromQuery] bool onlyActive = true,
        CancellationToken cancellationToken = default)
    {
        var scenes = await _sceneService.GetScenesAsync(onlyActive, cancellationToken);
        return Ok(scenes);
    }

    [HttpGet("{slug}")]
    [ProducesResponseType(typeof(RoomSceneDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBySlug(
        string slug,
        CancellationToken cancellationToken)
    {
        var scene = await _sceneService.GetSceneBySlugAsync(slug, onlyActive: true, cancellationToken);
        return Ok(scene);
    }
}
