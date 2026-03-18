using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BusTicketingSystem.Models
{

    public class Passenger
    {
        [Key]
        public int PassengerId { get; set; }

        [Required]
        public int BookingId { get; set; }

        [ForeignKey("BookingId")]
        public Booking Booking { get; set; } = null!;

        [Required]
        public int SeatId { get; set; }

        [ForeignKey("SeatId")]
        public Seat Seat { get; set; } = null!;

    
        [Required]
        [MaxLength(10)]
        public string SeatNumber { get; set; } = string.Empty;

   
        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

  
        [Required]
        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

   
        [Required]
        [MaxLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

    
        [Required]
        [MaxLength(200)]
        public string Email { get; set; } = string.Empty;

    
        [MaxLength(50)]
        public string IdType { get; set; } = string.Empty;

  
        [MaxLength(50)]
        public string IdNumber { get; set; } = string.Empty;

     
        public int? Age { get; set; }


        [MaxLength(500)]
        public string SpecialRequirements { get; set; } = string.Empty;

    
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

  
        public DateTime? UpdatedAt { get; set; }

    
        public bool IsDeleted { get; set; } = false;
    }
}
