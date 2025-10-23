using EventChainsCore;
using System.ComponentModel.DataAnnotations;
using EventChains_CS.DTOs;

namespace EventChains_CS.Validation_Events
{
    /// <summary>
    /// Validates required fields for Titanic passenger data
    /// </summary>
    public class ValidateTitanicRequiredFields : BaseEvent
    {
        public override async Task<EventResult> ExecuteAsync(IEventContext context)
        {
            await Task.CompletedTask;

            var passenger = context.Get<TitanicPassenger>("passenger_data");
            var missingFields = new List<string>();

            if (passenger.PassengerId <= 0) missingFields.Add("PassengerId");
            if (string.IsNullOrWhiteSpace(passenger.Name)) missingFields.Add("Name");
            if (string.IsNullOrWhiteSpace(passenger.Sex)) missingFields.Add("Sex");
            if (string.IsNullOrWhiteSpace(passenger.Ticket)) missingFields.Add("Ticket");
            if (string.IsNullOrWhiteSpace(passenger.Embarked)) missingFields.Add("Embarked");

            if (missingFields.Any())
            {
                return Failure($"Missing required fields: {string.Join(", ", missingFields)}", 0);
            }

            return Success(precisionScore: 100);
        }
    }

    /// <summary>
    /// Validates data ranges and formats for Titanic passenger data
    /// </summary>
    public class ValidateTitanicDataRanges : BaseEvent
    {
        public override async Task<EventResult> ExecuteAsync(IEventContext context)
        {
            await Task.CompletedTask;

            var passenger = context.Get<TitanicPassenger>("passenger_data");
            var errors = new List<string>();

            // Validate Survived (0 or 1)
            if (passenger.Survived < 0 || passenger.Survived > 1)
                errors.Add($"Survived must be 0 or 1, got {passenger.Survived}");

            // Validate Pclass (1, 2, or 3)
            if (passenger.Pclass < 1 || passenger.Pclass > 3)
                errors.Add($"Pclass must be 1, 2, or 3, got {passenger.Pclass}");

            // Validate Sex
            if (passenger.Sex != "male" && passenger.Sex != "female")
                errors.Add($"Sex must be 'male' or 'female', got '{passenger.Sex}'");

            // Validate Age (if provided)
            if (passenger.Age.HasValue && (passenger.Age.Value < 0 || passenger.Age.Value > 120))
                errors.Add($"Age must be between 0 and 120, got {passenger.Age.Value}");

            // Validate SibSp
            if (passenger.SibSp < 0 || passenger.SibSp > 20)
                errors.Add($"SibSp must be between 0 and 20, got {passenger.SibSp}");

            // Validate Parch
            if (passenger.Parch < 0 || passenger.Parch > 20)
                errors.Add($"Parch must be between 0 and 20, got {passenger.Parch}");

            // Validate Fare
            if (passenger.Fare < 0 || passenger.Fare > 1000)
                errors.Add($"Fare must be between 0 and 1000, got {passenger.Fare}");

            // Validate Embarked
            if (passenger.Embarked != "C" && passenger.Embarked != "Q" && passenger.Embarked != "S")
                errors.Add($"Embarked must be 'C', 'Q', or 'S', got '{passenger.Embarked}'");

            if (errors.Any())
            {
                return Failure($"Data validation failed: {string.Join("; ", errors)}", 0);
            }

            return Success(precisionScore: 100);
        }
    }

    /// <summary>
    /// Validates the passenger data using DataAnnotations
    /// </summary>
    public class ValidateTitanicAnnotations : BaseEvent
    {
        public override async Task<EventResult> ExecuteAsync(IEventContext context)
        {
            await Task.CompletedTask;

            var passenger = context.Get<TitanicPassenger>("passenger_data");
            var validationContext = new ValidationContext(passenger);
            var validationResults = new List<ValidationResult>();

            bool isValid = Validator.TryValidateObject(passenger, validationContext, validationResults, true);

            if (!isValid)
            {
                var errors = validationResults.Select(vr => vr.ErrorMessage).ToList();
                return Failure($"Validation failed: {string.Join("; ", errors)}", 0);
            }

            return Success(precisionScore: 100);
        }
    }

    /// <summary>
    /// Enriches passenger data with calculated fields and quality scores
    /// </summary>
    public class EnrichTitanicData : BaseEvent
    {
        public override async Task<EventResult> ExecuteAsync(IEventContext context)
        {
            await Task.CompletedTask;

            var passenger = context.Get<TitanicPassenger>("passenger_data");

            // Calculate data quality score based on completeness
            int qualityScore = 100;
            var missingOptionalFields = new List<string>();

            if (!passenger.Age.HasValue)
            {
                missingOptionalFields.Add("Age");
                qualityScore -= 20;
            }

            if (string.IsNullOrWhiteSpace(passenger.Cabin))
            {
                missingOptionalFields.Add("Cabin");
                qualityScore -= 10;
            }

            // Store enrichment data in context
            context.Set("family_size", passenger.FamilySize);
            context.Set("is_alone", passenger.IsAlone);
            context.Set("data_quality_score", qualityScore);
            context.Set("missing_optional_fields", missingOptionalFields);

            var enrichmentData = new
            {
                FamilySize = passenger.FamilySize,
                IsAlone = passenger.IsAlone,
                DataQualityScore = qualityScore,
                MissingFields = missingOptionalFields
            };

            return Success(enrichmentData, qualityScore);
        }
    }

    /// <summary>
    /// Calculates survival risk factors based on passenger data
    /// </summary>
    public class CalculateTitanicRiskScore : BaseEvent
    {
        public override async Task<EventResult> ExecuteAsync(IEventContext context)
        {
            await Task.CompletedTask;

            var passenger = context.Get<TitanicPassenger>("passenger_data");

            // Simple risk score calculation (inverted - higher score = higher survival chance)
            int riskScore = 50; // baseline

            // Class factor (First class had higher survival rates)
            if (passenger.Pclass == 1) riskScore += 30;
            else if (passenger.Pclass == 2) riskScore += 15;
            else riskScore -= 10; // Third class

            // Gender factor (Women and children first)
            if (passenger.Sex == "female") riskScore += 40;
            else riskScore -= 20;

            // Age factor (Children had higher survival rates)
            if (passenger.Age.HasValue)
            {
                if (passenger.Age.Value < 16) riskScore += 20;
                else if (passenger.Age.Value > 60) riskScore -= 10;
            }

            // Family factor (Small families had better survival rates)
            if (passenger.FamilySize >= 2 && passenger.FamilySize <= 4) riskScore += 10;
            else if (passenger.FamilySize > 4) riskScore -= 15;
            else if (passenger.IsAlone) riskScore -= 5;

            // Fare factor (Higher fare may indicate better cabin location)
            if (passenger.Fare > 50) riskScore += 10;
            else if (passenger.Fare < 10) riskScore -= 5;

            // Clamp to 0-100 range
            riskScore = Math.Max(0, Math.Min(100, riskScore));

            context.Set("survival_risk_score", riskScore);

            return Success(new { SurvivalRiskScore = riskScore }, 100);
        }
    }

    /// <summary>
    /// Routes passenger data to appropriate processing queue based on quality
    /// </summary>
    public class RouteTitanicData : BaseEvent
    {
        public override async Task<EventResult> ExecuteAsync(IEventContext context)
        {
            await Task.CompletedTask;

            var dataQualityScore = context.Get<int>("data_quality_score");
            var survivalRiskScore = context.Get<int>("survival_risk_score");

            string routingQueue;
            int precisionScore;

            if (dataQualityScore >= 90)
            {
                routingQueue = "HIGH_QUALITY_QUEUE";
                precisionScore = 100;
            }
            else if (dataQualityScore >= 70)
            {
                routingQueue = "MEDIUM_QUALITY_QUEUE";
                precisionScore = 80;
            }
            else
            {
                routingQueue = "LOW_QUALITY_QUEUE";
                precisionScore = 60;
            }

            context.Set("routing_queue", routingQueue);

            return Success(new
            {
                Queue = routingQueue,
                DataQuality = dataQualityScore,
                SurvivalRisk = survivalRiskScore
            }, precisionScore);
        }
    }
}