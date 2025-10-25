using System.Text.Json.Serialization;
namespace Lpp_Solver.Models
{
    public class Constraint
    {
        [JsonPropertyName("Coefficients")]
        public double[]? Coefficients { get; set; }
        [JsonPropertyName("Relation")]

        public string? Relation { get; set; }
        [JsonPropertyName("Righthandside")]
        public double RightHandSide { get; set; }
    }
}
