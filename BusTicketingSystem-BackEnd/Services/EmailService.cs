using BusTicketingSystem.Interfaces.Services;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace BusTicketingSystem.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration config, ILogger<EmailService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task SendBookingConfirmationAsync(
            string toEmail, string userName, string pnr,
            string source, string destination, DateTime travelDate,
            string departureTime, string arrivalTime, int numberOfSeats,
            decimal baseFare, decimal discountAmount, decimal gstAmount,
            decimal convenienceFee, decimal grandTotal, string? promoCode)
        {
            try
            {
                var body = BuildConfirmationBody(userName, pnr, source, destination, travelDate,
                    departureTime, arrivalTime, numberOfSeats, baseFare, discountAmount,
                    gstAmount, convenienceFee, grandTotal, promoCode);
                await SendEmailAsync(toEmail, userName, $"Booking Confirmed - PNR: {pnr}", body);
                _logger.LogInformation("Booking confirmation email sent to {Email} for PNR {PNR}", toEmail, pnr);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send booking confirmation email to {Email} for PNR {PNR}", toEmail, pnr);
            }
        }

        public async Task SendCancellationEmailAsync(
            string toEmail, string userName, string pnr,
            string source, string destination, DateTime travelDate,
            decimal amountPaid, decimal refundAmount, int refundPercentage,
            decimal cancellationFee, string cancellationReason)
        {
            try
            {
                var body = BuildCancellationBody(userName, pnr, source, destination, travelDate,
                    amountPaid, refundAmount, refundPercentage, cancellationFee, cancellationReason);
                await SendEmailAsync(toEmail, userName, $"Booking Cancelled - PNR: {pnr}", body);
                _logger.LogInformation("Cancellation email sent to {Email} for PNR {PNR}", toEmail, pnr);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send cancellation email to {Email} for PNR {PNR}", toEmail, pnr);
            }
        }

        private async Task SendEmailAsync(string toEmail, string toName, string subject, string htmlBody)
        {
            var s        = _config.GetSection("SmtpSettings");
            var host     = s["Host"]!;
            var port     = int.Parse(s["Port"]!);
            var username = s["Username"]!;
            var password = s["Password"]!;
            var fromEmail = s["FromEmail"]!;
            var fromName  = s["FromName"] ?? "Bus Ticketing System";

            var msg = new MimeMessage();
            msg.From.Add(new MailboxAddress(fromName, fromEmail));
            msg.To.Add(new MailboxAddress(toName, toEmail));
            msg.Subject = subject;
            msg.Body = new TextPart("html") { Text = htmlBody };

            using var client = new SmtpClient();
            await client.ConnectAsync(host, port, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(username, password);
            await client.SendAsync(msg);
            await client.DisconnectAsync(true);
        }

        private static string BuildConfirmationBody(
            string userName, string pnr, string source, string destination,
            DateTime travelDate, string departureTime, string arrivalTime,
            int numberOfSeats, decimal baseFare, decimal discountAmount,
            decimal gstAmount, decimal convenienceFee, decimal grandTotal, string? promoCode)
        {
            decimal fareAfterDiscount = baseFare - discountAmount;
            var promoRow = !string.IsNullOrEmpty(promoCode) && discountAmount > 0
                ? $"<tr><td style='padding:8px;color:#555;'>Promo Discount ({promoCode})</td><td style='padding:8px;font-weight:bold;color:#2e7d32;'>- &#8377;{discountAmount:F2}</td></tr>"
                : string.Empty;

            return $@"<!DOCTYPE html><html><head><meta charset='utf-8'/></head>
<body style='font-family:Arial,sans-serif;background:#f4f4f4;margin:0;padding:0;'>
<div style='max-width:600px;margin:30px auto;background:#fff;border-radius:8px;overflow:hidden;box-shadow:0 2px 8px rgba(0,0,0,0.1);'>
  <div style='background:#1a73e8;padding:24px;text-align:center;'>
    <h1 style='color:#fff;margin:0;font-size:22px;'>Booking Confirmed!</h1>
  </div>
  <div style='padding:24px;'>
    <p style='font-size:16px;color:#333;'>Hi <strong>{userName}</strong>,</p>
    <p style='color:#555;'>Your bus ticket has been booked successfully. Here are your trip details:</p>
    <div style='background:#f9f9f9;border-radius:6px;padding:16px;margin:20px 0;'>
      <table style='width:100%;border-collapse:collapse;'>
        <tr style='background:#e8f0fe;'><td style='padding:8px;color:#555;'>PNR</td><td style='padding:8px;font-weight:bold;font-size:18px;color:#1a73e8;'>{pnr}</td></tr>
        <tr><td style='padding:8px;color:#555;'>From</td><td style='padding:8px;font-weight:bold;'>{source}</td></tr>
        <tr style='background:#f0f0f0;'><td style='padding:8px;color:#555;'>To</td><td style='padding:8px;font-weight:bold;'>{destination}</td></tr>
        <tr><td style='padding:8px;color:#555;'>Travel Date</td><td style='padding:8px;font-weight:bold;'>{travelDate:dddd, MMMM dd yyyy}</td></tr>
        <tr style='background:#f0f0f0;'><td style='padding:8px;color:#555;'>Departure</td><td style='padding:8px;font-weight:bold;'>{departureTime}</td></tr>
        <tr><td style='padding:8px;color:#555;'>Arrival</td><td style='padding:8px;font-weight:bold;'>{arrivalTime}</td></tr>
        <tr style='background:#f0f0f0;'><td style='padding:8px;color:#555;'>Seats</td><td style='padding:8px;font-weight:bold;'>{numberOfSeats}</td></tr>
      </table>
    </div>
    <p style='color:#333;font-weight:bold;margin-bottom:4px;'>Price Breakdown</p>
    <div style='background:#f9f9f9;border-radius:6px;padding:16px;margin-bottom:20px;'>
      <table style='width:100%;border-collapse:collapse;'>
        <tr><td style='padding:8px;color:#555;'>Base Fare</td><td style='padding:8px;'>&#8377;{baseFare:F2}</td></tr>
        {promoRow}
        <tr style='background:#f0f0f0;'><td style='padding:8px;color:#555;'>Fare after Discount</td><td style='padding:8px;'>&#8377;{fareAfterDiscount:F2}</td></tr>
        <tr><td style='padding:8px;color:#555;'>GST (6%)</td><td style='padding:8px;'>&#8377;{gstAmount:F2}</td></tr>
        <tr style='background:#f0f0f0;'><td style='padding:8px;color:#555;'>Convenience Fee</td><td style='padding:8px;'>&#8377;{convenienceFee:F2}</td></tr>
        <tr style='background:#e8f0fe;'><td style='padding:8px;color:#555;font-weight:bold;'>Total Paid</td><td style='padding:8px;font-weight:bold;font-size:16px;color:#1a73e8;'>&#8377;{grandTotal:F2}</td></tr>
      </table>
    </div>
    <p style='color:#555;font-size:14px;'>Please carry a valid ID proof during your journey. Arrive at the boarding point at least 15 minutes before departure.</p>
    <p style='color:#999;font-size:12px;margin-top:24px;'>This is an automated email. Please do not reply to this message.</p>
  </div>
  <div style='background:#f4f4f4;padding:12px;text-align:center;'>
    <p style='color:#aaa;font-size:12px;margin:0;'>&#169; {DateTime.UtcNow.Year} Bus Ticketing System. All rights reserved.</p>
  </div>
</div>
</body></html>";
        }

        private static string BuildCancellationBody(
            string userName, string pnr, string source, string destination,
            DateTime travelDate, decimal amountPaid, decimal refundAmount,
            int refundPercentage, decimal cancellationFee, string cancellationReason)
        {
            string refundRows = refundAmount > 0
                ? $@"<tr style='background:#e8f5e9;'><td style='padding:8px;color:#555;'>Refund Amount</td><td style='padding:8px;font-weight:bold;color:#2e7d32;'>&#8377;{refundAmount:F2} ({refundPercentage}%)</td></tr>
        <tr><td style='padding:8px;color:#555;'>Cancellation Fee</td><td style='padding:8px;color:#c62828;'>&#8377;{cancellationFee:F2}</td></tr>
        <tr style='background:#f0f0f0;'><td colspan='2' style='padding:8px;color:#555;font-size:13px;'>The refund of <strong>&#8377;{refundAmount:F2}</strong> will be credited to your wallet shortly.</td></tr>"
                : $@"<tr style='background:#fff3e0;'><td style='padding:8px;color:#555;'>Refund Amount</td><td style='padding:8px;font-weight:bold;color:#e65100;'>&#8377;0.00 (0%)</td></tr>
        <tr><td colspan='2' style='padding:8px;color:#555;font-size:13px;'>As per our cancellation policy, no refund is applicable for cancellations made within the non-refundable window.</td></tr>";

            var reasonRow = !string.IsNullOrWhiteSpace(cancellationReason)
                ? $"<tr><td style='padding:8px;color:#555;'>Reason</td><td style='padding:8px;'>{cancellationReason}</td></tr>"
                : string.Empty;

            return $@"<!DOCTYPE html><html><head><meta charset='utf-8'/></head>
<body style='font-family:Arial,sans-serif;background:#f4f4f4;margin:0;padding:0;'>
<div style='max-width:600px;margin:30px auto;background:#fff;border-radius:8px;overflow:hidden;box-shadow:0 2px 8px rgba(0,0,0,0.1);'>
  <div style='background:#d32f2f;padding:24px;text-align:center;'>
    <h1 style='color:#fff;margin:0;font-size:22px;'>Booking Cancelled</h1>
  </div>
  <div style='padding:24px;'>
    <p style='font-size:16px;color:#333;'>Hi <strong>{userName}</strong>,</p>
    <p style='color:#555;'>Your booking has been cancelled. Here is a summary:</p>
    <div style='background:#f9f9f9;border-radius:6px;padding:16px;margin:20px 0;'>
      <table style='width:100%;border-collapse:collapse;'>
        <tr style='background:#fce4e4;'><td style='padding:8px;color:#555;'>PNR</td><td style='padding:8px;font-weight:bold;font-size:18px;color:#d32f2f;'>{pnr}</td></tr>
        <tr><td style='padding:8px;color:#555;'>From</td><td style='padding:8px;font-weight:bold;'>{source}</td></tr>
        <tr style='background:#f0f0f0;'><td style='padding:8px;color:#555;'>To</td><td style='padding:8px;font-weight:bold;'>{destination}</td></tr>
        <tr><td style='padding:8px;color:#555;'>Travel Date</td><td style='padding:8px;font-weight:bold;'>{travelDate:dddd, MMMM dd yyyy}</td></tr>
        <tr style='background:#f0f0f0;'><td style='padding:8px;color:#555;'>Amount Paid</td><td style='padding:8px;font-weight:bold;'>&#8377;{amountPaid:F2}</td></tr>
        {reasonRow}
        {refundRows}
      </table>
    </div>
    <p style='color:#999;font-size:12px;margin-top:24px;'>This is an automated email. Please do not reply to this message.</p>
  </div>
  <div style='background:#f4f4f4;padding:12px;text-align:center;'>
    <p style='color:#aaa;font-size:12px;margin:0;'>&#169; {DateTime.UtcNow.Year} Bus Ticketing System. All rights reserved.</p>
  </div>
</div>
</body></html>";
        }
    }
}
