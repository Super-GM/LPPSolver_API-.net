namespace Lpp_Solver.Models
{
    public class Constraint
    {
        public double[]? coefficient { get; set; }
        public string? relation { get; set; }
        public double righthandside { get; set; }
    }
}
