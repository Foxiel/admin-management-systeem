using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DataAccessLayer.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace DataAccessLayer.Repositories
{
    public class CustomerRepository : BaseDAL
    {
        public CustomerRepository(IConfiguration configuration) : base(configuration)
        {
        }

        public async Task<IEnumerable<Customer>> GetAllAsync()
        {
            const string sql = "SELECT klant_nummer, klant_naam, klant_email FROM klant";
            var customers = new List<Customer>();

            await using var connection = (SqlConnection)GetConnection();
            await connection.OpenAsync();

            await using var command = new SqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                customers.Add(new Customer
                {
                    Id = reader.GetInt32(reader.GetOrdinal("klant_nummer")),
                    Name = reader.GetString(reader.GetOrdinal("klant_naam")),
                    Email = reader.IsDBNull(reader.GetOrdinal("klant_email"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("klant_email"))
                });
            }

            return customers;
        }

        public async Task<Customer?> GetByIdAsync(int id)
        {
            const string sql = @"
                SELECT klant_nummer, klant_naam, klant_email
                FROM klant
                WHERE klant_nummer = @Id";

            await using var connection = (SqlConnection)GetConnection();
            await connection.OpenAsync();

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", id);

            await using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new Customer
                {
                    Id = reader.GetInt32(reader.GetOrdinal("klant_nummer")),
                    Name = reader.GetString(reader.GetOrdinal("klant_naam")),
                    Email = reader.IsDBNull(reader.GetOrdinal("klant_email"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("klant_email"))
                };
            }

            return null;
        }

        public async Task AddAsync(Customer customer)
        {
            const string sql = @"
                INSERT INTO klant (klant_naam, klant_email)
                VALUES (@Name, @Email);
                SELECT CAST(SCOPE_IDENTITY() AS int);";

            await using var connection = (SqlConnection)GetConnection();
            await connection.OpenAsync();

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Name", customer.Name ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Email", customer.Email ?? (object)DBNull.Value);

            var result = await command.ExecuteScalarAsync();
            if (result != null && int.TryParse(result.ToString(), out var newId))
            {
                customer.Id = newId;
            }
        }

        public async Task UpdateAsync(Customer customer)
        {
            const string sql = @"
                UPDATE klant
                SET klant_naam = @Name,
                    klant_email = @Email
                WHERE klant_nummer = @Id";

            await using var connection = (SqlConnection)GetConnection();
            await connection.OpenAsync();

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", customer.Id);
            command.Parameters.AddWithValue("@Name", customer.Name ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Email", customer.Email ?? (object)DBNull.Value);

            await command.ExecuteNonQueryAsync();
        }

        public async Task DeleteAsync(int id)
        {
            const string sql = "DELETE FROM klant WHERE klant_nummer = @Id";

            await using var connection = (SqlConnection)GetConnection();
            await connection.OpenAsync();

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", id);

            await command.ExecuteNonQueryAsync();
        }
    }
}