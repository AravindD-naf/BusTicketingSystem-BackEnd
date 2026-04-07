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
            decimal baseFare,
            decimal discountAmount,
            decimal gstAmount,
            decimal convenienceFee,
            decimal grandTotal,
            string? promoCode);

        Task SendCancellationEmailAsync(
            string toEmail,
            string userName,
            string pnr,
            string source,
            string destination,
            DateTime travelDate,
            decimal amountPaid,
            decimal refundAmount,
            int refundPercentage,
            decimal cancellationFee,
            string cancellationReason);
    }
}
