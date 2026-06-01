using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace DataAccessLayer
{
    public abstract class BaseDAL
    {
        private readonly string _connectionString;

        protected BaseDAL(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("Connection")
                                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        }

        protected IDbConnection GetConnection()
        {
            return new SqlConnection(_connectionString);
        }
    }
}