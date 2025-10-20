namespace Lpp_Solver.Models
{
    public class LPP
    {
        public double[]? Objectivecoefficient { get; set; }
        public string? OptimizationType { get; set; }
        public List<Constraint>? constraints { get; set; }
        public bool SolveGraphically { get; set; } = false;
    }
}
