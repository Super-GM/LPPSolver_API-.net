using Microsoft.AspNetCore.Mvc;
using Lpp_Solver.Models;
using Lpp_Solver.services;
using System;
using System.Linq;
using System.Diagnostics;
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
        [HttpPost("Numerical")]
        public IActionResult SolveNumerically([FromBody] LPP lpp)
        {
            try
            {
                var result = _solverservice.SolveNumerically(lpp);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message, StackTrace = ex.StackTrace });
            }
        }

        [HttpPost("graphical2D")]
        public IActionResult SolveGraphically2D([FromBody] LPP lpp)
        {
            try
            {
                var result = _solverservice.SolveGraphically2D(lpp);
                return Ok(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine("🔥 ERROR MESSAGE: " + ex.Message);
                Console.WriteLine("📜 STACK TRACE: " + ex.StackTrace);
                if (ex.InnerException != null)
                {
                    Console.WriteLine("💥 INNER EXCEPTION: " + ex.InnerException.Message);
                }

                return StatusCode(500, new
                {
                    error = ex.Message,
                    stackTrace = ex.StackTrace,
                    inner = ex.InnerException?.Message
                });
            }
        }
        [HttpPost("graphical3D")]
        public IActionResult SolveGraphically3D([FromBody] LPP lpp)
        {
            try
            {
                var result = _solverservice.SolveGraphically3D(lpp);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message, StackTrace = ex.StackTrace });
            }
        }

    }
}
