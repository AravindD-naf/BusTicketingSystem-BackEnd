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
        private readonly IWebHostEnvironment _env;

        public EmailService(IConfiguration config, ILogger<EmailService> logger, IWebHostEnvironment env)
        {
            _config = config;
            _logger = logger;
            _env    = env;
        }

        // ── Public methods ────────────────────────────────────────────────────

        public async Task SendBookingConfirmationAsync(
            string toEmail, string userName, string pnr,
            string source, string destination, DateTime travelDate,
            string departureTime, string arrivalTime, int numberOfSeats,
            List<string> seatNumbers,
            decimal baseFare, decimal discountAmount, decimal gstAmount,
            decimal convenienceFee, decimal grandTotal, string? promoCode)
        {
            try
            {
                decimal fareAfterDiscount = baseFare - discountAmount;

                var promoRow = !string.IsNullOrEmpty(promoCode) && discountAmount > 0
                    ? $"<tr><td style='padding:8px;color:#555;'>Promo Discount ({promoCode})</td><td style='padding:8px;font-weight:bold;color:#2e7d32;'>- &#8377;{discountAmount:F2}</td></tr>"
                    : string.Empty;

                var seatDisplay = seatNumbers != null && seatNumbers.Count > 0
                    ? string.Join(", ", seatNumbers)
                    : numberOfSeats.ToString();

                var body = LoadTemplate("booking-confirmation.html")
                    .Replace("{{UserName}}",         userName)
                    .Replace("{{PNR}}",              pnr)
                    .Replace("{{Source}}",           source)
                    .Replace("{{Destination}}",      destination)
                    .Replace("{{TravelDate}}",       travelDate.ToString("dddd, MMMM dd yyyy"))
                    .Replace("{{DepartureTime}}",    departureTime)
                    .Replace("{{ArrivalTime}}",      arrivalTime)
                    .Replace("{{NumberOfSeats}}",    numberOfSeats.ToString())
                    .Replace("{{SeatNumbers}}",      seatDisplay)
                    .Replace("{{BaseFare}}",         baseFare.ToString("F2"))
                    .Replace("{{PromoRow}}",         promoRow)
                    .Replace("{{FareAfterDiscount}}", fareAfterDiscount.ToString("F2"))
                    .Replace("{{GstAmount}}",        gstAmount.ToString("F2"))
                    .Replace("{{ConvenienceFee}}",   convenienceFee.ToString("F2"))
                    .Replace("{{GrandTotal}}",       grandTotal.ToString("F2"))
                    .Replace("{{Year}}",             DateTime.UtcNow.Year.ToString());

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
                var refundRows = refundAmount > 0
                    ? $"<tr style='background:#e8f5e9;'><td style='padding:8px;color:#555;'>Refund Amount</td><td style='padding:8px;font-weight:bold;color:#2e7d32;'>&#8377;{refundAmount:F2} ({refundPercentage}%)</td></tr>" +
                      $"<tr><td style='padding:8px;color:#555;'>Cancellation Fee</td><td style='padding:8px;color:#c62828;'>&#8377;{cancellationFee:F2}</td></tr>" +
                      $"<tr style='background:#f0f0f0;'><td colspan='2' style='padding:8px;color:#555;font-size:13px;'>The refund of <strong>&#8377;{refundAmount:F2}</strong> will be credited to your BusMate Wallet shortly.</td></tr>"
                    : "<tr style='background:#fff3e0;'><td style='padding:8px;color:#555;'>Refund Amount</td><td style='padding:8px;font-weight:bold;color:#e65100;'>&#8377;0.00 (0%)</td></tr>" +
                      "<tr><td colspan='2' style='padding:8px;color:#555;font-size:13px;'>As per our cancellation policy, no refund is applicable for cancellations made within the non-refundable window.</td></tr>";

                var reasonRow = !string.IsNullOrWhiteSpace(cancellationReason)
                    ? $"<tr><td style='padding:8px;color:#555;'>Reason</td><td style='padding:8px;'>{cancellationReason}</td></tr>"
                    : string.Empty;

                var body = LoadTemplate("booking-cancellation.html")
                    .Replace("{{UserName}}",    userName)
                    .Replace("{{PNR}}",         pnr)
                    .Replace("{{Source}}",      source)
                    .Replace("{{Destination}}", destination)
                    .Replace("{{TravelDate}}",  travelDate.ToString("dddd, MMMM dd yyyy"))
                    .Replace("{{AmountPaid}}",  amountPaid.ToString("F2"))
                    .Replace("{{ReasonRow}}",   reasonRow)
                    .Replace("{{RefundRows}}",  refundRows)
                    .Replace("{{Year}}",        DateTime.UtcNow.Year.ToString());

                await SendEmailAsync(toEmail, userName, $"Booking Cancelled - PNR: {pnr}", body);
                _logger.LogInformation("Cancellation email sent to {Email} for PNR {PNR}", toEmail, pnr);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send cancellation email to {Email} for PNR {PNR}", toEmail, pnr);
            }
        }

        public async Task SendRefundStatusEmailAsync(
            string toEmail, string userName, string pnr,
            bool isApproved, decimal refundAmount, string reason)
        {
            try
            {
                var headerColor = isApproved ? "#2e7d32" : "#d32f2f";
                var title       = isApproved ? "Refund Approved ✅" : "Refund Rejected ❌";
                var statusColor = isApproved ? "#2e7d32" : "#d32f2f";
                var statusText  = isApproved ? "Approved" : "Rejected";

                var amountRow = isApproved
                    ? $"<tr style='background:#e8f5e9;'><td style='padding:8px;color:#555;'>Refund Amount</td><td style='padding:8px;font-weight:bold;color:#2e7d32;'>&#8377;{refundAmount:F2}</td></tr>"
                    : string.Empty;

                var walletNote = isApproved
                    ? $"<p style='color:#2e7d32;font-size:14px;'>&#8377;{refundAmount:F2} has been credited to your BusMate Wallet.</p>"
                    : "<p style='color:#555;font-size:14px;'>Your refund request has been reviewed and rejected. Please contact support if you have any questions.</p>";

                var reasonRow = !string.IsNullOrWhiteSpace(reason)
                    ? $"<tr><td style='padding:8px;color:#555;'>Reason</td><td style='padding:8px;'>{reason}</td></tr>"
                    : string.Empty;

                var subject = isApproved ? $"Refund Approved - PNR: {pnr}" : $"Refund Rejected - PNR: {pnr}";

                var body = LoadTemplate("refund-status.html")
                    .Replace("{{UserName}}",    userName)
                    .Replace("{{PNR}}",         pnr)
                    .Replace("{{HeaderColor}}", headerColor)
                    .Replace("{{Title}}",       title)
                    .Replace("{{StatusColor}}", statusColor)
                    .Replace("{{StatusText}}",  statusText)
                    .Replace("{{AmountRow}}",   amountRow)
                    .Replace("{{ReasonRow}}",   reasonRow)
                    .Replace("{{WalletNote}}",  walletNote)
                    .Replace("{{Year}}",        DateTime.UtcNow.Year.ToString());

                await SendEmailAsync(toEmail, userName, subject, body);
                _logger.LogInformation("Refund status email sent to {Email} for PNR {PNR}", toEmail, pnr);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send refund status email to {Email} for PNR {PNR}", toEmail, pnr);
            }
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private string LoadTemplate(string fileName)
        {
            var path = Path.Combine(_env.ContentRootPath, "Templates", "Email", fileName);
            return File.ReadAllText(path);
        }

        private async Task SendEmailAsync(string toEmail, string toName, string subject, string htmlBody)
        {
            var s         = _config.GetSection("SmtpSettings");
            var host      = s["Host"]!;
            var port      = int.Parse(s["Port"]!);
            var username  = s["Username"]!;
            var password  = s["Password"]!;
            var fromEmail = s["FromEmail"]!;
            var fromName  = s["FromName"] ?? "Bus Ticketing System";

            var msg = new MimeMessage();
            msg.From.Add(new MailboxAddress(fromName, fromEmail));
            msg.To.Add(new MailboxAddress(toName, toEmail));
            msg.Subject = subject;
            msg.Body    = new TextPart("html") { Text = htmlBody };

            using var client = new SmtpClient();
            await client.ConnectAsync(host, port, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(username, password);
            await client.SendAsync(msg);
            await client.DisconnectAsync(true);
        }
    }
}
