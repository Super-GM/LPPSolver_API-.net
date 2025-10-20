using Microsoft.AspNetCore.Mvc;
using Lpp_Solver.Models;
using Lpp_Solver.services;
using System;
using System.Linq;
namespace Lpp_Solver.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LPPsolverController : ControllerBase
    {
        private readonly ILPSolverservice _solverservice;

        public LPPsolverController(ILPSolverservice solverservice)
        {
            _solverservice = solverservice;
        }

        [HttpPost]
        public IActionResult Solve([FromBody] LPP lpp)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            if (lpp.Objectivecoefficient == null || lpp.Objectivecoefficient.Length < 2)
            {
                return BadRequest(new { error = "identify at least two variables" });
            }
            try
            {
                SolutionResult result = _solverservice.Solve(lpp);
                    return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = $"input data error : {ex.Message}" });
            }
            catch (Exception)
            {
                return StatusCode(500, new { error = "Unexpected internal error while processing the issue" });
            }

        }

    }
}
