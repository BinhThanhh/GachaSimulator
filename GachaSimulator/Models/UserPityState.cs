namespace GachaSimulator.Models;

public class UserPityState
{
    public int Id { get; set; }
    public BannerType BannerType { get; set; }
    public int CurrentPity5 { get; set; }
    public int CurrentPity4 { get; set; } 
    public bool IsGuaranteed { get; set; }
}