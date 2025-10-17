using EventChainsCore;

namespace EventChains_CS.Validation_Events
{
    public class EnrichWithGeolocation : BaseEvent
    {
        public override async Task<EventResult> ExecuteAsync(IEventContext context)
        {
            // NOTE: In production, this would be an async API call to a geolocation service
            // For demo purposes, we're doing it synchronously to show realistic throughput
            await Task.CompletedTask;

            var data = context.Get<CustomerData>("customer_data");

            if (string.IsNullOrWhiteSpace(data.Country))
            {
                return Failure("Cannot enrich: No country data", 40);
            }

            // Simulate enrichment
            var knownCountries = new[] { "US", "CA", "UK", "AU" };

            if (knownCountries.Contains(data.Country))
            {
                context.Set("geo_enriched", true);
                context.Set("geo_region", "North America"); // Simplified
                return Success(new { Region = "North America" }, 100);
            }

            return PartialSuccess("Country code not in database", 60);
        }
    }
}