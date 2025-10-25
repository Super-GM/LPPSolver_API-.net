namespace Lpp_Solver.Models
{
    public class GraphicalResult3D : SolutionResult
    {
        public List<point3D> FeasibleVertices { get; set; }
        public point3D OptimalPoint { get; set; }

    }
}
