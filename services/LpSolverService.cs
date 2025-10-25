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
        private bool IsMaximization(LPP problem)
        {
            var op = problem.OptimizationType?.Trim().ToLowerInvariant() ?? "";
            return op == "max" || op == "maximize" || op == "maximization" || op == "maximize ";
        }

        public SolutionResult SolveNumerically(LPP problem)
        {
            int N = problem.ObjectiveCoefficients?.Length ?? 0;
            if (N == 0 || problem.Constraints == null)
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
            foreach (var constraint in problem.Constraints)
            {
                // المكتبة بتتعامل مع القيود على انها مصفوفات و لازم يكون عدد المتغيرات فيها نفس العدد اللى فى المسئلة
                if (constraint.Coefficients == null || constraint.Coefficients.Length != N)
                {
                    throw new ArgumentException("the number of coofficient in the constraint not match with the number of variables");
                }

                // lhs => left hand side 
                //rhs => right hand side 
                LinearExpr lhs = new LinearExpr();
                for (int i = 0; i < N; i++)
                {
                    lhs +=  constraint.Coefficients[i] * x[i];
                }
                double rhs = constraint.RightHandSide;

                // the type of relation <= or >= or =
                switch (constraint.Relation)
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
                        throw new ArgumentException($" Ruls error : (invalid relation : {constraint.Relation})");
                }
            }

            //your goal max or min and the objective function Objective is a class from Google.OrTool Ya jo
            Objective objective = solver.Objective();
            for (int i = 0; i < N; i++)
            {
                //in here we know the coofficients of the variables in the objective function Ya jo
                objective.SetCoefficient(x[i], problem.ObjectiveCoefficients[i]);
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
        private Point SolveTwoEquations(Lpp_Solver.Models.Constraint C1, Lpp_Solver.Models.Constraint C2)
        {
            if (C1 == null || C2 == null)
                throw new ArgumentNullException("One or both constraints are null.");

            if (C1.Coefficients == null || C2.Coefficients == null)
                throw new ArgumentNullException("Coefficient array is null.");

            if (C1.Coefficients.Length < 2 || C2.Coefficients.Length < 2)
                throw new ArgumentException("Each constraint must have at least two coefficients.");

            double a1 = C1.Coefficients[0];
            double b1 = C1.Coefficients[1];
            double d1 = C1.RightHandSide;

            double a2 = C2.Coefficients[0];
            double b2 = C2.Coefficients[1];
            double d2 = C2.RightHandSide;

            double det = a1 * b2 - a2 * b1;
            if (Math.Abs(det) < 1e-9)
            {
                // لا يوجد تقاطع حقيقي (المستقيمان متوازيان)
                return null;
            }

            double x1 = (d1 * b2 - d2 * b1) / det;
            double x2 = (a1 * d2 - a2 * d1) / det;

            return new Point { X = x1, Y = x2 };
        }
        private bool IsFeasible(Point p, List<Lpp_Solver.Models.Constraint> allConstraints)
        {
            double tolerance = 1e-6; // هامش خطا

            foreach (var constraint in allConstraints)
            {
                double lhs = constraint.Coefficients[0] * p.X + constraint.Coefficients[1] * p.Y;
                double rhs = constraint.RightHandSide;

                switch (constraint.Relation)
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
        public GraphicalResult SolveGraphically2D(LPP problem)
        {
            if (problem.Constraints.Any(c => c.Coefficients == null))
            {
                throw new Exception("One or more constraints have null coefficients!");
            }
            if (problem == null)
                throw new ArgumentNullException(nameof(problem), "Problem data cannot be null.");

            if (problem.Constraints == null || problem.Constraints.Count == 0)
                throw new ArgumentException("No constraints provided.");

            var allConstraints = new List<Lpp_Solver.Models.Constraint>(problem.Constraints);

            // ضمان تحديد الربع الأول
            allConstraints.AddRange(new[]
            {
        new Lpp_Solver.Models.Constraint { Coefficients = new double[] { 1, 0 }, Relation = ">=", RightHandSide = 0 },
        new Lpp_Solver.Models.Constraint { Coefficients = new double[] { 0, 1 }, Relation = ">=", RightHandSide = 0 }
    });

            var feasibleVertices = new List<Point>();

            for (int i = 0; i < allConstraints.Count; i++)
            {
                for (int j = i + 1; j < allConstraints.Count; j++)
                {
                    var c1 = allConstraints[i];
                    var c2 = allConstraints[j];

                    var intersectionPoint = SolveTwoEquations(c1, c2);
                    if (intersectionPoint == null) continue;

                    if (IsFeasible(intersectionPoint, allConstraints))
                    {
                        feasibleVertices.Add(intersectionPoint);
                    }
                }
            }

            // إزالة النقاط المكررة
            feasibleVertices = feasibleVertices
                .GroupBy(p => new { X = Math.Round(p.X, 5), Y = Math.Round(p.Y, 5) })
                .Select(g => g.First())
                .ToList();

            if (feasibleVertices.Count == 0)
                return new GraphicalResult { Status = "Infeasible", FeasibleVertices = new List<Point>() };

            var objectiveCoeffs = problem.ObjectiveCoefficients;
            bool isMax = IsMaximization(problem);

            double bestZ = isMax ? double.NegativeInfinity : double.PositiveInfinity;
            Point optimalPoint = null;

            foreach (var p in feasibleVertices)
            {
                p.Z = objectiveCoeffs[0] * p.X + objectiveCoeffs[1] * p.Y;

                if (isMax && p.Z > bestZ)
                {
                    bestZ = p.Z;
                    optimalPoint = p;
                }
                else if (!isMax && p.Z < bestZ)
                {
                    bestZ = p.Z;
                    optimalPoint = p;
                }
            }

            if (optimalPoint == null)
                return new GraphicalResult { Status = "Infeasible", FeasibleVertices = feasibleVertices };

            return new GraphicalResult
            {
                Status = "Optimal",
                ObjectiveValue = optimalPoint.Z,
                FeasibleVertices = feasibleVertices,
                OptimalPoint = optimalPoint,
            };
        }
        private point3D SolveThreeEquations(Lpp_Solver.Models.Constraint C1, Lpp_Solver.Models.Constraint C2, Lpp_Solver.Models.Constraint C3)
        {
            double a1 = C1.Coefficients[0], b1 = C1.Coefficients[1], c11 = C1.Coefficients[2], d1 = C1.RightHandSide;
            double a2 = C2.Coefficients[0], b2 = C2.Coefficients[1], c22 = C2.Coefficients[2], d2 = C2.RightHandSide;
            double a3 = C3.Coefficients[0], b3 = C3.Coefficients[1], c33 = C3.Coefficients[2], d3 = C3.RightHandSide;
            double det = a1 * (b2 * c33 - c22 * b3) - b1 * (a2 * c33 - c22 * a3) + c11 * (a2 * b3 - b2 * a3);
            if (Math.Abs(det) < 1e-9)
                return null;
            double dx = d1 * (b2 * c33 - c22 * b3) - b1 * (d2 * c33 - c22 * d3) + c11 * (d2 * b3 - b2 * d3);
            double dy = a1 * (d2 * c33 - c22 * d3) - d1 * (a2 * c33 - c22 * a3) + c11 * (a2 * d3 - d2 * a3);
            double dz = a1 * (b2 * d3 - d2 * b3) - b1 * (a2 * d3 - d2 * a3) + d1 * (a2 * b3 - b2 * a3);

            double x = dx / det;
            double y = dy / det;
            double z = dz / det;
            return new point3D { X = x, Y = y, Z = z };
        }
        private bool IsFeasible3D(point3D p, List<Lpp_Solver.Models.Constraint> constraints)
        {
            double tolerance = 1e-6; // هامش خطا

            foreach (var constraint in constraints)
            {
                double lhs = constraint.Coefficients[0] * p.X 
                    + constraint.Coefficients[1] * p.Y +constraint.Coefficients[2]*p.Z;
                double rhs = constraint.RightHandSide;

                switch (constraint.Relation)
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
        public GraphicalResult3D SolveGraphically3D(LPP lpp)
        {
            var allConstraints = new List<Lpp_Solver.Models.Constraint>(lpp.Constraints);
            allConstraints.Add(new Lpp_Solver.Models.Constraint { Coefficients = new double[] { 1, 0, 0 }, Relation = ">=", RightHandSide = 0 });
            allConstraints.Add(new Lpp_Solver.Models.Constraint { Coefficients = new double[] { 0, 1, 0 }, Relation = ">=", RightHandSide = 0 });
            allConstraints.Add(new Lpp_Solver.Models.Constraint { Coefficients = new double[] { 0, 0, 1 }, Relation = ">=", RightHandSide = 0 });

            var feasibleVertices = new List<point3D>();
            for (int i = 0; i < allConstraints.Count - 2; i++)
            {
                for (int j = i + 1; j < allConstraints.Count - 1; j++)
                {
                    for (int k = j + 1; k < allConstraints.Count; k++)
                    {
                        var c1 = allConstraints[i];
                        var c2 = allConstraints[j];
                        var c3 = allConstraints[k];

                        var point = SolveThreeEquations(c1, c2, c3);
                        if (point != null)
                        {
                            if (IsFeasible3D(point, allConstraints))
                                feasibleVertices.Add(point);
                        }
                    }
                }
            }
            feasibleVertices = feasibleVertices
                .GroupBy(p => new { X = Math.Round(p.X, 5), Y = Math.Round(p.Y, 5), Z = Math.Round(p.Z, 5) })
                .Select(g => g.First())
                .ToList();
            foreach (var p in feasibleVertices)
            {
                p.F = lpp.ObjectiveCoefficients[0] * p.X +
                           lpp.ObjectiveCoefficients[1] * p.Y +
                           lpp.ObjectiveCoefficients[2] * p.Z;
            }
            bool isMax = IsMaximization(lpp);
            point3D optimal = null;

            if (feasibleVertices.Count > 0)
            {
                optimal = isMax ? feasibleVertices.OrderByDescending(p => p.F).First()
                                 : feasibleVertices.OrderBy(p => p.F).First();
            }

            var result = new GraphicalResult3D
            {
                FeasibleVertices = feasibleVertices,
                OptimalPoint = optimal,
                Status = feasibleVertices.Count == 0 ? "Infeasible" : "Optimal",
                ObjectiveValue = optimal?.F ?? 0,
                VariableValues = new Dictionary<string, double>
        {
            {"x1", optimal?.X ?? 0},
            {"x2", optimal?.Y ?? 0},
            {"x3", optimal?.Z ?? 0}
        }
            };

            return result;
        }
    }
}