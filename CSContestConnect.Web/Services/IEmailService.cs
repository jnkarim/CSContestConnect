namespace CSContestConnect.Web.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string body);
    }
}