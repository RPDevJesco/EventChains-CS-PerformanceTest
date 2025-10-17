using EventChainsCore;

namespace EventChains_CS.Validation_Events
{
    public class CalculateRiskScore : BaseEvent
    {
        public override async Task<EventResult> ExecuteAsync(IEventContext context)
        {
            await Task.CompletedTask;

            var data = context.Get<CustomerData>("customer_data");
            double riskScore = 50; // Baseline

            // Lower risk if we have good data
            if (context.ContainsKey("geo_enriched") && context.Get<bool>("geo_enriched"))
            {
                riskScore -= 10;
            }

            if (context.ContainsKey("credit_score"))
            {
                var creditScore = context.Get<int>("credit_score");
                riskScore -= (creditScore - 600) / 20.0; // Better credit = lower risk
            }

            if (data.Revenue.HasValue && data.Revenue > 500000)
            {
                riskScore -= 15; // Established business
            }

            riskScore = Math.Max(0, Math.Min(100, riskScore));

            context.Set("risk_score", riskScore);

            return Success(new { RiskScore = riskScore }, 100);
        }
    }
}