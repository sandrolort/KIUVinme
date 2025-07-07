using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KiuWho.Entities;

public class Sticker
{ 
    [Key]
    public Guid Uid { get; set; }
    
    [ForeignKey(nameof(StickerPack))]
    public Guid StickerPackUid { get; set; }
    
    public string ImageUrl { get; set; } = null!;
    
    public string DisplayName { get; set; } = null!;
    
    public int DisplayOrder { get; set; }
    public StickerPack StickerPack { get; set; } = null!;
}