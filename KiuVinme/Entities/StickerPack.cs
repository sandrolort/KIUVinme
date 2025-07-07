﻿using System.ComponentModel.DataAnnotations;

namespace KiuWho.Entities;

public class StickerPack
{
    [Key]
    public Guid Uid { get; set; }
    
    public required string Name { get; set; }
    
    public DateTime CreateAt { get; set; } = DateTime.Now;
    
    public bool IsActive {get; set;}

    public ICollection<Sticker> Stickers { get; set; } = new List<Sticker>();
}