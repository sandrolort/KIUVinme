using KiuWho.Context;
using KiuWho.DTO;
using KiuWho.Entities;
using Microsoft.EntityFrameworkCore;

namespace KiuWho.Services;

public class MediaServices
{
    private readonly ChatContext _context;
    private readonly ILogger<MediaServices> _logger;

    public MediaServices(ChatContext context, ILogger<MediaServices> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Ads?> GetRandomAd(CancellationToken cancellationToken = default)
    {
        var activeAds = await _context.Ads
            .Where(x => x.isActive)
            .ToListAsync(cancellationToken);

        if (!activeAds.Any())
            return null;

        var randomAd = activeAds[new Random().Next(activeAds.Count)];
        
        randomAd.ImpressionCount++;
        await _context.SaveChangesAsync(cancellationToken);

        return randomAd;
    }

    public async Task<IEnumerable<Ads>> GetAllActiveAds(CancellationToken cancellationToken = default)
    {
        return await _context.Ads
            .Where(a => a.isActive)
            .OrderByDescending(a => a.ImpressionCount)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Ads>> GetAllAds(CancellationToken cancellationToken = default)
    {
        return await _context.Ads
            .OrderByDescending(a => a.isActive)
            .ThenByDescending(a => a.ImpressionCount)
            .ToListAsync(cancellationToken);
    }

    public async Task<(bool Success, string Message, Ads? Ad)> AddAd(string imageUrl, bool isUserCreated = false, bool isActive = true, CancellationToken cancellationToken = default)
    {
        try
        {
            var existingAd = await _context.Ads
                .FirstOrDefaultAsync(a => a.ImageUrl == imageUrl, cancellationToken);

            if (existingAd != null)
            {
                return (false, "Ad with this URL already exists", null);
            }

            var ad = new Ads
            {
                Uid = Guid.NewGuid(),
                ImageUrl = imageUrl,
                IsUserCreated = isUserCreated,
                isActive = isActive,
                ImpressionCount = 0
            };

            _context.Ads.Add(ad);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation($"Added new video ad: {imageUrl}");

            return (true, "Ad added successfully", ad);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding ad");
            return (false, $"Error adding ad: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message, bool? NewStatus)> ToggleAdStatus(Guid adId, CancellationToken cancellationToken = default)
    {
        try
        {
            var ad = await _context.Ads.FindAsync(new object[] { adId }, cancellationToken);
            if (ad == null)
            {
                return (false, "Ad not found", null);
            }

            ad.isActive = !ad.isActive;
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation($"Toggled ad status: {adId} -> {ad.isActive}");

            return (true, "Ad status toggled successfully", ad.isActive);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling ad status");
            return (false, $"Error toggling ad status: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message)> DeleteAd(Guid adId, CancellationToken cancellationToken = default)
    {
        try
        {
            var ad = await _context.Ads.FindAsync(new object[] { adId }, cancellationToken);
            if (ad == null)
            {
                return (false, "Ad not found");
            }

            _context.Ads.Remove(ad);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation($"Deleted ad: {adId}");

            return (true, "Ad deleted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting ad");
            return (false, $"Error deleting ad: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Message, int Count)> DeleteAllAds(CancellationToken cancellationToken = default)
    {
        try
        {
            var ads = await _context.Ads.ToListAsync(cancellationToken);
            var count = ads.Count;

            _context.Ads.RemoveRange(ads);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation($"Deleted all {count} ads");

            return (true, $"Successfully deleted {count} video ads", count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting all ads");
            return (false, $"Error deleting all ads: {ex.Message}", 0);
        }
    }

    public async Task<(bool Success, string Message, int? ImpressionCount)> RecordImpression(Guid adId, CancellationToken cancellationToken = default)
    {
        try
        {
            var ad = await _context.Ads.FindAsync(new object[] { adId }, cancellationToken);
            if (ad == null)
            {
                return (false, "Ad not found", null);
            }

            ad.ImpressionCount++;
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation($"Recorded impression for ad: {adId} (Total: {ad.ImpressionCount})");

            return (true, "Impression recorded successfully", ad.ImpressionCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording impression");
            return (false, $"Error recording impression: {ex.Message}", null);
        }
    }

    public (bool Success, string Message, IEnumerable<VideoFileInfo> Videos) GetVideosFromFolder()
    {
        try
        {
            var mediaPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "media");
            
            if (!Directory.Exists(mediaPath))
            {
                _logger.LogWarning($"Media directory not found: {mediaPath}");
                return (true, "Media directory not found", new List<VideoFileInfo>());
            }

            var videoFiles = Directory.GetFiles(mediaPath, "*.mp4", SearchOption.AllDirectories);
            
            var videos = videoFiles.Select(file => new VideoFileInfo
            {
                Name = Path.GetFileName(file),
                Url = "/media/" + Path.GetFileName(file),
                FileName = Path.GetFileName(file),
                FilePath = Path.GetRelativePath(Directory.GetCurrentDirectory(), file).Replace("\\", "/")
            }).OrderBy(v => v.FileName).ToList();

            _logger.LogInformation($"Found {videos.Count} video files in media folder");

            return (true, $"Found {videos.Count} videos", videos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting videos from media folder");
            return (false, $"Error getting videos: {ex.Message}", new List<VideoFileInfo>());
        }
    }
}
