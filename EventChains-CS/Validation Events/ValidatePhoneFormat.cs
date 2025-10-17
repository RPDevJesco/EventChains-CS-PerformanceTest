using EventChainsCore;

namespace EventChains_CS.Validation_Events
{
    public class ValidatePhoneFormat : BaseEvent
    {
        public override async Task<EventResult> ExecuteAsync(IEventContext context)
        {
            await Task.CompletedTask;

            var data = context.Get<CustomerData>("customer_data");

            if (string.IsNullOrWhiteSpace(data.Phone))
            {
                // Missing phone is OK (not required), but lowers quality
                return PartialSuccess("Phone number not provided", 50);
            }

            if (!PhoneValidation.TryNormalizePhone(data.Phone, data.Country, out var e164, out var region, out var error))
            {
                // You can include error detail if helpful
                return Failure(error ?? "Invalid phone number", 30);
            }

            // Normalize in-place so downstream steps get a clean format
            data.Phone = e164!; // e.g., +12025550123

            // Optionally: boost if number's region matches the customer's stated country
            var note = region != null && data.Country?.Equals(region, StringComparison.OrdinalIgnoreCase) == true
                ? $"Valid phone ({e164})"
                : $"Valid phone ({e164}) — detected region {region ?? "unknown"}";

            return Success(note, precisionScore: 100);
        }
    }
}