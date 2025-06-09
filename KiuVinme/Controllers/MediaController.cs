using KiuWho.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace KiuWho.Controllers;

[ApiController]
[Route("media")]
public class MediaController : ControllerBase
{
    private readonly MediaServices _mediaServices;
    private readonly ILogger<MediaController> _logger;

    public MediaController(MediaServices mediaServices, ILogger<MediaController> logger)
    {
        _mediaServices = mediaServices;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> AddAd([FromBody] JsonElement request, CancellationToken cancellationToken = default)
    {
        if (!request.TryGetProperty("imageUrl", out var imageUrlElement) || 
            string.IsNullOrWhiteSpace(imageUrlElement.GetString()))
        {
            return BadRequest("imageUrl is required");
        }

        var imageUrl = imageUrlElement.GetString()!;
        var isUserCreated = request.TryGetProperty("isUserCreated", out var userCreatedElement) && 
                           userCreatedElement.GetBoolean();
        var isActive = !request.TryGetProperty("isActive", out var activeElement) || 
                      activeElement.GetBoolean(); // Default to true

        var (success, message, ad) = await _mediaServices.AddAd(
            imageUrl, 
            isUserCreated, 
            isActive, 
            cancellationToken);

        if (!success)
        {
            return BadRequest(message);
        }

        return Ok(new
        {
            uid = ad!.Uid,
            imageUrl = ad.ImageUrl,
            isUserCreated = ad.IsUserCreated,
            isActive = ad.isActive,
            impressionCount = ad.ImpressionCount
        });
    }

    [HttpGet("GetAllAds")]
    public async Task<IActionResult> GetAllAds(CancellationToken cancellationToken = default)
    {
        var ads = await _mediaServices.GetAllActiveAds(cancellationToken);

        var result = ads.Select(ad => new
        {
            uid = ad.Uid,
            imageUrl = ad.ImageUrl,
            isUserCreated = ad.IsUserCreated,
            isActive = ad.isActive,
            impressionCount = ad.ImpressionCount
        });

        return Ok(result);
    }

    [HttpGet("all")] 
    public async Task<IActionResult> GetAllAdsIncludingInactive(CancellationToken cancellationToken = default)
    {
        var ads = await _mediaServices.GetAllAds(cancellationToken);

        var result = ads.Select(ad => new
        {
            uid = ad.Uid,
            imageUrl = ad.ImageUrl,
            isUserCreated = ad.IsUserCreated,
            isActive = ad.isActive,
            impressionCount = ad.ImpressionCount
        });

        return Ok(result);
    }

    [HttpPatch("{adId}/toggle")]
    public async Task<IActionResult> ToggleAd(Guid adId, CancellationToken cancellationToken = default)
    {
        var (success, message, newStatus) = await _mediaServices.ToggleAdStatus(adId, cancellationToken);

        if (!success)
        {
            return NotFound(message);
        }

        return Ok(new { success = true, isActive = newStatus });
    }

    [HttpDelete("DeleteAd/{adId}")]
    public async Task<IActionResult> DeleteAd(Guid adId, CancellationToken cancellationToken = default)
    {
        var (success, message) = await _mediaServices.DeleteAd(adId, cancellationToken);

        if (!success)
        {
            return NotFound(message);
        }

        return Ok(new { message });
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteAllAds(CancellationToken cancellationToken = default)
    {
        var (success, message, count) = await _mediaServices.DeleteAllAds(cancellationToken);

        if (!success)
        {
            return StatusCode(500, message);
        }

        return Ok(new { message });
    }

    [HttpPost("{adId}/impression")]
    public async Task<IActionResult> RecordImpression(Guid adId, CancellationToken cancellationToken = default)
    {
        var (success, message, impressionCount) = await _mediaServices.RecordImpression(adId, cancellationToken);

        if (!success)
        {
            return NotFound(message);
        }

        return Ok(new { success = true, impressionCount });
    }

    [HttpGet("videos")]
    public IActionResult GetVideos()
    {
        var (success, message, videos) = _mediaServices.GetVideosFromFolder();

        if (!success)
        {
            return StatusCode(500, message);
        }

        return Ok(new { 
            videos = videos.Select(v => new
            {
                name = v.Name,
                url = v.Url,
                fileName = v.FileName,
                filePath = v.FilePath
            }),
            count = videos.Count(),
            message = videos.Any() ? $"Found {videos.Count()} videos" : "No videos found"
        });
    }

    [HttpGet("random")]
    public async Task<IActionResult> GetRandomAd(CancellationToken cancellationToken = default)
    {
        var ad = await _mediaServices.GetRandomAd(cancellationToken);

        if (ad == null)
        {
            return NotFound("No active ads available");
        }

        return Ok(new
        {
            uid = ad.Uid,
            imageUrl = ad.ImageUrl,
            isUserCreated = ad.IsUserCreated,
            isActive = ad.isActive,
            impressionCount = ad.ImpressionCount
        });
    }
}