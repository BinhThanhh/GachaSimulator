namespace GachaSimulator.Models;

public class Banner
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public BannerType Type { get; set; }
    public string ImageUrl { get; set; } = "";
    public string IconUrl { get; set; } = "";
    public virtual ICollection<BannerRateUp> BannerRateUps { get; set; } = new List<BannerRateUp>();
}