using System.Data.Common;
using Npgsql;

namespace Dragons;

public sealed class DbConnectionProvider
{
    private readonly string m_connectionString;

    public DbConnectionProvider(string connectionString)
    {
       m_connectionString = connectionString;
    }

    public DbConnection GetConnection()
    {
        return new NpgsqlConnection(m_connectionString);
    }
}