//Gemaakt door Fabian

using DataAccessLayer.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace DataAccessLayer.Repositories;

public class CategoryRespository : dbContext
{
    public CategoryRespository()
    {
    }

    public async Task<IEnumerable<Category>> GetAllCategories()
    {
        var sql = "SELECT categorie_id, naam FROM categorie";
        sql += " ORDER BY naam";
        var categories = new List<Category>();

        await using var connection = (SqlConnection)GetConnection();
        await connection.OpenAsync();
        
        await using var command = new SqlCommand(sql, connection);

        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            categories.Add(new Category
            {
                Id = reader.GetInt32(reader.GetOrdinal("categorie_id")),
                Naam = reader.GetString(reader.GetOrdinal("naam"))
            });
        } 
        await connection.CloseAsync();
        
        return categories;
    }

    public async Task<Category?> GetCategoryById(int id)
    {
        const string sql = "SELECT categorie_id, naam FROM categorie WHERE categorie_id = @Id";
        
        await using var connection = (SqlConnection)GetConnection();
        await connection.OpenAsync();
        
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", id);

        await using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new Category
            {
                Id = reader.GetInt32(reader.GetOrdinal("categorie_id")),
                Naam = reader.GetString(reader.GetOrdinal("naam"))
            };
        }

        return null!;
    }

    public async Task AddCategory(Category category)
    {
        const string sql = "INSERT INTO categorie (naam) VALUES (@name)";

        await using var connection = (SqlConnection)GetConnection();
        await connection.OpenAsync();
        
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@name", category.Naam);
        
        await command.ExecuteNonQueryAsync();
    }

    public async Task EditCategory(Category category)
    {
        const string sql = "UPDATE categorie SET naam = @name WHERE categorie_id = @id";
        
        await using var connection = (SqlConnection)GetConnection();
        await connection.OpenAsync();
        
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", category.Id);
        command.Parameters.AddWithValue("@name", category.Naam);
        
        await command.ExecuteNonQueryAsync();
    }

    public async Task DeleteCategory(int id)
    {
        const string sql =  "DELETE FROM categorie WHERE categorie_id = @id";
        
        await using var connection = (SqlConnection)GetConnection();
        await connection.OpenAsync();
        
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", id);
        
        await command.ExecuteNonQueryAsync();
    }
}