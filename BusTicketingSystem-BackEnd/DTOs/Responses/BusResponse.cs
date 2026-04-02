namespace BusTicketingSystem.DTOs.Responses
{
    public class BusResponse
    {
        public int BusId { get; set; }
        public string BusNumber { get; set; } = string.Empty;
        public string BusType { get; set; } = string.Empty;
        public int TotalSeats { get; set; }
        public string OperatorName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}