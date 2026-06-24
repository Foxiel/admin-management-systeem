using DataAccessLayer.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DataAccessLayer.Repositories
{
    public class LeverancierRepository : dbContext
    {
        public LeverancierRepository()
        {
        }

        public async Task<IEnumerable<Leverancier>> GetAllAsync()
        {
            const string sql = @"
                SELECT leverancier_id, naam
                FROM leverancier";

            var leveranciers = new List<Leverancier>();

            await using var connection = (SqlConnection)GetConnection();
            await connection.OpenAsync();

            await using var command = new SqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                leveranciers.Add(new Leverancier
                {
                    Id = reader.GetInt32(reader.GetOrdinal("leverancier_id")),
                    Naam = reader.GetString(reader.GetOrdinal("naam")),
                });
            }

            return leveranciers;
        }

        public async Task<Leverancier?> GetByIdAsync(int id)
        {
            const string sql = @"
                SELECT leverancier_id, naam
                FROM leverancier
                WHERE leverancier_id = @Id";

            await using var connection = (SqlConnection)GetConnection();
            await connection.OpenAsync();

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", id);

            await using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new Leverancier
                {
                    Id = reader.GetInt32(reader.GetOrdinal("leverancier_id")),
                    Naam = reader.GetString(reader.GetOrdinal("naam")),
                };
            }

            return null;
        }

        public async Task AddAsync(Leverancier leverancier)
        {
            const string sql = @"
                INSERT INTO leverancier (naam)
                VALUES (@Naam);
                SELECT CAST(SCOPE_IDENTITY() AS int);";

            await using var connection = (SqlConnection)GetConnection();
            await connection.OpenAsync();

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Naam", leverancier.Naam ?? (object)DBNull.Value);

            var id = (int)(await command.ExecuteScalarAsync())!;
            leverancier.Id = id;
        }

        public async Task UpdateAsync(Leverancier leverancier)
        {
            const string sql = @"
                UPDATE leverancier
                SET naam = @Naam
                WHERE leverancier_id = @Id";

            await using var connection = (SqlConnection)GetConnection();
            await connection.OpenAsync();

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", leverancier.Id);
            command.Parameters.AddWithValue("@Naam", leverancier.Naam ?? (object)DBNull.Value);

            await command.ExecuteNonQueryAsync();
        }

        public async Task DeleteAsync(int id)
        {
            const string sql = @"
                DELETE FROM leverancier
                WHERE leverancier_id = @Id";

            await using var connection = (SqlConnection)GetConnection();
            await connection.OpenAsync();

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", id);

            await command.ExecuteNonQueryAsync();
        }
    }
}
