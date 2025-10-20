namespace Lpp_Solver.Models
{
    public class SolutionResult
    {
        public string? Status { get; set; } //optimal,unbounded,infinite
        public double? ObjectiveValue { get; set; } //Z
        public Dictionary<string,double>? VariableValues { get; set; } //x1,x2,...
    }
}
