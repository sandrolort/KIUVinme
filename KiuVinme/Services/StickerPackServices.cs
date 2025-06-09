using KiuWho.Context;
using KiuWho.Entities;
using Microsoft.EntityFrameworkCore;

namespace KiuWho.Services;

public class StickerPackServices
{
    private readonly ChatContext _context;
    
    public StickerPackServices(ChatContext context)
    {
        this._context = context;
    }

    public async Task<IEnumerable<StickerPack>> GetAllStickerPacks(CancellationToken cancellationToken)
    {
        return await _context.StickerPacks
            .Include(x => x.Stickers)
            .ToListAsync(cancellationToken);
    }
}