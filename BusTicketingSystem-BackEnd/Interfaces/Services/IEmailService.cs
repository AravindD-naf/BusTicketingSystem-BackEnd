namespace BusTicketingSystem.Interfaces.Services
{
    public interface IEmailService
    {
        Task SendBookingConfirmationAsync(
            string toEmail,
            string userName,
            string pnr,
            string source,
            string destination,
            DateTime travelDate,
            string departureTime,
            string arrivalTime,
            int numberOfSeats,
            decimal totalAmount,
            string? promoCode,
            decimal discountAmount);
    }
}
