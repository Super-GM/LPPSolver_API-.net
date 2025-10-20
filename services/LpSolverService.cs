using Google.OrTools.LinearSolver;
using OR = Google.OrTools.LinearSolver;
using Lpp_Solver.Models;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Diagnostics.Eventing.Reader;
using System.Runtime.InteropServices;

namespace Lpp_Solver.services
{
    public class LpSolverService : ILPSolverservice
    {
        public SolutionResult Solve(LPP problem)
        {
            // number of variables
            int N = problem.Objectivecoefficient?.Length ?? 0;
            if (problem.SolveGraphically)
            {
                if (N != 2)
                {
                    throw new ArgumentException("For Grapically solution you need two variables");
                }
                return SolveGraphically(problem);
            }
            else
            {
                return SolveNumerically(problem);
            }
        }
        private bool IsMaximization(LPP problem)
        {
            var op = problem.OptimizationType?.Trim().ToLowerInvariant() ?? "";
            return op == "max" || op == "maximize" || op == "maximization" || op == "maximize ";
        }

        private SolutionResult SolveNumerically(LPP problem)
        {
            int N = problem.Objectivecoefficient?.Length ?? 0;
            if (N == 0 || problem.constraints == null)
            {
                throw new ArgumentException("incomplete problem data");
            }
            Solver solver = Solver.CreateSolver("GLOP");//GLOP is type of solvers in Google.OrTool for LPP

            if (solver == null)
            {
                throw new Exception("OR Tools failed to create a solver.");
            }

            // (Decision Variables)
            Variable[] x = new Variable[N];
            for (int i = 0; i < N; i++)
            {
                //شرط عدم السالبية + create your Decision Variables
                x[i] = solver.MakeNumVar(0.0, double.PositiveInfinity, $"x{i + 1}");
            }

            // (Constraints)
            foreach (var constraint in problem.constraints)
            {
                // المكتبة بتتعامل مع القيود على انها مصفوفات و لازم يكون عدد المتغيرات فيها نفس العدد اللى فى المسئلة
                if (constraint.coefficient == null || constraint.coefficient.Length != N)
                {
                    throw new ArgumentException("the number of coofficient in the constraint not match with the number of variables");
                }

                // lhs => left hand side 
                //rhs => right hand side 
                LinearExpr lhs = new LinearExpr();
                for (int i = 0; i < N; i++)
                {
                    lhs +=  constraint.coefficient[i] * x[i];
                }
                double rhs = constraint.righthandside;

                // the type of relation <= or >= or =
                switch (constraint.relation)
                {
                    case "<=":
                         solver.Add(lhs <= rhs);
                        break;
                    case ">=":
                        solver.Add(lhs >= rhs);
                        break;
                    case "=":
                        solver.Add(lhs == rhs);
                        break;
                    default:
                        throw new ArgumentException($" Ruls error : (invalid relation : {constraint.relation})");
                }
            }

            //your goal max or min and the objective function Objective is a class from Google.OrTool Ya jo
            Objective objective = solver.Objective();
            for (int i = 0; i < N; i++)
            {
                //in here we know the coofficients of the variables in the objective function Ya jo
                objective.SetCoefficient(x[i], problem.Objectivecoefficient[i]);
            }

            // the type of Optimization Ya jo
            if (IsMaximization(problem))
                objective.SetMaximization();
            else
                objective.SetMinimization();


            // in this step we solve the LPP by Google.Ortool Ya jo
            Solver.ResultStatus resultStatus = solver.Solve();

            
            SolutionResult result = new SolutionResult
            {
                VariableValues = new Dictionary<string, double>()
            };

            if (resultStatus == Solver.ResultStatus.OPTIMAL)
            {
                result.Status = "Optimal";
                result.ObjectiveValue = objective.Value();

                for (int i = 0; i < N; i++)
                {
                    result.VariableValues.Add($"x{i + 1}", x[i].SolutionValue());
                }
            }
            else
            {
                result.Status = resultStatus.ToString();
                result.ObjectiveValue = 0;
            }
            return result;
        }
        private Point SolveTwoEquations(Lpp_Solver.Models.Constraint C1 ,Lpp_Solver.Models.Constraint C2 )
        {
            double a1 = C1.coefficient[0];
            double b1 = C1.coefficient[1];
            double d1 = C1.righthandside;

            double a2 = C2.coefficient[0];
            double b2 = C2.coefficient[1];
            double d2 = C2.righthandside;
            double det = a1 * b2 - a2 * b1;//لو بصفر مش هيكون فى تقاطع 
            if (Math.Abs(det) < 1e-9)//هنا لو قيمة المحدد صغيرة جدا معناها اننا مش هنعرف نحل السؤال 
            {
                return null;
            }
            double x1 = (d1 * b2 - d2 * b1)/det;
            double x2 = (a1 * d2 - a2 * d1)/det;
            return new Point { X=x1, Y = x2 };
        }
        private bool IsFeasible(Point p, List<Lpp_Solver.Models.Constraint> allConstraints)
        {
            double tolerance = 1e-6; // هامش خطا

            foreach (var constraint in allConstraints)
            {
                double lhs = constraint.coefficient[0] * p.X + constraint.coefficient[1] * p.Y;
                double rhs = constraint.righthandside;

                switch (constraint.relation)
                {
                    case "<=":
                        if (lhs > rhs + tolerance) return false;
                        break;
                    case ">=":
                        if (lhs < rhs - tolerance) return false;
                        break;
                    case "=":
                        if (Math.Abs(lhs - rhs) > tolerance) return false;
                        break;
                }
            }
            return true;
        }
        private GraphicalResult SolveGraphically(LPP problem)
        {
            var allConstraints = problem.constraints;
            var feasibleVertices = new List<Point>();
            var virtualConstraints = new List<Lpp_Solver.Models.Constraint> //تحديد الربع الاول
            {
                 new Lpp_Solver.Models.Constraint { coefficient = new double[] { 1, 0 }, relation = ">=", righthandside = 0 },//محورx
                 new Lpp_Solver.Models.Constraint { coefficient = new double[] { 0, 1 }, relation = ">=", righthandside = 0 }//محور y
            };
            allConstraints.AddRange(virtualConstraints);
            for (int i = 0; i < allConstraints.Count; i++)
            {
                for (int j = i + 1; j < allConstraints.Count; j++)
                {
                    var c1 = allConstraints[i];
                    var c2 = allConstraints[j];
                    var intersectionPoint = SolveTwoEquations(c1, c2);
                    if (intersectionPoint != null && IsFeasible(intersectionPoint, allConstraints))
                    {
                        feasibleVertices.Add(intersectionPoint);
                    }
                }
            }
            feasibleVertices = feasibleVertices
            .GroupBy(p => new { X1 = Math.Round(p.X, 5), X2 = Math.Round(p.Y, 5) })
            .Select(g => g.First())
            .ToList();
            var objectiveCoeffs = problem.Objectivecoefficient;
            Point optimalPoint = null;
            bool isMax = IsMaximization(problem);
            double bestZ = isMax ? double.NegativeInfinity : double.PositiveInfinity;
            foreach (var p in feasibleVertices)
            {
                p.Z = objectiveCoeffs[0] * p.X + objectiveCoeffs[1] * p.Y;

                if (isMax)
                {
                    if (p.Z > bestZ) { bestZ = p.Z; optimalPoint = p; }
                }
                else
                {
                    if (p.Z < bestZ) { bestZ = p.Z; optimalPoint = p; }
                }
            }
            if (optimalPoint == null)
            {
                return new GraphicalResult { Status = "Infeasible", FeasibleVertices = new List<Point>() };
            }

            return new GraphicalResult
            {
                Status = "Optimal",
                ObjectiveValue = optimalPoint.Z,
                FeasibleVertices = feasibleVertices,
                OptimalPoint = optimalPoint,
            };
        }
    }
}