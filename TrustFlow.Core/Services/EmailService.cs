using MailKit.Net.Smtp;
using Microsoft.Extensions.Logging;
using MimeKit;
using MongoDB.Driver;
using TrustFlow.Core.Communication;
using TrustFlow.Core.Data;
using TrustFlow.Core.DTOs;
using TrustFlow.Core.Models;

namespace TrustFlow.Core.Services
{
    public class EmailService
    {
        private readonly IMongoCollection<SMTPConfig> _smtpConfig;
        private readonly ILogger<EmailService> _logger;

        public EmailService(ApplicationContext context, ILogger<EmailService> logger)
        {
            _logger = logger;
            _smtpConfig = context.SMTPConfig;
        }

        public async Task<ServiceResult> GetConfig()
        {
            try
            {
                var config = await _smtpConfig.Find(s => s.IsActive == true).FirstOrDefaultAsync();
               
                return new ServiceResult(true, "SMTP Config retrieved successfully.", config);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve smtp config.");
                return new ServiceResult(false, "An internal error occurred while retrieving email config.",null);
            }
        }

        public async Task<ServiceResult> TestSMTP()
        {
            try
            {
                var res = await GetConfig();
                var smtp = (SMTPConfig)res?.Result;

                if (smtp == null)
                {
                    _logger.LogError("SMTP Config not found");
                    return new ServiceResult(false, "SMTP Config not found", null);
                }

                using (var client = new SmtpClient())
                {
                    await client.ConnectAsync(smtp.Host, smtp.Port, smtp.EnableSsl);
                    client.AuthenticationMechanisms.Remove("XOAUTH2");

                    await client.AuthenticateAsync(smtp.UserName, smtp.Password);
                    await client.DisconnectAsync(true);
                }

                return new ServiceResult(true, "SMTP Connection successful", null);
            }
            catch (MailKit.Security.AuthenticationException ex)
            {
                _logger.LogError("SMTP Authentication failed: " + ex.Message);
                return new ServiceResult(false, "SMTP Authentication failed", ex);
            }
            catch (MailKit.ServiceNotConnectedException ex)
            {
                _logger.LogError("SMTP Connection failed: " + ex.Message);
                return new ServiceResult(false, "SMTP Connection failed", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError("Unexpected error testing SMTP: " + ex.Message);
                return new ServiceResult(false, "An internal error occurred", ex);
            }
        }


        public async Task<ServiceResult> SendEmailAsync(SendEmailRequest request)
        {
            try
            {
                var smtp = await _smtpConfig.Find(s => s.IsActive).FirstOrDefaultAsync();

                if (smtp == null)
                {
                    _logger.LogError("SMTP Config not found");
                    return new ServiceResult(false, "SMTP Config not found", null);
                }

                var message = new MimeMessage();

                message.From.Add(new MailboxAddress(smtp.SenderName ?? smtp.UserName, smtp.UserName));
                message.To.AddRange(request.To.Split(';', ',').Select(x => MailboxAddress.Parse(x.Trim())));

                if (request.Cc != null && request.Cc.Any())
                    message.Cc.AddRange(request.Cc.Select(x => MailboxAddress.Parse(x)));

                if (request.Bcc != null && request.Bcc.Any())
                    message.Bcc.AddRange(request.Bcc.Select(x => MailboxAddress.Parse(x)));

                message.Subject = request.Subject;

                var bodyBuilder = new BodyBuilder();

                if (request.IsHtmlBody)
                    bodyBuilder.HtmlBody = request.Body;
                else
                    bodyBuilder.TextBody = request.Body;

                if (request.Attachments != null && request.Attachments.Any())
                {
                    foreach (var attachment in request.Attachments)
                    {
                        if (attachment.Content != null && attachment.Content.Length > 0)
                        {
                            using var stream = new MemoryStream(attachment.Content);
                            bodyBuilder.Attachments.Add(attachment.FileName, stream);
                        }
                    }
                }

                message.Body = bodyBuilder.ToMessageBody();

                using (var client = new SmtpClient())
                {
                    await client.ConnectAsync(smtp.Host, smtp.Port, smtp.EnableSsl);
                    client.AuthenticationMechanisms.Remove("XOAUTH2");

                    await client.AuthenticateAsync(smtp.UserName, smtp.Password);
                    await client.SendAsync(message);
                    await client.DisconnectAsync(true);
                }

                _logger.LogInformation($"Email sent successfully to {request.To}");
                return new ServiceResult(true, "Email sent successfully", null);
            }
            catch (MailKit.Security.AuthenticationException ex)
            {
                _logger.LogError("SMTP Authentication failed: " + ex.Message);
                return new ServiceResult(false, "SMTP Authentication failed", ex);
            }
            catch (MailKit.ServiceNotConnectedException ex)
            {
                _logger.LogError("SMTP Connection failed: " + ex.Message);
                return new ServiceResult(false, "SMTP Connection failed", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError("Unexpected error while sending email: " + ex.Message);
                return new ServiceResult(false, "An internal error occurred", ex);
            }
        }


        public async Task<ServiceResult> SendRegistrationMailAsync(EmailRequest request)
        {
            var toEmail = request.To;
            var userName = request.UserName;
            var subject = "Welcome to TrustFlow!";
            var body = $"<p>Dear {userName},</p>" +
                       "<p>Welcome to TrustFlow! We're excited to have you on board.</p>" +
                       "<p>Your default password is <strong>TrustFlow@123</strong></p>"+
                       "<p>Best regards,<br/>The TrustFlow Team</p>";
            var emailRequest = new SendEmailRequest
            {
                To = toEmail,
                Subject = subject,
                Body = body,
                IsHtmlBody = true
            };
            return await SendEmailAsync(emailRequest);
        }

        public async Task<ServiceResult> SendPasswordResetMailAsync(EmailRequest request)
        {
            var toEmail = request.To;
            var userName = request.UserName;
            var token = Guid.NewGuid().ToString();
            var subject = "TrustFlow Password Reset Request";
            var url = $"https://trustflow.example.com/reset-password?token={token}";
            var body = $"<p>Dear {userName},</p>" +
                       "<p>We received a request to reset your password. Click the link below to reset it:</p>" +
                       $"<p><a href='{url}'>Reset Password</a></p>" +
                       "<p>If you did not request a password reset, please ignore this email.</p>" +
                       "<p>Best regards,<br/>The TrustFlow Team</p>";
            var emailRequest = new SendEmailRequest
            {
                To = toEmail,
                Subject = subject,
                Body = body,
                IsHtmlBody = true
            };
            return await SendEmailAsync(emailRequest);
        }

    }
}
