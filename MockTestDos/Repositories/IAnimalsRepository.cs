using MockTestDos.Models;

namespace MockTestDos.Repositories;

public interface IAnimalsRepository
{
    Task<AnimalDTO>GetAnimalByIdAsync(int id);
    Task<bool> DoesAnimalExistAsync(int id);
    Task<bool> DoesOwnerExistAsync(int id);
    Task<bool> DoesProcedureExistAsync(int id);
    
    Task AddAnimalAsync(NewAnimalDTO newAnimal);
}