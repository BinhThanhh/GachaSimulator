using Microsoft.EntityFrameworkCore;
using GachaSimulator.Data;
using GachaSimulator.Models;

namespace GachaSimulator.Services;

public class GachaService
{
    private readonly GachaDbContext _context;

    // Tiêm DbContext vào để dùng
    public GachaService(GachaDbContext context)
    {
        _context = context;
    }

    // Hàm 1: Lấy toàn bộ danh sách Items
    public async Task<List<Items>> GetAllItemsAsync()
    {
        // Lấy danh sách, sắp xếp theo số sao giảm dần (5 sao lên đầu)
        return await _context.Items
            .OrderByDescending(i => i.Rarity)
            .ToListAsync();
    }
}