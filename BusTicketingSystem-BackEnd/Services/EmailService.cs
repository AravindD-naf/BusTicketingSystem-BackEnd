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
            decimal discountAmount)
        {
            try
            {
                var smtpSettings = _config.GetSection("SmtpSettings");
                var host = smtpSettings["Host"]!;
                var port = int.Parse(smtpSettings["Port"]!);
                var username = smtpSettings["Username"]!;
                var password = smtpSettings["Password"]!;
                var fromEmail = smtpSettings["FromEmail"]!;
                var fromName = smtpSettings["FromName"] ?? "Bus Ticketing System";

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(fromName, fromEmail));
                message.To.Add(new MailboxAddress(userName, toEmail));
                message.Subject = $"Booking Confirmed - PNR: {pnr}";
                message.Body = new TextPart("html") { Text = BuildEmailBody(userName, pnr, source, destination, travelDate, departureTime, arrivalTime, numberOfSeats, totalAmount, promoCode, discountAmount) };

                using var client = new SmtpClient();
                await client.ConnectAsync(host, port, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(username, password);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation("Booking confirmation email sent to {Email} for PNR {PNR}", toEmail, pnr);
            }
            catch (Exception ex)
            {
                // Log but don't throw — email failure should not break the booking confirmation flow
                _logger.LogError(ex, "Failed to send booking confirmation email to {Email} for PNR {PNR}", toEmail, pnr);
            }
        }

        private static string BuildEmailBody(
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
            decimal discountAmount)
        {
            var promoSection = !string.IsNullOrEmpty(promoCode) && discountAmount > 0
                ? $@"<tr><td style='padding:8px;color:#555;'>Promo Code</td><td style='padding:8px;font-weight:bold;'>{promoCode} (-₹{discountAmount:F2})</td></tr>"
                : string.Empty;

            return $@"
<!DOCTYPE html>
<html>
<head><meta charset='utf-8'/></head>
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
          <tr style='background:#e8f0fe;'>
            <td style='padding:8px;color:#555;'>PNR</td>
            <td style='padding:8px;font-weight:bold;font-size:18px;color:#1a73e8;'>{pnr}</td>
          </tr>
          <tr>
            <td style='padding:8px;color:#555;'>From</td>
            <td style='padding:8px;font-weight:bold;'>{source}</td>
          </tr>
          <tr style='background:#f0f0f0;'>
            <td style='padding:8px;color:#555;'>To</td>
            <td style='padding:8px;font-weight:bold;'>{destination}</td>
          </tr>
          <tr>
            <td style='padding:8px;color:#555;'>Travel Date</td>
            <td style='padding:8px;font-weight:bold;'>{travelDate:dddd, MMMM dd yyyy}</td>
          </tr>
          <tr style='background:#f0f0f0;'>
            <td style='padding:8px;color:#555;'>Departure</td>
            <td style='padding:8px;font-weight:bold;'>{departureTime}</td>
          </tr>
          <tr>
            <td style='padding:8px;color:#555;'>Arrival</td>
            <td style='padding:8px;font-weight:bold;'>{arrivalTime}</td>
          </tr>
          <tr style='background:#f0f0f0;'>
            <td style='padding:8px;color:#555;'>Seats</td>
            <td style='padding:8px;font-weight:bold;'>{numberOfSeats}</td>
          </tr>
          {promoSection}
          <tr style='background:#e8f0fe;'>
            <td style='padding:8px;color:#555;'>Total Paid</td>
            <td style='padding:8px;font-weight:bold;font-size:16px;color:#1a73e8;'>₹{totalAmount:F2}</td>
          </tr>
        </table>
      </div>

      <p style='color:#555;font-size:14px;'>Please carry a valid ID proof during your journey. Arrive at the boarding point at least 15 minutes before departure.</p>
      <p style='color:#999;font-size:12px;margin-top:24px;'>This is an automated email. Please do not reply to this message.</p>
    </div>
    <div style='background:#f4f4f4;padding:12px;text-align:center;'>
      <p style='color:#aaa;font-size:12px;margin:0;'>© {DateTime.UtcNow.Year} Bus Ticketing System. All rights reserved.</p>
    </div>
  </div>
</body>
</html>";
        }
    }
}
