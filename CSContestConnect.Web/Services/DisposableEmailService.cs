using System.Collections.Generic;
using System.Linq;

namespace CSContestConnect.Web.Services
{
    public interface IDisposableEmailService
    {
        bool IsDisposableEmail(string email);
    }

    public class DisposableEmailService : IDisposableEmailService
    {
        private readonly HashSet<string> _disposableDomains;

        public DisposableEmailService()
        {
            _disposableDomains = new HashSet<string>
            {
                "10minutemail.com", "guerrillamail.com", "mailinator.com",
                "tempmail.org", "yopmail.com", "throwaway.email"
                // ... rest of domains
            };
        }

        public bool IsDisposableEmail(string email)
        {
            if (string.IsNullOrEmpty(email) || !email.Contains("@"))
                return true;

            var domain = email.Split('@')[1].ToLower();
            return _disposableDomains.Contains(domain);
        }
    }
}