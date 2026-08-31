using System;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;

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
        // Send Generic Email
        // =========================================================

        public async Task SendEmailAsync(
            string recipientEmail,
            string subject,
            string htmlBody)
        {
            if (string.IsNullOrWhiteSpace(recipientEmail))
            {
                throw new ArgumentException("Recipient email cannot be empty.", nameof(recipientEmail));
            }

            // ================= Sender Information =================

            var senderEmail =
                _configuration["EmailSettings:Email"]
                ?? _configuration["SMTP_EMAIL"]
                ?? string.Empty;

            var senderPassword =
                _configuration["EmailSettings:Password"]
                ?? _configuration["SMTP_PASSWORD"]
                ?? string.Empty;

            var senderName =
                _configuration["EmailSettings:SenderName"]
                ?? _configuration["DEFAULT_FROM_EMAIL"]
                ?? "SMHMS - Student Mental Health Monitoring System";

            var host =
                _configuration["EmailSettings:Host"]
                ?? "smtp.hostinger.com";

            var portStr =
                _configuration["EmailSettings:Port"]
                ?? "465";

            int.TryParse(portStr, out int port);
            if (port <= 0) port = 465;

            var useSslStr =
                _configuration["EmailSettings:UseSsl"]
                ?? "true";

            bool.TryParse(useSslStr, out bool useSsl);


            // ================= Validate Configuration =================

            if (string.IsNullOrWhiteSpace(senderEmail))
            {
                throw new InvalidOperationException(
                    "Sender email is not configured in appsettings.json (EmailSettings:Email)."
                );
            }

            if (string.IsNullOrWhiteSpace(senderPassword))
            {
                throw new InvalidOperationException(
                    "Sender email password is not configured in appsettings.json (EmailSettings:Password)."
                );
            }


            // ================= Create Email Message =================

            var message = new MimeMessage();

            message.From.Add(
                new MailboxAddress(
                    senderName,
                    senderEmail
                )
            );

            message.To.Add(
                MailboxAddress.Parse(
                    recipientEmail.Trim()
                )
            );

            message.Subject = subject;

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = htmlBody
            };

            message.Body = bodyBuilder.ToMessageBody();


            // =====================================================
            // SMTP CONNECTION VIA MAILKIT
            // =====================================================

            using var smtp = new SmtpClient();

            // Set reasonable timeout
            smtp.Timeout = 15000;

            SecureSocketOptions socketOption = useSsl || port == 465
                ? SecureSocketOptions.SslOnConnect
                : (port == 587 ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto);

            await smtp.ConnectAsync(
                host,
                port,
                socketOption
            );

            await smtp.AuthenticateAsync(
                senderEmail,
                senderPassword
            );

            await smtp.SendAsync(message);

            await smtp.DisconnectAsync(true);
        }


        // =========================================================
        // SEND PASSWORD RESET OTP EMAIL
        // =========================================================

        public async Task SendPasswordResetOtpAsync(
            string recipientEmail,
            string recipientName,
            string otpCode,
            string roleTitle = "User")
        {
            var subject = $"Password Reset OTP - SMHMS ({roleTitle})";

            var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Password Reset Request</title>
</head>
<body style='margin: 0; padding: 0; background-color: #F0F4F2; font-family: -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, Helvetica, Arial, sans-serif;'>
    <table role='presentation' border='0' cellpadding='0' cellspacing='0' width='100%' style='background-color: #F0F4F2; padding: 30px 15px;'>
        <tr>
            <td align='center'>
                <table role='presentation' border='0' cellpadding='0' cellspacing='0' width='100%' style='max-width: 540px; background-color: #FFFFFF; border-radius: 16px; overflow: hidden; box-shadow: 0 8px 30px rgba(0,0,0,0.08); border: 1px solid #D9E5DF;'>
                    
                    <!-- Header -->
                    <tr>
                        <td style='background: linear-gradient(135deg, #004D25 0%, #006837 100%); padding: 32px 24px; text-align: center; border-bottom: 4px solid #FFC20E;'>
                            <h2 style='color: #FFFFFF; margin: 0; font-size: 22px; font-weight: 700; letter-spacing: 0.5px;'>Student Mental Health Monitoring System</h2>
                            <p style='color: rgba(255,255,255,0.85); margin: 6px 0 0 0; font-size: 13px;'>Secure Verification Service</p>
                        </td>
                    </tr>

                    <!-- Body Content -->
                    <tr>
                        <td style='padding: 32px 28px;'>
                            <h3 style='color: #1F3A2B; margin: 0 0 12px 0; font-size: 18px; font-weight: 600;'>Hello {recipientName},</h3>
                            <p style='color: #4A6354; font-size: 14px; line-height: 1.6; margin: 0 0 20px 0;'>
                                We received a request to reset your password for your <strong>{roleTitle}</strong> account. Please use the One-Time Password (OTP) below to proceed with resetting your password:
                            </p>

                            <!-- OTP Box -->
                            <div style='background-color: #E8F3ED; border: 2px dashed #006837; border-radius: 12px; padding: 18px; text-align: center; margin: 24px 0;'>
                                <span style='display: block; font-size: 12px; color: #4A6354; font-weight: 600; text-transform: uppercase; letter-spacing: 1px; margin-bottom: 6px;'>Your Verification Code</span>
                                <span style='display: inline-block; font-size: 34px; font-weight: 800; color: #004D25; letter-spacing: 8px;'>{otpCode}</span>
                            </div>

                            <p style='color: #6C7A72; font-size: 13px; line-height: 1.5; margin: 0 0 16px 0;'>
                                <strong style='color: #C0392B;'>⏱ Note:</strong> This OTP is valid for <strong>10 minutes</strong>. Do not share this code with anyone.
                            </p>
                            
                            <p style='color: #8C9991; font-size: 12px; line-height: 1.5; margin: 24px 0 0 0; border-top: 1px solid #E8ECE9; padding-top: 16px;'>
                                If you did not request a password reset, you can safely ignore this email. Your current password will remain unchanged.
                            </p>
                        </td>
                    </tr>

                    <!-- Footer -->
                    <tr>
                        <td style='background-color: #F8FAF9; padding: 16px 24px; text-align: center; border-top: 1px solid #E8ECE9;'>
                            <p style='color: #8C9991; font-size: 11px; margin: 0;'>
                                &copy; {DateTime.Now.Year} Student Mental Health Monitoring System (SMHMS). All rights reserved.
                            </p>
                        </td>
                    </tr>

                </table>
            </td>
        </tr>
    </table>
</body>
</html>";

            await SendEmailAsync(recipientEmail, subject, htmlBody);
        }


        // =========================================================
        // SEND GUARDIAN LOGIN OTP EMAIL
        // =========================================================

        public async Task SendGuardianLoginOtpAsync(
            string recipientEmail,
            string? guardianName,
            string studentName,
            string studentIdNumber,
            string otpCode)
        {
            var subject = $"Guardian Portal Access OTP - Student: {studentName} ({studentIdNumber})";

            var greeting = string.IsNullOrWhiteSpace(guardianName)
                ? "Dear Guardian,"
                : $"Dear {guardianName},";

            var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Guardian Portal Login Verification</title>
</head>
<body style='margin: 0; padding: 0; background-color: #F0F4F2; font-family: -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, Helvetica, Arial, sans-serif;'>
    <table role='presentation' border='0' cellpadding='0' cellspacing='0' width='100%' style='background-color: #F0F4F2; padding: 30px 15px;'>
        <tr>
            <td align='center'>
                <table role='presentation' border='0' cellpadding='0' cellspacing='0' width='100%' style='max-width: 540px; background-color: #FFFFFF; border-radius: 16px; overflow: hidden; box-shadow: 0 8px 30px rgba(0,0,0,0.08); border: 1px solid #D9E5DF;'>
                    
                    <!-- Header -->
                    <tr>
                        <td style='background: linear-gradient(135deg, #2C5E7A 0%, #457B9D 100%); padding: 32px 24px; text-align: center; border-bottom: 4px solid #E9C46A;'>
                            <h2 style='color: #FFFFFF; margin: 0; font-size: 22px; font-weight: 700; letter-spacing: 0.5px;'>Guardian Wellness Portal</h2>
                            <p style='color: rgba(255,255,255,0.85); margin: 6px 0 0 0; font-size: 13px;'>Student Mental Health Monitoring System</p>
                        </td>
                    </tr>

                    <!-- Body Content -->
                    <tr>
                        <td style='padding: 32px 28px;'>
                            <h3 style='color: #1F3A4B; margin: 0 0 12px 0; font-size: 18px; font-weight: 600;'>{greeting}</h3>
                            <p style='color: #4A5F6D; font-size: 14px; line-height: 1.6; margin: 0 0 16px 0;'>
                                A login request was made to access mental health wellness and assessment reports for:
                            </p>

                            <!-- Student Info Card -->
                            <div style='background-color: #F1F6F9; border-left: 4px solid #457B9D; border-radius: 6px; padding: 12px 16px; margin: 0 0 20px 0;'>
                                <p style='margin: 0; color: #2C3E50; font-size: 14px;'><strong>Student Name:</strong> {studentName}</p>
                                <p style='margin: 4px 0 0 0; color: #2C3E50; font-size: 14px;'><strong>Student ID:</strong> {studentIdNumber}</p>
                            </div>

                            <p style='color: #4A5F6D; font-size: 14px; line-height: 1.6; margin: 0 0 20px 0;'>
                                Please enter the following 6-digit One-Time Password (OTP) on the Guardian Login page to verify your identity:
                            </p>

                            <!-- OTP Box -->
                            <div style='background-color: #EAF2F8; border: 2px dashed #457B9D; border-radius: 12px; padding: 18px; text-align: center; margin: 24px 0;'>
                                <span style='display: block; font-size: 12px; color: #4A5F6D; font-weight: 600; text-transform: uppercase; letter-spacing: 1px; margin-bottom: 6px;'>Guardian Login OTP</span>
                                <span style='display: inline-block; font-size: 34px; font-weight: 800; color: #1D3557; letter-spacing: 8px;'>{otpCode}</span>
                            </div>

                            <p style='color: #6C7A82; font-size: 13px; line-height: 1.5; margin: 0 0 16px 0;'>
                                <strong style='color: #E63946;'>⏱ Note:</strong> This OTP is valid for <strong>10 minutes</strong>.
                            </p>
                            
                            <p style='color: #8C99A2; font-size: 12px; line-height: 1.5; margin: 24px 0 0 0; border-top: 1px solid #E8ECEF; padding-top: 16px;'>
                                If you did not initiate this login request, please ensure your student's account security and contact university counseling support immediately.
                            </p>
                        </td>
                    </tr>

                    <!-- Footer -->
                    <tr>
                        <td style='background-color: #F8FAFB; padding: 16px 24px; text-align: center; border-top: 1px solid #E8ECEF;'>
                            <p style='color: #8C99A2; font-size: 11px; margin: 0;'>
                                &copy; {DateTime.Now.Year} Student Mental Health Monitoring System (SMHMS). All rights reserved.
                            </p>
                        </td>
                    </tr>

                </table>
            </td>
        </tr>
    </table>
</body>
</html>";

            await SendEmailAsync(recipientEmail, subject, htmlBody);
        }
    }
}