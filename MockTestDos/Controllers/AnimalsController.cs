using Microsoft.AspNetCore.Mvc;
using MockTestDos.Exceptions;
using MockTestDos.Models;
using MockTestDos.Repositories;

namespace MockTestDos.Controllers;

public class AnimalsController : ControllerBase
{
    private readonly IAnimalsRepository _animalsRepository;

    public AnimalsController(IAnimalsRepository animalsRepository)
    {
        _animalsRepository = animalsRepository;
    }

    [HttpGet("api/[controller]/{id}")]
    public async Task<IActionResult> GetAnimal(int id)
    {
        try
        {
            if (_animalsRepository.DoesAnimalExistAsync(id).Result == false)
                return NotFound("Animal with given ID doesn't exist. Id: " + id);

            var animal = await _animalsRepository.GetAnimalByIdAsync(id);
            return Ok(animal);
        }
        catch (NotFoundException exception)
        {
            return NotFound(exception.Message);
        }
    }

    [HttpPost("api/[controller]")]
    public async Task<IActionResult> AddAnimal([FromBody] NewAnimalDTO newAnimal)
    {
        try
        {
            if (await _animalsRepository.DoesOwnerExistAsync(newAnimal.OwnerId) == false)
                return NotFound("Owner with given ID doesn't exist. Id: " + newAnimal.OwnerId);

            foreach (var procedure in newAnimal.Procedures)
            {
                if (await _animalsRepository.DoesProcedureExistAsync(procedure.ProcedureId) == false)
                    return NotFound("Procedure with given ID doesn't exist. Id: " + procedure.ProcedureId);
            }

            await _animalsRepository.AddAnimalAsync(newAnimal);
            return Created("Ok", newAnimal);
        }
        catch (NotFoundException exception)
        {
            return NotFound(exception.Message);
        }
        catch (Exception exception)
        {
            return StatusCode(500, "Internal server error: " + exception.Message);
        }
    }
}