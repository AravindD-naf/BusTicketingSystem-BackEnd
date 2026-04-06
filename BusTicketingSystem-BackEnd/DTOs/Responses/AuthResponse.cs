namespace BusTicketingSystem.DTOs.Responses
{
    public class AuthResponse
    {
        public string Token { get; set; } = string.Empty;
        public DateTime Expiry { get; set; }
    }
}