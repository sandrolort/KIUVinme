using KiuWho.Context;
using KiuWho.Entities;
using Microsoft.EntityFrameworkCore;

namespace KiuWho.Services;

public class StickerServices
{
    private readonly ChatContext _context;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<StickerServices> _logger;

    public StickerServices(ChatContext context, IWebHostEnvironment environment, ILogger<StickerServices> logger)
    {
        _context = context;
        _environment = environment;
        _logger = logger;
    }

    public async Task<Sticker?> GetStickerById(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Stickers.FirstOrDefaultAsync(x => x.Uid == id, cancellationToken);
    }

    public async Task<IEnumerable<StickerPack>> GetAllStickerPacks(CancellationToken cancellationToken = default)
    {
        return await _context.StickerPacks
            .Include(x => x.Stickers)
            .Where(x => x.IsActive)
            .ToListAsync(cancellationToken);
    }

    public async Task<StickerPack?> GetStickerPack(Guid packId, CancellationToken cancellationToken = default)
    {
        return await _context.StickerPacks
            .Include(x => x.Stickers)
            .FirstOrDefaultAsync(x => x.Uid == packId && x.IsActive, cancellationToken);
    }

    public async Task<(bool Success, string Message)> PopulateStickersFromFolders(CancellationToken cancellationToken = default)
    {
        try
        {
            var stickerPath = Path.Combine(_environment.WebRootPath, "images", "stickers");
            _logger.LogInformation($"Looking for stickers in: {stickerPath}");
            
            if (!Directory.Exists(stickerPath))
            {
                var message = $"Stickers directory not found: {stickerPath}";
                _logger.LogError(message);
                return (false, message);
            }

            var packFolders = Directory.GetDirectories(stickerPath);
            _logger.LogInformation($"Found {packFolders.Length} pack folders");

            if (packFolders.Length == 0)
            {
                return (false, "No sticker pack folders found");
            }

            foreach (var packFolder in packFolders)
            {
                var packName = Path.GetFileName(packFolder);
                _logger.LogInformation($"Processing pack: {packName}");
                
                var existingPack = await _context.StickerPacks
                    .FirstOrDefaultAsync(p => p.Name == packName, cancellationToken);

                StickerPack pack;
                if (existingPack == null)
                {
                    pack = new StickerPack
                    {
                        Uid = Guid.NewGuid(),
                        Name = packName,
                        CreateAt = DateTime.UtcNow,
                        IsActive = true
                    };
                    
                    _context.StickerPacks.Add(pack);
                    await _context.SaveChangesAsync(cancellationToken);
                    
                    _logger.LogInformation($"Created sticker pack: {packName}");
                }
                else
                {
                    pack = existingPack;
                    _logger.LogInformation($"Using existing sticker pack: {packName}");
                }

                var allStickerFiles = new List<string>();
                
                // Check webp folder
                var webpSubfolder = Path.Combine(packFolder, "webp");
                if (Directory.Exists(webpSubfolder))
                {
                    var subfolderFiles = Directory.GetFiles(webpSubfolder, "*.webp");
                    if (subfolderFiles.Length > 0)
                    {
                        allStickerFiles.AddRange(subfolderFiles);
                        _logger.LogInformation($"Found {subfolderFiles.Length} files in webp subfolder");
                    }
                }

                // Sort files
                allStickerFiles.Sort();
                _logger.LogInformation($"Total files found in {packName}: {allStickerFiles.Count}");

                // Process each file
                var displayOrder = 1;
                foreach (var stickerFile in allStickerFiles)
                {
                    var fileName = Path.GetFileName(stickerFile);
                    var displayName = Path.GetFileNameWithoutExtension(fileName);
                    
                    // Determine correct URL based on actual file location
                    string imageUrl;
                    if (stickerFile.StartsWith(Path.Combine(packFolder, "webp")))
                    {
                        // File is in webp subfolder
                        imageUrl = $"/images/stickers/{packName}/webp/{fileName}";
                    }
                    else
                    {
                        // File is directly in pack folder
                        imageUrl = $"/images/stickers/{packName}/{fileName}";
                    }

                    // Check if sticker already exists
                    var existingSticker = await _context.Stickers
                        .FirstOrDefaultAsync(s => s.StickerPackUid == pack.Uid && s.ImageUrl == imageUrl, cancellationToken);

                    if (existingSticker == null)
                    {
                        var sticker = new Sticker
                        {
                            Uid = Guid.NewGuid(),
                            StickerPackUid = pack.Uid,
                            ImageUrl = imageUrl,
                            DisplayName = displayName,
                            DisplayOrder = displayOrder
                        };

                        _context.Stickers.Add(sticker);
                        _logger.LogInformation($"  Added sticker: {displayName} -> {imageUrl}");
                    }
                    else
                    {
                        _logger.LogInformation($"  Skipped existing sticker: {displayName}");
                    }

                    displayOrder++;
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
            
            var result = $"Successfully processed {packFolders.Length} folders.";
            _logger.LogInformation(result);
            
            return (true, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error populating stickers");
            return (false, $"Error populating stickers: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Message, int StickerCount)> DeleteStickerPack(Guid packId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation($"DeleteStickerPack called for pack: {packId}");
            
            var pack = await _context.StickerPacks
                .Include(p => p.Stickers)
                .FirstOrDefaultAsync(p => p.Uid == packId, cancellationToken);
                
            if (pack == null)
            {
                return (false, "Sticker pack not found", 0);
            }

            var stickerCount = pack.Stickers.Count();
            
            // Delete all stickers in the pack first
            _context.Stickers.RemoveRange(pack.Stickers);
            
            // Delete the pack
            _context.StickerPacks.Remove(pack);
            
            await _context.SaveChangesAsync(cancellationToken);
            
            var result = $"Successfully deleted sticker pack '{pack.Name}' and {stickerCount} stickers.";
            _logger.LogInformation(result);
            
            return (true, result, stickerCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting sticker pack");
            return (false, $"Error deleting sticker pack: {ex.Message}", 0);
        }
    }

    public async Task<(bool Success, string Message, int PackCount, int StickerCount)> DeleteAllStickerPacks(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("DeleteAllStickerPacks called");
            
            var allStickers = await _context.Stickers.ToListAsync(cancellationToken);
            var allPacks = await _context.StickerPacks.ToListAsync(cancellationToken);
            
            _context.Stickers.RemoveRange(allStickers);
            _context.StickerPacks.RemoveRange(allPacks);
            
            await _context.SaveChangesAsync(cancellationToken);
            
            var result = $"Successfully deleted all sticker packs ({allPacks.Count} packs, {allStickers.Count} stickers).";
            _logger.LogInformation(result);
            
            return (true, result, allPacks.Count, allStickers.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting all sticker packs");
            return (false, $"Error deleting all sticker packs: {ex.Message}", 0, 0);
        }
    }

    public async Task<(bool Success, string Message)> LogStickerInfo(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("StickerInfo called");
            
            var packs = await _context.StickerPacks
                .Include(p => p.Stickers)
                .ToListAsync(cancellationToken);

            _logger.LogInformation("\n=== STICKER PACK INFORMATION ===");
            foreach (var pack in packs)
            {
                _logger.LogInformation($"\nPack: {pack.Name} (ID: {pack.Uid})");
                _logger.LogInformation($"  Active: {pack.IsActive}");
                _logger.LogInformation($"  Stickers: {pack.Stickers.Count()}");
                
                foreach (var sticker in pack.Stickers.OrderBy(s => s.DisplayOrder))
                {
                    _logger.LogInformation($"    - {sticker.DisplayName} ({sticker.ImageUrl})");
                }
            }
            
            return (true, "Sticker info printed to console");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sticker info");
            return (false, $"Error getting sticker info: {ex.Message}");
        }
    }
}