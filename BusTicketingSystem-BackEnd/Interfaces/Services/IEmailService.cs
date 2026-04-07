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
            List<string> seatNumbers,
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

        Task SendRefundStatusEmailAsync(
            string toEmail,
            string userName,
            string pnr,
            bool isApproved,
            decimal refundAmount,
            string reason);
    }
}
