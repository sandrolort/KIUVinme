using System.ComponentModel.DataAnnotations;

namespace KiuWho.Entities;

public class Ads
{
    [Key]
    public Guid Uid { get; set; }
    
    public bool IsUserCreated { get; set; }
    
    public string? ImageUrl { get; set; }
    
    public int ImpressionCount { get; set; }
    
    public bool isActive {get; set;}    
}