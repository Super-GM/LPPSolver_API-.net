using Lpp_Solver.Models;
namespace Lpp_Solver.services
{
    public interface ILPSolverservice
    {
        SolutionResult Solve(LPP lPP);
    }
}
