using EventChainsCore;

using System.Net;
using System.Net.Mail;
using System.Net.Sockets;

namespace EventChains_CS.Validation_Events
{
    public class ValidateEmailFormat : BaseEvent
    {
        // Static flag to disable DNS lookups for maximum performance
        public static bool SkipDnsLookup { get; set; } = false;

        private static readonly HashSet<string> DisposableDomains = new(StringComparer.OrdinalIgnoreCase)
        {
            "tempmail.com", "throwaway.com", "mailinator.com", "10minutemail.com",
            "guerrillamail.com", "yopmail.com", "trashmail.com", "getnada.com", "test.com"
        };

        // Optional: catch common typo domains and flag them
        private static readonly string[] CommonTypos =
        {
            "gmal.com", "gnail.com", "hotmial.com", "yaho.com", "outlok.com"
        };

        // DNS cache to avoid repeated lookups for the same domain
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, bool> _dnsCache = new();

        public override async Task<EventResult> ExecuteAsync(IEventContext context)
        {
            await Task.CompletedTask;

            var data = context.Get<CustomerData>("customer_data");
            var email = data.Email?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(email))
            {
                return PartialSuccess("Email address not provided", 50);
            }

            // Step 1: Basic structure validation using System.Net.Mail
            try
            {
                var mailAddr = new MailAddress(email);
                email = mailAddr.Address; // normalize
            }
            catch (FormatException)
            {
                return Failure("Invalid email format", 20);
            }

            // Step 2: Domain-level checks
            var domain = email.Split('@').LastOrDefault()?.ToLowerInvariant();
            if (string.IsNullOrEmpty(domain))
                return Failure("Invalid email domain", 20);

            // Disposable email check
            if (DisposableDomains.Contains(domain))
                return PartialSuccess($"Disposable email domain ({domain}) detected", 60);

            // Common typo check
            if (CommonTypos.Contains(domain))
                return PartialSuccess($"Possible typo in domain ({domain})", 70);

            // Step 3: Optional DNS lookup (with caching for performance)
            // Skip if --no-dns flag is set
            if (!SkipDnsLookup)
            {
                bool domainExists = await DomainExistsAsync(domain);
                if (!domainExists)
                {
                    return PartialSuccess($"Email domain '{domain}' does not appear to exist", 80);
                }
            }

            return Success("Valid email address", 100);
        }

        private static async Task<bool> DomainExistsAsync(string domain)
        {
            // Check cache first (huge performance boost!)
            if (_dnsCache.TryGetValue(domain, out var cachedResult))
            {
                return cachedResult;
            }

            // Not in cache, do DNS lookup
            try
            {
                var hostEntry = await Dns.GetHostEntryAsync(domain);
                var exists = hostEntry?.AddressList?.Length > 0;

                // Cache the result (domains don't change often)
                _dnsCache.TryAdd(domain, exists);

                return exists;
            }
            catch (SocketException)
            {
                _dnsCache.TryAdd(domain, false);
                return false;
            }
            catch
            {
                _dnsCache.TryAdd(domain, false);
                return false;
            }
        }
    }
}