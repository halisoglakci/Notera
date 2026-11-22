using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Notera.Web.Data;
using Notera.Web.Models.AudD;

namespace Notera.Web.Services;

public class AudDService : IAudDService
{
    private const string AudDEndpoint = "https://api.audd.io/";
    private readonly HttpClient _httpClient;
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AudDService> _logger;

    public AudDService(
        HttpClient httpClient,
        AppDbContext context,
        IConfiguration configuration,
        ILogger<AudDService> logger)
    {
        _httpClient = httpClient;
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<AudDRecognitionResult> RecognizeAsync(
        Stream audioStream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var apiToken = _configuration["AudD:ApiToken"];
        if (string.IsNullOrWhiteSpace(apiToken))
        {
            _logger.LogError("AudD:ApiToken User Secrets içerisinde tanımlı değil.");
            return new AudDRecognitionResult
            {
                Success = false,
                IsRecognized = false,
                Message = "Müzik tanıma servisi yapılandırması eksik."
            };
        }

        try
        {
            using var formData = new MultipartFormDataContent();
            formData.Add(new StringContent(apiToken), "api_token");

            var streamContent = new StreamContent(audioStream);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue(
                string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType);

            var safeFileName = string.IsNullOrWhiteSpace(fileName) ? "recording.webm" : fileName;
            formData.Add(streamContent, "file", safeFileName);

            using var response = await _httpClient.PostAsync(AudDEndpoint, formData, cancellationToken);
            var jsonString = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("AudD API HTTP hatası: {StatusCode}", response.StatusCode);
                return new AudDRecognitionResult
                {
                    Success = false,
                    IsRecognized = false,
                    Message = "Müzik tanıma servisinden geçerli yanıt alınamadı."
                };
            }

            using var document = JsonDocument.Parse(jsonString);
            var root = document.RootElement;

            var status = root.TryGetProperty("status", out var statusProp)
                ? statusProp.GetString()
                : null;

            if (!string.Equals(status, "success", StringComparison.OrdinalIgnoreCase))
            {
                var errorMessage = root.TryGetProperty("error", out var errorProp) &&
                                   errorProp.TryGetProperty("error_message", out var msgProp)
                    ? msgProp.GetString()
                    : "Bilinmeyen API hatası.";

                _logger.LogWarning("AudD API hata döndürdü: {ErrorMessage}", errorMessage);
                return new AudDRecognitionResult
                {
                    Success = false,
                    IsRecognized = false,
                    Message = "Müzik tanıma işlemi sırasında servis hatası oluştu."
                };
            }

            if (!root.TryGetProperty("result", out var resultProp) ||
                resultProp.ValueKind == JsonValueKind.Null)
            {
                return new AudDRecognitionResult
                {
                    Success = true,
                    IsRecognized = false,
                    Message = "Dinletilen ses kaydına ait şarkı tanımlanamadı. Daha net bir kayıt deneyin."
                };
            }

            var title = resultProp.TryGetProperty("title", out var titleProp) ? titleProp.GetString() : null;
            var artist = resultProp.TryGetProperty("artist", out var artistProp) ? artistProp.GetString() : null;
            var album = resultProp.TryGetProperty("album", out var albumProp) ? albumProp.GetString() : null;
            var songLink = resultProp.TryGetProperty("song_link", out var linkProp) ? linkProp.GetString() : null;

            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(artist))
            {
                return new AudDRecognitionResult
                {
                    Success = true,
                    IsRecognized = false,
                    Message = "Şarkı bilgileri eksik döndü, tekrar deneyin."
                };
            }

            // PostgreSQL Songs tablosunda büyük/küçük harf duyarsız eşleştirme
            var normalizedTitle = title.Trim().ToLower();
            var normalizedArtist = artist.Trim().ToLower();

            var matchedSong = await _context.Songs
                .AsNoTracking()
                .FirstOrDefaultAsync(s =>
                    s.Title.ToLower() == normalizedTitle &&
                    s.Artist.ToLower() == normalizedArtist,
                    cancellationToken);

            // Tam eşleşme yoksa kısmi başlık + sanatçı eşleşmesi kontrolü
            matchedSong ??= await _context.Songs
                .AsNoTracking()
                .FirstOrDefaultAsync(s =>
                    s.Title.ToLower().Contains(normalizedTitle) &&
                    s.Artist.ToLower().Contains(normalizedArtist),
                    cancellationToken);

            if (matchedSong != null)
            {
                return new AudDRecognitionResult
                {
                    Success = true,
                    IsRecognized = true,
                    Title = matchedSong.Title,
                    Artist = matchedSong.Artist,
                    Album = matchedSong.Album ?? album,
                    SongLink = matchedSong.YoutubeUrl ?? songLink,
                    MatchedSongId = matchedSong.Id,
                    Message = "Şarkı tanındı ve arşivde bulundu."
                };
            }

            return new AudDRecognitionResult
            {
                Success = true,
                IsRecognized = true,
                Title = title,
                Artist = artist,
                Album = album,
                SongLink = songLink,
                MatchedSongId = null,
                Message = $"Şarkı tanındı ({artist} - {title}), ancak arşivimizde bu şarkıya ait nota içeriği henüz mevcut değil."
            };
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("AudD API isteği zaman aşımına uğradı.");
            return new AudDRecognitionResult
            {
                Success = false,
                IsRecognized = false,
                Message = "Müzik tanıma servisi zaman aşımına uğradı. Lütfen tekrar deneyin."
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "AudD API bağlantı hatası.");
            return new AudDRecognitionResult
            {
                Success = false,
                IsRecognized = false,
                Message = "Müzik tanıma servisine şu anda bağlanılamıyor."
            };
        }
    }
}