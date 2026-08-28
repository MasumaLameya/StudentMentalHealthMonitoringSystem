using MimeKit;
using MailKit.Security;

namespace StudentMentalHealthMonitoringSystem.Services
{
    public class EmailService
    {
        // =========================================================
        // Configuration
        // =========================================================

        private readonly IConfiguration _configuration;


        // =========================================================
        // Constructor
        // =========================================================

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }


        // =========================================================
        // Send Email
        // =========================================================

        public async Task SendEmailAsync(
            string recipientEmail,
            string subject,
            string htmlBody)
        {
            // ================= Sender Information =================

            var senderEmail =
                _configuration["EmailSettings:Email"];

            var senderPassword =
                _configuration["EmailSettings:Password"];

            var senderName =
                _configuration["EmailSettings:SenderName"]
                ?? "Student Mental Health Monitoring System";


            // ================= Validate Configuration =================

            if (string.IsNullOrWhiteSpace(senderEmail))
            {
                throw new InvalidOperationException(
                    "Sender email is not configured."
                );
            }

            if (string.IsNullOrWhiteSpace(senderPassword))
            {
                throw new InvalidOperationException(
                    "Sender email password is not configured."
                );
            }


            // ================= Create Email =================

            var message = new MimeMessage();


            // ================= Sender =================

            message.From.Add(
                new MailboxAddress(
                    senderName,
                    senderEmail
                )
            );


            // ================= Recipient =================

            message.To.Add(
                MailboxAddress.Parse(
                    recipientEmail
                )
            );


            // ================= Subject =================

            message.Subject = subject;


            // ================= HTML Body =================

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = htmlBody
            };

            message.Body =
                bodyBuilder.ToMessageBody();


            // =====================================================
            // SMTP CONNECTION
            // Explicitly use MailKit SmtpClient
            // This avoids conflict with System.Net.Mail.SmtpClient
            // =====================================================

            using var smtp =
                new MailKit.Net.Smtp.SmtpClient();


            // ================= Connect =================

            await smtp.ConnectAsync(
                "smtp.gmail.com",
                587,
                SecureSocketOptions.StartTls
            );


            // ================= Authenticate =================

            await smtp.AuthenticateAsync(
                senderEmail,
                senderPassword
            );


            // ================= Send =================

            await smtp.SendAsync(message);


            // ================= Disconnect =================

            await smtp.DisconnectAsync(true);
        }
    }
}