using Lpp_Solver.Models;
namespace Lpp_Solver.services
{
    public interface ILPSolverservice
    {
        SolutionResult SolveNumerically(LPP problem);
        GraphicalResult3D SolveGraphically3D(LPP lpp);
        GraphicalResult SolveGraphically2D(LPP problem);
    }
}
