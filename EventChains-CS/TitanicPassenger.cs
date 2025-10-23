using System.ComponentModel.DataAnnotations;

namespace EventChains_CS.DTOs
{
    /// <summary>
    /// Data Transfer Object for Titanic passenger data with validation.
    /// Represents a passenger record from the Titanic dataset.
    /// </summary>
    public class TitanicPassenger
    {
        [Required(ErrorMessage = "PassengerId is required")]
        [Range(1, int.MaxValue, ErrorMessage = "PassengerId must be a positive integer")]
        public int PassengerId { get; set; }

        [Required(ErrorMessage = "Survived status is required")]
        [Range(0, 1, ErrorMessage = "Survived must be 0 (No) or 1 (Yes)")]
        public int Survived { get; set; }

        [Required(ErrorMessage = "Pclass is required")]
        [Range(1, 3, ErrorMessage = "Pclass must be 1 (First), 2 (Second), or 3 (Third)")]
        public int Pclass { get; set; }

        [Required(ErrorMessage = "Name is required")]
        [StringLength(200, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 200 characters")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Sex is required")]
        [RegularExpression("^(male|female)$", ErrorMessage = "Sex must be 'male' or 'female'")]
        public string Sex { get; set; } = string.Empty;

        // Age is nullable as many records have missing age data
        [Range(0.0, 120.0, ErrorMessage = "Age must be between 0 and 120")]
        public double? Age { get; set; }

        [Required(ErrorMessage = "SibSp is required")]
        [Range(0, 20, ErrorMessage = "SibSp (siblings/spouses) must be between 0 and 20")]
        public int SibSp { get; set; }

        [Required(ErrorMessage = "Parch is required")]
        [Range(0, 20, ErrorMessage = "Parch (parents/children) must be between 0 and 20")]
        public int Parch { get; set; }

        [Required(ErrorMessage = "Ticket is required")]
        [StringLength(50, MinimumLength = 1, ErrorMessage = "Ticket must be between 1 and 50 characters")]
        public string Ticket { get; set; } = string.Empty;

        [Required(ErrorMessage = "Fare is required")]
        [Range(0.0, 1000.0, ErrorMessage = "Fare must be between 0 and 1000")]
        public double Fare { get; set; }

        // Cabin is nullable as many records have missing cabin data
        [StringLength(50, ErrorMessage = "Cabin cannot exceed 50 characters")]
        public string? Cabin { get; set; }

        [Required(ErrorMessage = "Embarked is required")]
        [RegularExpression("^[CQS]$", ErrorMessage = "Embarked must be 'C' (Cherbourg), 'Q' (Queenstown), or 'S' (Southampton)")]
        public string Embarked { get; set; } = string.Empty;

        /// <summary>
        /// Calculated property: Total family members aboard
        /// </summary>
        public int FamilySize => SibSp + Parch + 1;

        /// <summary>
        /// Calculated property: Is traveling alone
        /// </summary>
        public bool IsAlone => SibSp == 0 && Parch == 0;

        /// <summary>
        /// Calculated property: Passenger class description
        /// </summary>
        public string PclassDescription => Pclass switch
        {
            1 => "First Class",
            2 => "Second Class",
            3 => "Third Class",
            _ => "Unknown"
        };

        /// <summary>
        /// Returns a string representation of the passenger
        /// </summary>
        public override string ToString()
        {
            return $"Passenger {PassengerId}: {Name} ({Sex}, Age: {Age?.ToString() ?? "Unknown"}), " +
                   $"Class: {Pclass}, Survived: {(Survived == 1 ? "Yes" : "No")}";
        }
    }
}