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
        
        // Lấy banner để biết banner type
        var banner = await _context.Banners.FirstOrDefaultAsync(b => b.Id == bannerId);
        if (banner == null)
        {
            throw new Exception($"Không tìm thấy banner với ID: {bannerId}");
        }

        // Lấy hoặc tạo pity state theo banner type
        var pityState = await _context.UserPityState
            .FirstOrDefaultAsync(p => p.BannerType == banner.Type);

        if (pityState == null)
        {
            pityState = new UserPityState { BannerType = banner.Type };
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

    public async Task<int> GetTotalRollsAsync(int bannerId)
    {
        return await _context.WishHistory
            .CountAsync(w => w.BannerId == bannerId);
    }

    public async Task<(int pity5, int pity4, bool isGuaranteed)> GetCurrentPityStateAsync(int bannerId)
    {
        var banner = await _context.Banners.FirstOrDefaultAsync(b => b.Id == bannerId);
        if (banner == null)
        {
            return (0, 0, false);
        }

        var pityState = await _context.UserPityState
            .FirstOrDefaultAsync(p => p.BannerType == banner.Type);

        if (pityState == null)
        {
            return (0, 0, false);
        }

        return (pityState.CurrentPity5, pityState.CurrentPity4, pityState.IsGuaranteed);
    }

    private async Task<WishHistory> RollSingleAsync(UserPityState state, List<int> rateUps, int bannerId)
    {
        state.CurrentPity5++;
        state.CurrentPity4++;

        // Lưu pity trước khi reset để lưu vào lịch sử
        int pityAtPull = state.CurrentPity5;
        
        int rarity = DetermineRarity(state.CurrentPity5, state.CurrentPity4);
        Items resultItem;
        bool isWin5050 = false;

        if (rarity == 5)
        {
            // Reset pity 5 sao
            state.CurrentPity5 = 0;

            // Kiểm tra có rate-up items không
            var rateUp5Stars = rateUps.Any() 
                ? await _context.Items
                    .Where(x => x.Rarity == 5 && rateUps.Contains(x.Id))
                    .ToListAsync()
                : new List<Items>();

            if (state.IsGuaranteed || (_random.NextDouble() < 0.5 && rateUp5Stars.Any()))
            {
                // TRÚNG RATE UP (Ra tướng trong banner)
                if (rateUp5Stars.Any())
                {
                    resultItem = rateUp5Stars[_random.Next(rateUp5Stars.Count)];
                    state.IsGuaranteed = false; // Reset bảo hiểm về 50/50
                    isWin5050 = true;
                }
                else
                {
                    // Nếu không có rate-up 5 sao, lấy random 5 sao bất kỳ
                    var all5Stars = await _context.Items.Where(x => x.Rarity == 5).ToListAsync();
                    if (all5Stars.Any())
                    {
                        resultItem = all5Stars[_random.Next(all5Stars.Count)];
                        state.IsGuaranteed = false;
                        isWin5050 = true;
                    }
                    else
                    {
                        throw new Exception("Không có item 5 sao trong database");
                    }
                }
            }
            else
            {
                // LỆCH RATE (Ra tướng standard)
                var standard5Stars = await _context.Items
                    .Where(x => x.Rarity == 5 && (rateUps.Count == 0 || !rateUps.Contains(x.Id)))
                    .ToListAsync();

                if (standard5Stars.Any())
                {
                    resultItem = standard5Stars[_random.Next(standard5Stars.Count)];
                }
                else if (rateUp5Stars.Any())
                {
                    // Fallback: nếu không có standard, lấy rate-up
                    resultItem = rateUp5Stars[_random.Next(rateUp5Stars.Count)];
                }
                else
                {
                    throw new Exception("Không có item 5 sao trong database");
                }

                state.IsGuaranteed = true; // Kích hoạt bảo hiểm cho lần sau
                isWin5050 = false;
            }
        }
        else if (rarity == 4)
        {
            state.CurrentPity4 = 0; // Reset pity 4 sao
            
            // Logic 4 sao: ưu tiên rate-up
            var rateUp4Stars = rateUps.Any()
                ? await _context.Items
                    .Where(x => x.Rarity == 4 && rateUps.Contains(x.Id))
                    .ToListAsync()
                : new List<Items>();

            var all4Stars = await _context.Items.Where(x => x.Rarity == 4).ToListAsync();
            
            if (all4Stars.Any())
            {
                // 50% rate-up nếu có, 50% random
                if (rateUp4Stars.Any() && _random.NextDouble() < 0.5)
                {
                    resultItem = rateUp4Stars[_random.Next(rateUp4Stars.Count)];
                }
                else
                {
                    resultItem = all4Stars[_random.Next(all4Stars.Count)];
                }
            }
            else
            {
                throw new Exception("Không có item 4 sao trong database");
            }
        }
        else
        {
            // 3 Sao
            var pool3 = await _context.Items.Where(x => x.Rarity == 3).ToListAsync();
            if (pool3.Any())
            {
                resultItem = pool3[_random.Next(pool3.Count)];
            }
            else
            {
                // Tạo fallback item và lưu vào DB nếu chưa có
                resultItem = new Items 
                { 
                    Name = "Item 3 Sao", 
                    Rarity = 3, 
                    ImageUrl = "",
                    Type = ItemType.weapon
                };
                _context.Items.Add(resultItem);
                await _context.SaveChangesAsync();
            }
        }

        // Tạo record lịch sử
        var history = new WishHistory
        {
            Id = Guid.NewGuid(),
            BannerId = bannerId,
            ItemId = resultItem.Id,
            Items = resultItem,
            TimePulled = DateTime.Now,
            PityAtPull = (rarity == 5) ? pityAtPull : 0, // Lưu pity lúc nổ 5 sao
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

    public async Task ResetPityAsync(int bannerId)
    {
        var banner = await _context.Banners.FirstOrDefaultAsync(b => b.Id == bannerId);
        if (banner == null)
        {
            throw new Exception($"Không tìm thấy banner với ID: {bannerId}");
        }

        var pityState = await _context.UserPityState
            .FirstOrDefaultAsync(p => p.BannerType == banner.Type);

        if (pityState != null)
        {
            pityState.CurrentPity5 = 0;
            pityState.CurrentPity4 = 0;
            pityState.IsGuaranteed = false;
            await _context.SaveChangesAsync();
        }
    }
}