namespace BusTicketingSystem.DTOs.Responses
{
    public class RouteResponseDto
    {
        public int RouteId { get; set; }
        public string Source { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        
        public decimal Distance { get; set; }

        public int EstimatedTravelTimeMinutes { get; set; }
        public string FormattedDuration
        {
            get
            {
                int h = EstimatedTravelTimeMinutes / 60;
                int m = EstimatedTravelTimeMinutes % 60;
                if (h == 0) return $"{m}m";
                if (m == 0) return $"{h}h";
                return $"{h}h {m}m";
            }
        }


        public decimal BaseFare { get; set; }

        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
