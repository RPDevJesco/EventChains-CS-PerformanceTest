using System.Text.Json.Serialization;

namespace EventChains_CS
{
    public class CustomerData
    {
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public int? Age { get; set; }
        public string? City { get; set; }
        public string? Country { get; set; }
        public string? CompanyName { get; set; }

        [JsonConverter(typeof(CurrencyConverter))]
        public decimal? Revenue { get; set; }
        public decimal? CreditScore { get; set; }
    }
}