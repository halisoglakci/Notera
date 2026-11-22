using Notera.Web.Models.AudD;

namespace Notera.Web.Services;

public interface IAudDService
{
    Task<AudDRecognitionResult> RecognizeAsync(
        Stream audioStream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default);
}