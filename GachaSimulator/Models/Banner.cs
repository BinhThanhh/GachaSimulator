namespace GachaSimulator.Models;

public class Banner
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public BannerType Type { get; set; }

    public string ImageUrl { get; set; } = ""; // Ảnh Banner to (Chữ nhật)
    public string IconUrl { get; set; } = "";  // Ảnh Icon tròn (Để bấm chọn)
}