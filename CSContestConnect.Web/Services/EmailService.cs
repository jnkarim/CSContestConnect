using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using CSContestConnect.Web.Models;

namespace CSContestConnect.Web.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                var from = _configuration["EmailSettings:From"];
                var smtpServer = _configuration["EmailSettings:SmtpServer"];
                var port = int.TryParse(_configuration["EmailSettings:Port"], out var p) ? p : 587;
                var username = _configuration["EmailSettings:Username"];
                var password = _configuration["EmailSettings:Password"];

                using var client = new SmtpClient(smtpServer, port)
                {
                    Credentials = new NetworkCredential(username, password),
                    EnableSsl = true,
                    UseDefaultCredentials = false
                };

                using var message = new MailMessage(from!, toEmail, subject, body)
                {
                    IsBodyHtml = true
                };

                await client.SendMailAsync(message);
            }
            catch (SmtpException ex)
            {
                // Log the specific SMTP error
                Console.WriteLine($"SMTP Error: {ex.StatusCode} - {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"General Error: {ex.Message}");
                throw;
            }
        }
    }
}