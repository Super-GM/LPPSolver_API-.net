namespace Lpp_Solver.Models
{
    public class GraphicalResult : SolutionResult
    {
        public List<Point> FeasibleVertices { get; set; }
        public List<ConstraintPlotLine> PlotLines { get; set; }
        public Point OptimalPoint { get; set; }
    }
}
