using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Notera.Web.Models.AudD;
using Notera.Web.Services;

namespace Notera.Web.Controllers;

[ApiController]
[Route("api/recognition")]
public class RecognitionController : ControllerBase
{
    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".webm", ".ogg", ".wav", ".mp3", ".m4a", ".aac", ".mp4"
    };

    private readonly IAudDService _audDService;

    public RecognitionController(IAudDService audDService)
    {
        _audDService = audDService;
    }

    // POST: /api/recognition/identify
    [HttpPost("identify")]
    [EnableRateLimiting("AudDPolicy")]
    [RequestSizeLimit(MaxFileSizeBytes)]
    public async Task<ActionResult<AudDRecognitionResult>> Identify(
        IFormFile? audioFile,
        CancellationToken cancellationToken)
    {
        if (audioFile == null || audioFile.Length == 0)
        {
            return BadRequest(new AudDRecognitionResult
            {
                Success = false,
                IsRecognized = false,
                Message = "Geçerli bir ses kaydı gönderilmedi."
            });
        }

        if (audioFile.Length > MaxFileSizeBytes)
        {
            return BadRequest(new AudDRecognitionResult
            {
                Success = false,
                IsRecognized = false,
                Message = "Ses kaydı boyutu 10 MB sınırını aşamaz."
            });
        }

        var extension = Path.GetExtension(audioFile.FileName);
        if (!string.IsNullOrWhiteSpace(extension) && !AllowedExtensions.Contains(extension))
        {
            return BadRequest(new AudDRecognitionResult
            {
                Success = false,
                IsRecognized = false,
                Message = "Desteklenmeyen ses dosyası formatı."
            });
        }

        await using var stream = audioFile.OpenReadStream();
        var result = await _audDService.RecognizeAsync(
            stream,
            audioFile.FileName,
            audioFile.ContentType,
            cancellationToken);

        if (!result.Success)
        {
            return StatusCode(StatusCodes.Status502BadGateway, result);
        }

        return Ok(result);
    }
}