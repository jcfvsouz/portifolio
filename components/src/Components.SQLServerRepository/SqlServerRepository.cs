using Components.SQLRepository;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace Components.SQLServerRepository;

public class SqlServerRepository(IConfiguration configuration) : ISqlRepository
{
    private readonly string _connectionString = configuration.GetConnectionString("SqlServer")!;

    private IDbConnection? _connection = null;
    public IDbConnection Connection
    {
        get
        {
            _connection ??= new SqlConnection(_connectionString);
            return _connection;
        }
    }

    public void Dispose()
    {
        _connection?.Close();
        _connection?.Dispose();
    }
}
