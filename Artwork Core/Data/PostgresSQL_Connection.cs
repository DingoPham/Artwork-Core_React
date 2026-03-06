using Artwork_Core.Models;
using Npgsql;
using System.Data;

namespace Artwork_Core.Data
{
    public class PostgressSQLConnection : IPostgresSqlConnection
    {
        private readonly string _connectionString;

        public PostgressSQLConnection(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public NpgsqlConnection CreateConnection()
        {
            return new NpgsqlConnection(_connectionString);
        }
    }
}
