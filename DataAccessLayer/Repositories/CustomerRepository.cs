using DataAccessLayer.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DataAccessLayer.Repositories
{
    public class CustomerRepository : BaseDAL
    {
        public CustomerRepository(IConfiguration configuration) : base(configuration)
        {
        }

        public async Task<IEnumerable<Customer>> GetAllAsync()
        {
            const string sql = 
                "SELECT k.klant_id, k.naam, e.email, COUNT(b.bestelling_id) AS AantalBestellingen"+
                " FROM klant k"+
                " JOIN Account a ON a.klant_id = k.klant_id"+
                " JOIN Email e ON a.email_id = e.email_id"+
                " LEFT JOIN Bestelling b ON b.klant_id = k.klant_id"+
                " GROUP BY k.klant_id, k.naam, e.email";

            var customers = new List<Customer>();

            await using var connection = (SqlConnection)GetConnection();
            await connection.OpenAsync();

            await using var command = new SqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                customers.Add(new Customer
                {
                    Id = reader.GetInt32(reader.GetOrdinal("klant_id")),
                    Naam = reader.GetString(reader.GetOrdinal("naam")),
                    Email = reader.IsDBNull(reader.GetOrdinal("email"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("email")),
                    AantalBestellingen = reader.GetInt32(reader.GetOrdinal("AantalBestellingen"))
                });
            }

            return customers;
        }

        public async Task<Customer?> GetByIdAsync(int id)
        {
            const string sql = @"
        SELECT     k.klant_id, k.naam, k.telefoon, k.adres, k.postcode, k.woonplaats, k.land, e.email
        FROM klant k
        JOIN Account a ON a.klant_id = k.klant_id
        JOIN Email e ON a.email_id = e.email_id
        WHERE k.klant_id = @Id";

            await using var connection = (SqlConnection)GetConnection();
            await connection.OpenAsync();

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", id);

            await using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new Customer
                {
                    Id = reader.GetInt32(reader.GetOrdinal("klant_id")),
                    Naam = reader.GetString(reader.GetOrdinal("naam")),
                    Email = reader.IsDBNull(reader.GetOrdinal("email"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("email")),
                    Telefoonnr = reader.IsDBNull(reader.GetOrdinal("telefoon"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("telefoon")),
                    Adres = reader.GetString(reader.GetOrdinal("adres")),
                    Postcode = reader.GetString(reader.GetOrdinal("postcode")),
                    Woonplaats = reader.GetString(reader.GetOrdinal("woonplaats")),
                    Land = reader.GetString(reader.GetOrdinal("land")),
                };
            }

            return null;
        }

        public async Task AddAsync(Customer customer)
        {
            await using var connection = (SqlConnection)GetConnection();
            await connection.OpenAsync();

            await using var transaction = await connection.BeginTransactionAsync();

            try
            {
                // Email toevoegen
                const string emailSql = @"
            INSERT INTO Email (email)
            VALUES (@Email);
            SELECT CAST(SCOPE_IDENTITY() AS int);";

                var emailCommand = new SqlCommand(emailSql, connection, (SqlTransaction)transaction);
                emailCommand.Parameters.AddWithValue("@Email",
                    customer.Email ?? (object)DBNull.Value);

                var emailId = (int)(await emailCommand.ExecuteScalarAsync())!;

                // Klant toevoegen
                const string customerSql = @"
            INSERT INTO klant (naam, telefoon, adres, postcode, woonplaats, land)
            VALUES (@Name, @Telefoonnr, @Adres, @Postcode, @Woonplaats, @Land);
            SELECT CAST(SCOPE_IDENTITY() AS int);";

                var customerCommand = new SqlCommand(customerSql, connection, (SqlTransaction)transaction);

                customerCommand.Parameters.AddWithValue("@Name",
                    customer.Naam ?? (object)DBNull.Value);

                customerCommand.Parameters.AddWithValue("@Telefoonnr",
                    (object?)customer.Telefoonnr ?? DBNull.Value); 

                customerCommand.Parameters.AddWithValue("@Adres",
                    customer.Adres ?? (object)DBNull.Value);

                customerCommand.Parameters.AddWithValue("@Postcode",
                    (object?)customer.Postcode ?? DBNull.Value);

                customerCommand.Parameters.AddWithValue("@Woonplaats",
                    (object?)customer.Woonplaats ?? DBNull.Value);

                customerCommand.Parameters.AddWithValue("@Land",
                    (object?)customer.Land ?? DBNull.Value);


                var customerId = (int)(await customerCommand.ExecuteScalarAsync())!;

                // Account koppelen
                const string accountSql = @"
            INSERT INTO Account (klant_id, email_id)
            VALUES (@KlantId, @EmailId)";

                var accountCommand = new SqlCommand(accountSql, connection, (SqlTransaction)transaction);

                accountCommand.Parameters.AddWithValue("@KlantId", customerId);
                accountCommand.Parameters.AddWithValue("@EmailId", emailId);

                await accountCommand.ExecuteNonQueryAsync();

                await transaction.CommitAsync();

                customer.Id = customerId;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task UpdateAsync(Customer customer)
        {
            const string sql = @"
                UPDATE k
                SET
                    k.naam = @Name,
                    k.telefoon = @Telefoonnr,
                    k.adres,
                    k.postcode,
                    k.woonplaats,
                    k.land,
                    e.email = @Email
                FROM klant k
                JOIN Account a ON a.klant_id = k.klant_id
                JOIN Email e ON a.email_id = e.email_id
                WHERE k.klant_id = @Id";

            await using var connection = (SqlConnection)GetConnection();
            await connection.OpenAsync();

            await using var command = new SqlCommand(sql, connection);

            command.Parameters.AddWithValue("@Id", customer.Id);
            command.Parameters.AddWithValue("@Name",
                customer.Naam ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Email",
                customer.Email ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Telefoonnr",
                (object?)customer.Telefoonnr ?? DBNull.Value);
            command.Parameters.AddWithValue("@Adres",
                customer.Adres ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Postcode",
                customer.Postcode ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Woonplaats",
                customer.Woonplaats ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Land",
                (object?)customer.Land ?? DBNull.Value);
            

            await command.ExecuteNonQueryAsync();
        }

        public async Task DeleteAsync(int id)
        {
            await using var connection = (SqlConnection)GetConnection();
            await connection.OpenAsync();

            await using var transaction = await connection.BeginTransactionAsync();

            try
            {
                // EmailId ophalen
                const string getEmailSql = @"
                    SELECT a.email_id
                    FROM Account a
                    WHERE a.klant_id = @Id";

                var getEmailCommand = new SqlCommand(
                    getEmailSql,
                    connection,
                    (SqlTransaction)transaction);

                getEmailCommand.Parameters.AddWithValue("@Id", id);

                var emailId = await getEmailCommand.ExecuteScalarAsync();

                // Account verwijderen
                const string deleteAccountSql =
                    "DELETE FROM Account WHERE klant_id = @Id";

                var deleteAccountCommand = new SqlCommand(
                    deleteAccountSql,
                    connection,
                    (SqlTransaction)transaction);

                deleteAccountCommand.Parameters.AddWithValue("@Id", id);

                await deleteAccountCommand.ExecuteNonQueryAsync();

                // Klant verwijderen
                const string deleteCustomerSql =
                    "DELETE FROM klant WHERE klant_id = @Id";

                var deleteCustomerCommand = new SqlCommand(
                    deleteCustomerSql,
                    connection,
                    (SqlTransaction)transaction);

                deleteCustomerCommand.Parameters.AddWithValue("@Id", id);

                await deleteCustomerCommand.ExecuteNonQueryAsync();

                // Email verwijderen
                if (emailId != null)
                {
                    const string deleteEmailSql =
                        "DELETE FROM Email WHERE email_id = @EmailId";

                    var deleteEmailCommand = new SqlCommand(
                        deleteEmailSql,
                        connection,
                        (SqlTransaction)transaction);

                    deleteEmailCommand.Parameters.AddWithValue("@EmailId", (int)emailId);

                    await deleteEmailCommand.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}