using System.ComponentModel.DataAnnotations.Schema;

namespace GachaSimulator.Models;

public class BannerRateUp
{
    public int Id { get; set; }
    public int BannerId { get; set; }
    public int ItemId { get; set; }

    [ForeignKey("ItemId")]
    public virtual Items Item { get; set; } = null!;
}