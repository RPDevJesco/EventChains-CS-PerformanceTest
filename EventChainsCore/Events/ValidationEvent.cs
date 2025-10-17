namespace EventChainsCore
{
    /// <summary>
    /// A validation event that checks context conditions.
    /// </summary>
    public class ValidationEvent : BaseEvent
    {
        private readonly Func<IEventContext, (bool isValid, string? errorMessage)> _validator;

        public ValidationEvent(
            Func<IEventContext, (bool, string?)> validator,
            string? name = null)
        {
            _validator = validator;
            if (name != null)
            {
                _validationName = name;
            }
        }

        private string? _validationName;
        public override string EventName => _validationName ?? base.EventName;

        public override async Task<EventResult> ExecuteAsync(IEventContext context)
        {
            await Task.CompletedTask;

            var (isValid, errorMessage) = _validator(context);

            if (isValid)
            {
                return Success(new { ValidationPassed = true });
            }

            return Failure(errorMessage ?? "Validation failed");
        }
    }
}
