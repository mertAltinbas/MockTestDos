using System.Data.Common;
using Microsoft.Data.SqlClient;
using MockTestDos.Exceptions;
using MockTestDos.Models;
using MockTestUno.Controllers;

namespace MockTestDos.Repositories;

public class AnimalsRepository : IAnimalsRepository
{
    private readonly string? _connectionString;

    public AnimalsRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection");
    }

    public async Task<bool> DoesAnimalExistAsync(int id)
    {
        var query = "SELECT 1 FROM Animal WHERE ID = @ID";

        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@ID", id);

        await connection.OpenAsync();
        var result = await command.ExecuteScalarAsync();

        return result is not null;
    }

    public async Task<bool> DoesOwnerExistAsync(int id)
    {
        var query = "SELECT 1 FROM Owner WHERE ID = @ID";

        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@ID", id);

        await connection.OpenAsync();
        var result = await command.ExecuteScalarAsync();

        return result is not null;
    }

    public async Task<bool> DoesProcedureExistAsync(int id)
    {
        var query = "SELECT 1 FROM [Procedure] WHERE ID = @ID";

        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@ID", id);

        await connection.OpenAsync();
        var result = await command.ExecuteScalarAsync();

        return result is not null;
    }

    public async Task<AnimalDTO> GetAnimalByIdAsync(int id)
    {
        var query = @"SELECT
           Animal.ID AS AnimalID,
           Animal.Name AS AnimalName,
           Type,
           AdmissionDate,
           Owner.ID as OwnerID,
           FirstName,
           LastName,
           Date,
           [Procedure].Name AS ProcedureName,
           Description
          FROM Animal
           JOIN Owner ON Owner.ID = Animal.Owner_ID
           JOIN Procedure_Animal ON Procedure_Animal.Animal_ID = Animal.ID
           LEFT JOIN [Procedure] ON [Procedure].ID = Procedure_Animal.Procedure_ID
          WHERE Animal.ID = @animalId";

        AnimalDTO? animals = null;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(query, connection);
        await connection.OpenAsync();

        command.Parameters.AddWithValue("@animalId", id);
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            if (animals is null)
            {
                animals = new AnimalDTO()
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Type = reader.GetString(2),
                    AdmissionDate = reader.GetDateTime(3),
                    Owner = new OwnerDTO()
                    {
                        Id = reader.GetInt32(4),
                        FirstName = reader.GetString(5),
                        LastName = reader.GetString(6)
                    },
                    Procedures = new List<ProceduresDTO>()
                };
            }

            animals.Procedures.Add(new ProceduresDTO()
            {
                Name = reader.IsDBNull(8) ? string.Empty : reader.GetString(8),
                Description = reader.IsDBNull(9) ? string.Empty : reader.GetString(9),
                Date = reader.IsDBNull(7) ? DateTime.MinValue : reader.GetDateTime(7)
            });
        }

        if (animals is null)
            throw new NotFoundException("Animal not found");

        return animals;
    }

    public async Task AddAnimalAsync(NewAnimalDTO newAnimal)
    {
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand();

        command.Connection = connection;
        await connection.OpenAsync();

        DbTransaction transaction = connection.BeginTransaction();
        command.Transaction = transaction as SqlTransaction;

        try
        {
            command.Parameters.Clear();
            command.CommandText = "select * from Owner where ID = @ownerId";
            command.Parameters.AddWithValue("@ownerId", newAnimal.OwnerId);

            var ownerId = await command.ExecuteScalarAsync();
            if (ownerId is null)
                throw new NotFoundException($"Owner with given ID - {newAnimal.OwnerId} doesn't exist");

            command.Parameters.Clear();
            command.CommandText = "INSERT INTO Animal OUTPUT INSERTED.ID VALUES(@Name, @Type, @AdmissionDate, @OwnerId);";
            command.Parameters.AddWithValue("@Name", newAnimal.Name);
            command.Parameters.AddWithValue("@Type", newAnimal.Type);
            command.Parameters.AddWithValue("@AdmissionDate", newAnimal.AdmissionDate);
            command.Parameters.AddWithValue("@OwnerId", newAnimal.OwnerId);

            var animalId = await command.ExecuteScalarAsync();

            foreach (var procedure in newAnimal.Procedures)
            {
                command.Parameters.Clear();
                command.CommandText = "INSERT INTO Procedure_Animal VALUES(@ProcedureId, @AnimalId, @Date)";
                command.Parameters.AddWithValue("@ProcedureId", procedure.ProcedureId);
                command.Parameters.AddWithValue("@AnimalId", animalId);
                command.Parameters.AddWithValue("@Date", procedure.Date);

                await command.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync();
            throw new ConflictException($"Transaction error: {e.Message}");
        }
    }
}