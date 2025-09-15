using System.ComponentModel.DataAnnotations;

namespace CSContestConnect.Web.Models.Auth
{
    public class ForgotPasswordViewModel
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;
    }
}
