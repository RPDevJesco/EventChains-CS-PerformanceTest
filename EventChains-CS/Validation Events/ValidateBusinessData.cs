using EventChainsCore;

namespace EventChains_CS.Validation_Events
{
    public class ValidateBusinessData : BaseEvent
    {
        public override async Task<EventResult> ExecuteAsync(IEventContext context)
        {
            await Task.CompletedTask;

            var data = context.Get<CustomerData>("customer_data");
            double precision = 100;
            var issues = new List<string>();

            // Age validation
            if (!data.Age.HasValue || data.Age < 18 || data.Age > 120)
            {
                issues.Add("Suspicious age");
                precision -= 30;
            }

            // Company data (optional but valuable)
            if (string.IsNullOrWhiteSpace(data.CompanyName))
            {
                issues.Add("No company information");
                precision -= 20;
            }

            if (!data.Revenue.HasValue)
            {
                issues.Add("No revenue information");
                precision -= 20;
            }

            if (issues.Any())
            {
                return PartialSuccess(
                    $"Business data incomplete: {string.Join(", ", issues)}",
                    Math.Max(precision, 30)
                );
            }

            return Success(precisionScore: 100);
        }
    }
}