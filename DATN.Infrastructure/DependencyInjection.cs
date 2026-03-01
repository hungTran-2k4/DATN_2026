
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MyProject.Application.Interfaces.Games;
using MyProject.Application.Interfaces.Publishers;
using MyProject.Application.Interfaces.Auth;
using MyProject.Application.Interfaces.Users;
using MyProject.Application.Interfaces.Roles;
using MyProject.Infrastructure.Persistence.Repositories.Publishers;
using MyProject.Infrastructure.Persistence.Repositories.Games;
using MyProject.Infrastructure.Persistence.Repositories.Users;
using MyProject.Infrastructure.Persistence.Repositories.Roles;
using MyProject.Infrastructure.Persistence.Repositories.Auth;
using MyProject.Infrastructure.Services;
using AutoMapper;
using DATN.DatabaseSpecific;
using Microsoft.Extensions.Logging;
using MyProject.Application.Interfaces.Services;
using SD.LLBLGen.Pro.ORMSupportClasses;

namespace MyProject.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
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

        // Register repositories
        services.AddScoped<IGameRepository, GameRepository>();
        services.AddScoped<IPublisherRepository, PublisherRepository>();
        
        // Register Auth services
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();


        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        
        // Background Service
        services.AddHostedService<TokenCleanupService>();

        // Email Service
        services.AddScoped<IEmailService, SmtpEmailService>();

        return services;
    }
}
