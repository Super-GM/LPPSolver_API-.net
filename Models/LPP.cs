namespace Lpp_Solver.Models
{
    public class LPP
    {
        public double[]? ObjectiveCoefficients { get; set; }
        public string? OptimizationType { get; set; }
        public List<Constraint>? Constraints { get; set; }
        public bool SolveGraphically { get; set; } = false;
    }
}
