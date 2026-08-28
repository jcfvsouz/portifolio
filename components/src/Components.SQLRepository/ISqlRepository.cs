using System.Data;

namespace Components.SQLRepository;

public interface ISqlRepository : IDisposable
{
    IDbConnection Connection { get; }
}
