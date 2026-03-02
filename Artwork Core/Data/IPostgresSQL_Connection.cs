using Npgsql;
using System.Data;

namespace Artwork_Core.Data
{
    public interface IPostgresSqlConnection
    {
        NpgsqlConnection CreateConnection();
    }
}
