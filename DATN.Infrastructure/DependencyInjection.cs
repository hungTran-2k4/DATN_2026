using System.Data.Common;
using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using DATN.Domain.Interfaces;
using DATN.Application.Interfaces.Auth;
using DATN.Application.Interfaces.Services;
using DATN.Infrastructure.Persistence.Repositories.Users;
using DATN.Infrastructure.Persistence.Repositories.Roles;
using DATN.Infrastructure.Persistence.Repositories.Auth;
using DATN.Infrastructure.Services;
using DATN_2026.DatabaseSpecific;
using SD.LLBLGen.Pro.ORMSupportClasses;
using SD.LLBLGen.Pro.DQE.PostgreSql;

namespace DATN.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // LLBLGen Config
        DbProviderFactories.RegisterFactory("Npgsql", Npgsql.NpgsqlFactory.Instance);
        RuntimeConfiguration.ConfigureDQE<PostgreSqlDQEConfiguration>(c =>
        {
            c.AddDbProviderFactory(typeof(NpgsqlFactory));
            c.SetTraceLevel(TraceLevel.Verbose);
        });

        // Firebase Config
        // Make sure it's only initialized once
        if (FirebaseApp.DefaultInstance == null)
        {
            FirebaseApp.Create(new AppOptions
            {
                Credential = GoogleCredential.FromFile("firebase-adminsdk.json")
            });
        }

        services.AddScoped<IDataAccessAdapterFactory, DataAccessAdapterFactory>();
        
        // Register IDataAccessAdapter using factory
        services.AddScoped<IDataAccessAdapter>(provider =>
        {
            var factory = provider.GetRequiredService<IDataAccessAdapterFactory>();
            return (IDataAccessAdapter)factory.CreateAdapter();
        });
        
        services.AddScoped<DataAccessAdapter>(provider =>
        {
            var factory = provider.GetRequiredService<IDataAccessAdapterFactory>();
            return (DataAccessAdapter)factory.CreateAdapter();
        });

        // Register AutoMapper profiles in Infrastructure
        services.AddAutoMapper(System.Reflection.Assembly.GetExecutingAssembly());

        // Register repositories

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        
        // Register Auth services
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        // Background Service
        services.AddHostedService<TokenCleanupService>();

        // Email Service
        services.AddScoped<IEmailService, SmtpEmailService>();

        return services;
    }
}
