namespace GachaSimulator.Models;

public class UserPityState
{
    public int Id { get; set; }
    public BannerType BannerType { get; set; } // Map với Enum
    public int CurrentPity5 { get; set; }      // Đếm 5 sao
    public int CurrentPity4 { get; set; }      // Đếm 4 sao
    public bool IsGuaranteed { get; set; }     // Có bảo hiểm không
}