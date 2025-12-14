using GachaSimulator.Models;

namespace GachaSimulator.Models
{
    public class WishHistory
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public int ItemId { get; set; }
        public Items Items { get; set; } = null!;
        public int BannerId { get; set; }
        public DateTime TimePulled { get; set; } = DateTime.Now;
        public int PityAtPull { get; set; }
        public bool IsWin5050 { get; set; }
    }
}
