using EventChainsCore;

namespace EventChains_CS.Validation_Events
{
    public class ValidateRequiredFields : BaseEvent
    {
        public override async Task<EventResult> ExecuteAsync(IEventContext context)
        {
            await Task.CompletedTask;

            var data = context.Get<CustomerData>("customer_data");
            var missingFields = new List<string>();

            if (string.IsNullOrWhiteSpace(data.Email)) missingFields.Add("Email");
            if (string.IsNullOrWhiteSpace(data.FirstName)) missingFields.Add("FirstName");
            if (string.IsNullOrWhiteSpace(data.LastName)) missingFields.Add("LastName");

            if (missingFields.Any())
            {
                return Failure($"Missing required fields: {string.Join(", ", missingFields)}", 0);
            }

            return Success(precisionScore: 100);
        }
    }
}