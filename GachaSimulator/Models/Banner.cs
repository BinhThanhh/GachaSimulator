namespace GachaSimulator.Models;

public class Banner
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public BannerType Type { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}