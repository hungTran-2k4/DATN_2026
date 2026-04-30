using System.Configuration;
using DATN_2026.DatabaseSpecific;
using Microsoft.Extensions.Configuration;
using DATN.Application.Interfaces.Services;

namespace DATN.Infrastructure.Services
{
    public class DataAccessAdapterFactory(IConfiguration configuration) : IDataAccessAdapterFactory
    {
        private readonly string _connectionString = configuration.GetConnectionString("DefaultConnection") 
            + ";Pooling=true;Minimum Pool Size=2;Maximum Pool Size=20;Connection Idle Lifetime=60;Connection Pruning Interval=10;Keepalive=30;";

       // ?? throw new ArgumentNullException(nameof(configuration));
        // Implementation cho LLBLGen
        public IDisposable CreateAdapter()
        {
            return new DataAccessAdapter(_connectionString);
        }
    }
}