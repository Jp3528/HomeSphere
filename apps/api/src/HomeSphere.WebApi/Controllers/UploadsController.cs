using HomeSphere.Application.Common.Interfaces;
using HomeSphere.Domain.Entities;
using HomeSphere.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeSphere.WebApi.Controllers;

[ApiController]
[Route("api/admin/uploads")]
[Authorize(Roles = $"{Role.Admin},{Role.Manager}")]
public class UploadsController : ControllerBase
{
    private readonly IFileStorageService _fileStorageService;

    public UploadsController(IFileStorageService fileStorageService)
    {
        _fileStorageService = fileStorageService;
    }

    [HttpPost]
    [RequestSizeLimit(10 * 1024 * 1024)] // 10MB
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Upload(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            throw new BusinessRuleValidationException("No file was uploaded.");
        }

        using var stream = file.OpenReadStream();
        var url = await _fileStorageService.UploadFileAsync(stream, file.FileName, file.ContentType, cancellationToken);

        return Ok(new { url });
    }
}
