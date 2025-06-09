using KiuWho.Services;
using Microsoft.AspNetCore.Mvc;

namespace KiuWho.Controllers;

[ApiController]
[Route("sticker")]
public class StickerController : ControllerBase
{
    private readonly StickerServices _stickerServices;
    private readonly ChatServices _chatServices;
    private readonly ILogger<StickerController> _logger;

    public StickerController(
        StickerServices stickerServices,
        ChatServices chatServices,
        ILogger<StickerController> logger)
    {
        _stickerServices = stickerServices;
        _chatServices = chatServices;
        _logger = logger;
    }
    
    [HttpGet("GetStickerPacks")]
    public async Task<IActionResult> GetStickerPacks(CancellationToken cancellationToken = default)
    {
        try
        {
            var stickerPacks = await _stickerServices.GetAllStickerPacks(cancellationToken);
            
            var result = stickerPacks.Select(pack => new
            {
                uid = pack.Uid,
                name = pack.Name,
                createAt = pack.CreateAt,
                isActive = pack.IsActive,
                stickerCount = pack.Stickers?.Count() ?? 0
            });

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sticker packs");
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    [HttpGet("GetStickersFromPack")]
    public async Task<IActionResult> GetStickersFromPack([FromQuery] Guid packId, CancellationToken cancellationToken = default)
    {
        try
        {
            var pack = await _stickerServices.GetStickerPack(packId, cancellationToken);
            
            if (pack == null)
            {
                return NotFound("Sticker pack not found");
            }

            var result = pack.Stickers
                .OrderBy(x => x.DisplayOrder)
                .Select(sticker => new
                {
                    uid = sticker.Uid,
                    stickerPackUid = sticker.StickerPackUid,
                    imageUrl = sticker.ImageUrl,
                    displayName = sticker.DisplayName,
                    displayOrder = sticker.DisplayOrder
                });

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting stickers from pack");
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    [HttpGet("GetSticker")]
    public async Task<IActionResult> GetSticker([FromQuery] Guid uid, CancellationToken cancellationToken = default)
    {
        try
        {
            var sticker = await _stickerServices.GetStickerById(uid, cancellationToken);

            if (sticker == null)
            {
                return NotFound("Sticker not found");
            }

            return Ok(new
            {
                uid = sticker.Uid,
                stickerPackUid = sticker.StickerPackUid,
                imageUrl = sticker.ImageUrl,
                displayName = sticker.DisplayName,
                displayOrder = sticker.DisplayOrder
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sticker");
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    [HttpGet("GetStats")]
    public IActionResult GetStats()
    {
        try
        {
            return Ok(new
            {
                activeConnections = _chatServices.GetActiveConnectionsCount(),
                waitingUsers = _chatServices.GetWaitingUsersCount(),
                messagesLastHour = 0 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting stats");
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    [HttpPost("PopulateStickers")]
    public async Task<IActionResult> PopulateStickers(CancellationToken cancellationToken = default)
    {
        var (success, message) = await _stickerServices.PopulateStickersFromFolders(cancellationToken);

        if (!success)
        {
            return BadRequest(message);
        }

        return Ok(message);
    }

    [HttpDelete("DeleteStickerPack/{packId}")]
    public async Task<IActionResult> DeleteStickerPack(Guid packId, CancellationToken cancellationToken = default)
    {
        var (success, message, stickerCount) = await _stickerServices.DeleteStickerPack(packId, cancellationToken);

        if (!success)
        {
            return NotFound(message);
        }

        return Ok(message);
    }

    [HttpDelete("DeleteAllStickerPacks")]
    public async Task<IActionResult> DeleteAllStickerPacks(CancellationToken cancellationToken = default)
    {
        var (success, message, packCount, stickerCount) = await _stickerServices.DeleteAllStickerPacks(cancellationToken);

        if (!success)
        {
            return StatusCode(500, message);
        }

        return Ok(message);
    }

    [HttpGet("StickerInfo")]
    public async Task<IActionResult> GetStickerInfo(CancellationToken cancellationToken = default)
    {
        var (success, message) = await _stickerServices.LogStickerInfo(cancellationToken);

        if (!success)
        {
            return StatusCode(500, message);
        }

        return Ok(message);
    }
}