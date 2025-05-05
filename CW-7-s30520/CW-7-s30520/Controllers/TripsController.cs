using CW_7_s30520.Services;
using Microsoft.AspNetCore.Mvc;

namespace CW_7_s30520.Controllers;

[ApiController]
[Route("[controller]")]
public class TripsController(IDbService dbService) : ControllerBase
{
    [HttpGet]
    //GET => /api/trips
    public async Task<IActionResult> GetTripsAsync()
    {
        return Ok(await dbService.GetTripsAsync());
    }
}