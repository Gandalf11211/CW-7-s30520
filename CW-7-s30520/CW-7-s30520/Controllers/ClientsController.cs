using CW_7_s30520.Exceptions;
using CW_7_s30520.Models.DTOs;
using CW_7_s30520.Services;
using Microsoft.AspNetCore.Mvc;

namespace CW_7_s30520.Controllers;

[ApiController]
[Route("[controller]")]
public class ClientsController(IDbService dbService) : ControllerBase
{
    [HttpGet]
    [Route("{id}/trips")]
    //GET => /api/clients/{id}/trips
    public async Task<IActionResult> GetClientsTrips([FromRoute] int id)
    {
        try
        {
            return Ok(await dbService.GetClientsTripsByIdAsync(id));
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost]
    //POST => /api/clients
    public async Task<IActionResult> CreateClient([FromBody] ClientCreateDTO body)
    {
        var client = await dbService.CreateClientAsync(body);

        return Created($"clients/{client.IdClient}", client);
    }

    [HttpPut]
    [Route("{id}/trips/{tripId}")]
    //PUT => /api/clients/{id}/trips/{tripId}
    public async Task<IActionResult> AssignClientToTrip([FromRoute] int id, [FromRoute] int tripId)
    {
        try
        {
            await dbService.AssignClientToTripAsync(id, tripId);
            return NoContent();
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
        catch (ToManyParticipantsException)
        {
            return BadRequest($"The trip with id: {tripId} has got max participants.");
        }
    }

    [HttpDelete]
    [Route("{id}/trips/{tripId}")]
    //DELETE => /api/clients/{id}/trips/{tripId}
    public async Task<IActionResult> DeleteClientFromTrip([FromRoute] int id, [FromRoute] int tripId)
    {
        try
        {
            await dbService.DeleteClientFromTripAsync(id, tripId);
            return NoContent();
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
    }
}