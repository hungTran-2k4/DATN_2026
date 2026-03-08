using System.Configuration;
using DATN.DatabaseSpecific;
using Microsoft.Extensions.Configuration;
using DATN.Application.Interfaces.Services;

namespace DATN.Infrastructure.Services
{
    public class DataAccessAdapterFactory(IConfiguration configuration) : IDataAccessAdapterFactory
    {
        private readonly string _connectionString = configuration.GetConnectionString("DefaultConnection") 
            + ";Pooling=false"; // Disable pooling to avoid disposed object errors with Supabase pooler/Npgsql

       // ?? throw new ArgumentNullException(nameof(configuration));
        // Implementation cho LLBLGen
        public IDisposable CreateAdapter()
        {
            return new DataAccessAdapter(_connectionString);
        }
    }
}