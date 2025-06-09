using KiuWho.Entities;
using Microsoft.EntityFrameworkCore;

namespace KiuWho.Context;

public class ChatContext : DbContext
{
    public ChatContext(DbContextOptions<ChatContext> options)
        : base(options)
    {
    }

    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StickerPack>()
            .HasMany(x => x.Stickers)
            .WithOne(x => x.StickerPack)
            .HasForeignKey(x => x.StickerPackUid);
    }
    
    public DbSet<StickerPack> StickerPacks { get; set; }
    public DbSet<Sticker> Stickers { get; set; }
    public DbSet<Ads> Ads { get; set; }
}