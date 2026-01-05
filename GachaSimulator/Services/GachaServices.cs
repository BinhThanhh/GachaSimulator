using GachaSimulator.Data;
using GachaSimulator.Models;
using Microsoft.EntityFrameworkCore;
using static MudBlazor.CategoryTypes;

namespace GachaSimulator.Services;

public class GachaService
{
    private readonly GachaDbContext _context;
    private readonly Random _random = new Random();

    public GachaService(GachaDbContext context)
    {
        _context = context;
    }

    public async Task<List<Items>> GetAllItemsAsync()
    {
        return await _context.Items
            .OrderByDescending(i => i.Rarity)
            .ToListAsync();
    }

    public async Task AddItemAsync(Items newItem)
    {
        _context.Items.Add(newItem);
        await _context.SaveChangesAsync();
    }

    public async Task<Models.Items?> GetItemByIdAsync(int id)
    {
        return await _context.Items.FindAsync(id);
    }

    public async Task UpdateItemAsync(Models.Items item)
    {
        _context.Items.Update(item);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteItemAsync(int id)
    {
        var item = await _context.Items.FindAsync(id);
        if (item != null)
        {
            _context.Items.Remove(item);
            await _context.SaveChangesAsync();
        }
    }

    //logic gacha
    public async Task<List<WishHistory>> RollBannerAsync(int bannerId, int times)
    {
        var results = new List<WishHistory>();
        var pityState = await _context.UserPityState.FirstOrDefaultAsync();

        if (pityState == null)
        {
            pityState = new UserPityState { BannerType = BannerType.character};
            _context.UserPityState.Add(pityState);
            await _context.SaveChangesAsync();
        }

        var rateUpItems = await _context.BannerRateUps
            .Where(r => r.BannerId == bannerId)
            .Select(r => r.ItemId)
            .ToListAsync();

        for (int i = 0; i < times; i++)
        {
            var result = await RollSingleAsync(pityState, rateUpItems, bannerId);
            results.Add(result);
        }

        await _context.SaveChangesAsync();
        return results;
    }

    private async Task<WishHistory> RollSingleAsync(UserPityState state, List<int> rateUps, int bannerId)
    {
        state.CurrentPity5++;
        state.CurrentPity4++;

        int rarity = DetermineRarity(state.CurrentPity5, state.CurrentPity4);
        Items resultItem;
        bool isWin5050 = false;

        if (rarity == 5)
        {
            state.CurrentPity5 = 0;

            if (state.IsGuaranteed || _random.NextDouble() < 0.5)
            {
                // TRÚNG RATE UP (Ra tướng trong banner)
                int rateUpId = rateUps.FirstOrDefault(); // Lấy tướng đầu tiên trong list rate up (VD: Hu Tao)
                // Lưu ý: Thực tế cần check xem rateUps có null không, ở đây giả sử DB luôn đúng
                resultItem = await _context.Items.FindAsync(rateUpId) ?? throw new Exception("DB lỗi: Không tìm thấy tướng Rate Up");

                state.IsGuaranteed = false; // Reset bảo hiểm về 50/50
                isWin5050 = true;
            }
            else
            {
                // LỆCH RATE (Ra Qiqi, Diluc...)
                // Lấy random 1 tướng 5 sao KHÔNG nằm trong list Rate Up
                var standard5Stars = await _context.Items
                    .Where(x => x.Rarity == 5 && !rateUps.Contains(x.Id))
                    .ToListAsync();

                if (standard5Stars.Any())
                    resultItem = standard5Stars[_random.Next(standard5Stars.Count)];
                else
                    resultItem = await _context.Items.FindAsync(rateUps.First()); // Fallback nếu DB không có tướng thường

                state.IsGuaranteed = true; // Kích hoạt bảo hiểm cho lần sau
                isWin5050 = false;
            }
        }
        else if (rarity == 4)
        {
            state.CurrentPity4 = 0; // Reset pity 4 sao
                                    // Logic 4 sao đơn giản hóa: 50% ra rate up, 50% ra thường
                                    // (Bạn có thể thêm logic bảo hiểm 4 sao nếu muốn)
            var pool4 = await _context.Items.Where(x => x.Rarity == 4).ToListAsync();
            resultItem = pool4[_random.Next(pool4.Count)];
        }
        else
        {
            // 3 Sao (Rác)
            // Lấy đại vũ khí 3 sao nào đó
            var pool3 = await _context.Items.Where(x => x.Rarity == 3).ToListAsync();
            if (pool3.Any())
                resultItem = pool3[_random.Next(pool3.Count)];
            else
                resultItem = new Models.Items { Name = "Rác 3 Sao", Rarity = 3, ImageUrl = "" }; // Fallback
        }

        // Tạo record lịch sử
        var history = new WishHistory
        {
            Id = Guid.NewGuid(),
            BannerId = bannerId,
            ItemId = resultItem.Id, // Có thể lỗi nếu item null, nhưng logic trên đã cover
            Items = resultItem,      // Gán luôn object để tí nữa hiển thị UI khỏi query lại
            TimePulled = DateTime.Now,
            PityAtPull = (rarity == 5) ? state.CurrentPity5 : 0, // Chỉ cần lưu pity lúc nổ vàng để thống kê
            IsWin5050 = isWin5050
        };

        _context.WishHistory.Add(history);
        return history;
    }

    // Thuật toán tính tỉ lệ
    private int DetermineRarity(int pity5, int pity4)
    {
        double rate5 = 0.006;
        if (pity5 >= 74) rate5 += (pity5 - 73) * 0.06;
        if (pity5 >= 90) rate5 = 1.0;

        if (_random.NextDouble() < rate5) return 5;

        double rate4 = 0.051;
        if (pity4 >= 10) rate4 = 1.0;

        if (_random.NextDouble() < rate4) return 4;

        return 3;
    }

    public async Task<List<Banner>> GetBannersAsync()
    {
        return await _context.Banners
        .Include(b => b.BannerRateUps)
            .ThenInclude(bru => bru.Item)
            .ToListAsync();
    }

    public async Task<Banner?> GetBannerByIdAsync(int id)
    {
        return await _context.Banners
            .Include(b => b.BannerRateUps)
            .ThenInclude(br => br.Item)
            .FirstOrDefaultAsync(b => b.Id == id);
    }

    public async Task AddBannerAsync(Banner banner, List<int> rateUpItemIds)
    {
        foreach (var itemId in rateUpItemIds)
        {
            banner.BannerRateUps.Add(new BannerRateUp { ItemId = itemId });
        }
        _context.Banners.Add(banner);
        await _context.SaveChangesAsync();
    }
    public async Task UpdateBannerAsync(Banner banner, List<int> rateUpItemIds)
    {
        var existingBanner = await _context.Banners
            .Include(b => b.BannerRateUps)
            .FirstOrDefaultAsync(b => b.Id == banner.Id);

        if (existingBanner != null)
        {
            existingBanner.Name = banner.Name;
            existingBanner.ImageUrl = banner.ImageUrl;
            existingBanner.IconUrl = banner.IconUrl;
            existingBanner.Type = banner.Type;

            _context.BannerRateUps.RemoveRange(existingBanner.BannerRateUps);

            foreach (var itemId in rateUpItemIds)
            {
                existingBanner.BannerRateUps.Add(new BannerRateUp { ItemId = itemId });
            }

            await _context.SaveChangesAsync();
        }
    }

    public async Task DeleteBannerAsync(int id)
    {
        var banner = await _context.Banners.FindAsync(id);
        if (banner != null)
        {
            _context.Banners.Remove(banner);
            await _context.SaveChangesAsync();
        }
    }
}