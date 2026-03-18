using System;
using System.ComponentModel.DataAnnotations;

namespace BusTicketingSystem.Models
{
  
    public class ErrorLog
    {
        [Key]
        public int ErrorLogId { get; set; }

 
        public int? UserId { get; set; }


        [Required]
        [MaxLength(255)]
        public string ExceptionType { get; set; }


        [Required]
        [MaxLength(50)]
        public string ErrorCode { get; set; }


        [Required]
        [MaxLength(1000)]
        public string UserMessage { get; set; }


        [MaxLength(2000)]
        public string InternalMessage { get; set; }


        public string StackTrace { get; set; }


        [MaxLength(1000)]
        public string InnerExceptionMessage { get; set; }


        public int StatusCode { get; set; }


        [MaxLength(500)]
        public string RequestUrl { get; set; }


        [MaxLength(10)]
        public string HttpMethod { get; set; }


        public string RequestBody { get; set; }

  
        [MaxLength(50)]
        public string ClientIpAddress { get; set; }

    
        public string RequestHeaders { get; set; }

   
        [MaxLength(100)]
        public string TraceId { get; set; }

     
        public string ValidationErrors { get; set; }


        public string ContextData { get; set; }

    
        public bool IsHandled { get; set; } = true;

   
        public bool IsCritical { get; set; } = false;

  
        [MaxLength(20)]
        public string Severity { get; set; } = "Error";

  
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

  
        public DateTime? ResolvedAt { get; set; }

        [MaxLength(500)]
        public string ResolutionNotes { get; set; }
    }
}
