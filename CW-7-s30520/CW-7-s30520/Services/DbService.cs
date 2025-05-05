using CW_7_s30520.Exceptions;
using CW_7_s30520.Models;
using CW_7_s30520.Models.DTOs;
using Microsoft.Data.SqlClient;

namespace CW_7_s30520.Services;


public interface IDbService
{
    public Task<IEnumerable<TripGetDTO>> GetTripsAsync();
    public Task<List<TripGetDTO>> GetClientsTripsByIdAsync(int id);
    public Task<Client> CreateClientAsync(ClientCreateDTO client);
    public Task AssignClientToTripAsync(int clientId, int tripId);
    public Task DeleteClientFromTripAsync(int clientId, int tripId);
}
public class DbService(IConfiguration config) : IDbService
{
    private readonly string? _connectionString = config.GetConnectionString("Default");
    
    public async Task<IEnumerable<TripGetDTO>> GetTripsAsync()
    {
        var result = new List<TripGetDTO>();

        await using var connection = new SqlConnection(_connectionString);
        
        //zapytanie pobiera dane wycieczek w tym kraje w jakich się odbywają
        const string sql = "SELECT t.IdTrip, t.Name, t.Description, t.DateFrom, t.DateTo, t.MaxPeople, c.Name AS CountryName" +
                           "FROM Trip t LEFT OUTER JOIN Country_Trip ct ON t.IdTrip = ct.IdTrip" + 
                           "LEFT OUTER JOIN Country c ON ct.IdCountry = c.IdCountry";
        
        await using var command = new SqlCommand(sql, connection);
        await connection.OpenAsync();
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            result.Add(new TripGetDTO
            {
                IdTrip = reader.GetInt32(0),
                Name = reader.GetString(1),
                Description = reader.GetString(2),
                DateFrom = reader.GetDateTime(3),
                DateTo = reader.GetDateTime(4),
                MaxPeople = reader.GetInt32(5),
                Country = reader.IsDBNull(6) ? null : reader.GetString(6)
            });
        }
        
        return result;
    }

    public async Task<List<TripGetDTO>> GetClientsTripsByIdAsync(int id)
    {
        var result = new List<TripGetDTO>();
        
        await using var connection = new SqlConnection(_connectionString);

        //zapytanie pobiera dane wycieczek w tym kraje w jakich się odbywają, jeśli klient o wskazanym id na nich był
        const string sql = "SELECT t.IdTrip, t.Name, t.Description, t.DateFrom, t.DateTo, t.MaxPeople, c.Name AS CountryName" +
                           " FROM Trip t INNER JOIN Client_Trip ct ON t.IdTrip = ct.IdTrip" + 
                           " LEFT OUTER JOIN Country_Trip ctt ON t.IdTrip = ctt.IdTrip" + 
                           " LEFT OUTER JOIN Country c ON c.IdCountry = ctt.IdCountry" + 
                           " WHERE ct.IdClient = @ClientId";
        
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@ClientId", id);
        await connection.OpenAsync();
        await using var reader = await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            throw new NotFoundException($"Klient o: {id} nie istnieje");
        }

        while (await reader.ReadAsync())
        {
            result.Add(new TripGetDTO()
            {
                IdTrip = reader.GetInt32(0),
                Name = reader.GetString(1),
                Description = reader.GetString(2),
                DateFrom = reader.GetDateTime(3),
                DateTo = reader.GetDateTime(4),
                MaxPeople = reader.GetInt32(5),
                Country = reader.IsDBNull(6) ? null : reader.GetString(6)
            });
        }

        if (result.Count == 0)
        {
            throw new NotFoundException($"Klient o: {id} nie był zapisany na żadną wycieczkę");
        }
        
        return result;
    }

    public async Task<Client> CreateClientAsync(ClientCreateDTO client)
    {
        await using var connection = new SqlConnection(_connectionString);
        
        //wstawienie klienta oraz pobranie jego id
        const string sql = "INSERT INTO Client (FirstName, LastName, Email, Telephone, Pesel) " +
                           " VALUES (@FirstName, @LastName, @Email, @Telephone, @Pesel)" +
                           " SELECT scope_identity()";
        
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@FirstName", client.FirstName);
        command.Parameters.AddWithValue("@LastName", client.LastName);
        command.Parameters.AddWithValue("@Email", client.Email);
        command.Parameters.AddWithValue("@Telephone", client.Telephone);
        command.Parameters.AddWithValue("@Pesel", client.Pesel);
        
        await connection.OpenAsync();
        var idClient = Convert.ToInt32(await command.ExecuteScalarAsync());
        
        return new Client
        {
            IdClient = idClient,
            FirstName = client.FirstName,
            LastName = client.LastName,
            Email = client.Email,
            Telephone = client.Telephone,
            Pesel = client.Pesel
        };
    }

    public async Task AssignClientToTripAsync(int idClient, int idTrip)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        
        //sprawdzenie czy klient istnieje
        var sql1 = "SELECT COUNT(1) FROM Client WHERE IdClient = @IdClient";
        var command1 = new SqlCommand(sql1, connection);
        
        command1.Parameters.AddWithValue("@ClientId", idClient);
        
        var clientCount = Convert.ToInt32(await command1.ExecuteScalarAsync());
        if (clientCount == 0)
        {
            throw new NotFoundException("Nie znaleziono klienta");
        }
        
        //sprawdzenie czy wycieczka istnieje
        var sql2 = "SELECT COUNT(1) FROM Trip WHERE IdTrip = @TripId";
        var command2 = new SqlCommand(sql2, connection);
        command2.Parameters.AddWithValue("@ClientId", idClient);
        
        var tripCount = Convert.ToInt32(await command2.ExecuteScalarAsync());
        if (tripCount == 0)
        {
            throw new NotFoundException("Nie znaleziono wycieczki");
        }
        
        //pobranie limitu osób wycieczki o danym id
        var sql3 = "SELECT MaxPeople FROM Trip WHERE IdTrip = @TripId";
        var command3 = new SqlCommand(sql3, connection);
        command3.Parameters.AddWithValue("@TripId", idTrip);
        var maxAttendeeCount = Convert.ToInt32(await command3.ExecuteScalarAsync());
        
        //pobranie aktualnej ilość osób zapisanych na wycieczkę o danym id
        var sql4 = "SELECT COUNT(1) FROM Client_Trip WHERE IdTrip = @TripId";
        var command4 = new SqlCommand(sql4, connection);
        var currentAttendeeCount = Convert.ToInt32(await command4.ExecuteScalarAsync());

        if (currentAttendeeCount >= maxAttendeeCount)
        {
            throw new ToManyParticipantsException($"Wycieczka o id: {idTrip} ma już maksymalną ilość uczestników");
        }
        
        //wstawienie rekordu do tabeli Client_Trip
        var insertionQuery = "INSERT INTO Client_Trip (IdClient, IdTrip, RegisteredAt) VALUES (@ClientId, @TripId, @RegisteredAt)";
        var insertionCommand = new SqlCommand(insertionQuery, connection);
        
        insertionCommand.Parameters.AddWithValue("@ClientId", idClient);
        insertionCommand.Parameters.AddWithValue("@TripId", idTrip);
        insertionCommand.Parameters.AddWithValue("@RegisteredAt", DateTime.Now);
        
        await insertionCommand.ExecuteNonQueryAsync();
    }

    public async Task DeleteClientFromTripAsync(int clientId, int tripId)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        //sprawdzenie czy rejestracja klienta istnieje
        var sql1 = "SELECT COUNT(1) FROM Client_Trip WHERE IdClient = @ClientId AND IdTrip = @TripId";
        var command1 = new SqlCommand(sql1, connection);
        
        command1.Parameters.AddWithValue("@ClientId", clientId);
        command1.Parameters.AddWithValue("@TripId", tripId);

        var registrationCount = Convert.ToInt32(await command1.ExecuteScalarAsync());

        if (registrationCount == 0)
        {
            throw new NotFoundException("Nie znaleziono rejestracji");
        }

        //usunięcie rejestracji
        var sql2 = "DELETE FROM Client_Trip WHERE IdClient = @ClientId AND IdTrip = @TripId";
        var command2 = new SqlCommand(sql2, connection);
        command2.Parameters.AddWithValue("@ClientId", clientId);
        command2.Parameters.AddWithValue("@TripId", tripId);

        await command2.ExecuteNonQueryAsync();
    }
}