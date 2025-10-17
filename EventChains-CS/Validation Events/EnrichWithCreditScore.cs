using EventChainsCore;

namespace EventChains_CS.Validation_Events
{
    public class EnrichWithCreditScore : BaseEvent
    {
        public override async Task<EventResult> ExecuteAsync(IEventContext context)
        {
            // NOTE: In production, this would be an async API call to a credit service
            // For demo purposes, we're doing it synchronously to show realistic throughput
            await Task.CompletedTask;

            var data = context.Get<CustomerData>("customer_data");

            // Only attempt if we have company data
            if (string.IsNullOrWhiteSpace(data.CompanyName))
            {
                return PartialSuccess("Skipped: No company data", 50);
            }

            // Simulate credit score lookup (in reality, this would be an API call)
            var simulatedScore = data.Revenue.HasValue && data.Revenue > 500000 ? 750 : 650;

            context.Set("credit_score", simulatedScore);

            return Success(new { CreditScore = simulatedScore }, 100);
        }
    }
}