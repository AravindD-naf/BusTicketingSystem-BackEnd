using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BusTicketingSystem.Models
{
    public enum DiscountType { Percentage, Flat }

    public class PromoCode
    {
        [Key]
        public int PromoCodeId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        [Required]
        public DiscountType DiscountType { get; set; }

        /// <summary>Percentage value (e.g. 20 = 20%) or flat amount (e.g. 150 = ₹150)</summary>
        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal DiscountValue { get; set; }

        /// <summary>Maximum discount cap in rupees (0 = no cap)</summary>
        [Column(TypeName = "decimal(10,2)")]
        public decimal MaxDiscountAmount { get; set; } = 0;

        /// <summary>Minimum booking amount required to use this code</summary>
        [Column(TypeName = "decimal(10,2)")]
        public decimal MinBookingAmount { get; set; } = 0;

        public DateTime ValidFrom { get; set; } = DateTime.UtcNow;
        public DateTime ValidUntil { get; set; }

        /// <summary>0 = unlimited</summary>
        public int MaxUsageCount { get; set; } = 0;

        public int UsedCount { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
