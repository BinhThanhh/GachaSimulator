using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.ComponentModel.DataAnnotations;

namespace GachaSimulator.Models
{
    public class Items
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; } = string.Empty;
        public int Rarity { get; set; }
        public ItemType Type { get; set; }
        public ElementType? Element { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
    }
}
