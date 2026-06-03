using DataAccessLayer.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace DataAccessLayer.Repositories;

public class CategoryRespository : BaseDAL
{
    public CategoryRespository(IConfiguration configuration) : base(configuration)
    {
    }

    public async Task<IEnumerable<Category>> GetAllCategories()
    {
        const string sql = "SELECT categorie_id, categorie_naam FROM categorie";
        var categories = new List<Category>();

        await using var connection = (SqlConnection)GetConnection();
        await connection.OpenAsync();
        
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            categories.Add(new Category
            {
                CategoryId = reader.GetString(reader.GetOrdinal("categorie_id")),
                CategoryName = reader.GetString(reader.GetOrdinal("categorie_naam"))
            });
        } 
        await connection.CloseAsync();
        
        return categories;
    }

    public async Task<Category> GetCategoryById(string id)
    {
        const string sql = "SELECT categorie_id, categorie_naam FROM categorie WHERE categorie_id = @Id";
        
        await using var connection = (SqlConnection)GetConnection();
        await connection.OpenAsync();
        
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", id);

        await using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new Category
            {
                CategoryId = reader.GetString(reader.GetOrdinal("categorie_id")),
                CategoryName = reader.GetString(reader.GetOrdinal("categorie_naam"))
            };
        }

        return null;
    }

    public async Task AddCategory(Category category)
    {
        const string sql = "INSERT INTO categorie (categorie_id, categorie_naam) VALUES (@id, @name)";

        await using var connection = (SqlConnection)GetConnection();
        await connection.OpenAsync();
        
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", category.CategoryId);
        command.Parameters.AddWithValue("@name", category.CategoryName);
        
        await command.ExecuteNonQueryAsync();
    }

    public async Task EditCategory(Category category)
    {
        const string sql = "UPDATE categorie SET categorie_id = @id, categorie_naam = @name WHERE categorie_id = @id";
        
        await using var connection = (SqlConnection)GetConnection();
        await connection.OpenAsync();
        
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", category.CategoryId);
        command.Parameters.AddWithValue("@name", category.CategoryName);
        
        await command.ExecuteNonQueryAsync();
    }

    public async Task DeleteCategory(string id)
    {
        const string sql =  "DELETE FROM categorie WHERE categorie_id = @id";
        
        await using var connection = (SqlConnection)GetConnection();
        await connection.OpenAsync();
        
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", id);
        
        await command.ExecuteNonQueryAsync();
    }
}