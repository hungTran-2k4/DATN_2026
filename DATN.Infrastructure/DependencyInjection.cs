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
using DATN.Infrastructure.Persistence.Repositories.Products;
using DATN.Infrastructure.Persistence.Repositories.Marketing;
using DATN.Infrastructure.Persistence.Repositories.Shops;
using DATN.Infrastructure.Persistence.Repositories.Categories;
using DATN.Infrastructure.Persistence.Repositories.Orders;
using DATN.Infrastructure.Services;
using DATN.Infrastructure.Services.Payment;
using DATN.Infrastructure.Services.Shipping;
using DATN.Infrastructure.Persistence.Repositories.Audit;
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
        // Khởi tạo Firebase, Catch exception để tránh sập toàn bộ ứng dụng nếu thiếu file credential
        if (FirebaseApp.DefaultInstance == null)
        {
            try
            {
                var firebaseOptions = new AppOptions();
                var firebaseCreds = configuration["Firebase:Credentials"]; // Lấy từ Environment Variable trên Azure thay vì file

                if (!string.IsNullOrEmpty(firebaseCreds))
                {
                    firebaseOptions.Credential = GoogleCredential.FromJson(firebaseCreds);
                }
                else if (File.Exists("firebase-adminsdk.json"))
                {
                    firebaseOptions.Credential = GoogleCredential.FromFile("firebase-adminsdk.json");
                }
                else
                {
                    // Fall back
                    firebaseOptions.Credential = GoogleCredential.GetApplicationDefault();
                }

                FirebaseApp.Create(firebaseOptions);
            }
            catch (Exception ex)
            {
                // Bỏ qua lỗi sập chương trình lúc khởi động, chỉ lưu vết lại.
                Console.WriteLine("Lỗi khởi tạo Firebase: " + ex.Message);
            }
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
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IShopRepository, ShopRepository>();
        services.AddScoped<IUserAddressRepository, UserAddressRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IProductVariantRepository, ProductVariantRepository>();
        services.AddScoped<ICartRepository, CartRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IReviewRepository, ReviewRepository>();
        services.AddScoped<IWishlistRepository, WishlistRepository>();
        services.AddScoped<IBrandRepository, BrandRepository>();
        services.AddScoped<IVoucherRepository, VoucherRepository>();
        services.AddScoped<IStockRepository, StockRepository>();
        services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IUserSessionRepository, UserSessionRepository>();
        services.AddScoped<IWalletRepository, WalletRepository>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<IShipmentRepository, ShipmentRepository>();

        // Register Auth services
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        // Background Service
        services.AddHostedService<TokenCleanupService>();
        services.AddHostedService<WalletEscrowService>();

        // Email Service
        services.AddScoped<IEmailService, SmtpEmailService>();

        // Storage Service (Azure Blob)
        services.AddScoped<IStorageService, AzureBlobStorageService>();

        // Cache
        services.AddSingleton<ICacheService, MemoryCacheService>();

        // Payment Gateway — Provider Pattern
        services.AddScoped<IPaymentProvider, VNPayProvider>(); // Thêm MoMoProvider ở đây khi cần
        services.AddScoped<IPaymentProviderFactory, PaymentProviderFactory>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IStatisticsService, StatisticsService>();

        // Shipping Provider — Strategy Pattern (giống Payment)
        services.AddHttpClient("GHN"); // HttpClientFactory cho GHN API
        services.AddScoped<IShippingProvider, GHNProvider>();

        // Unit of Work (transaction support)
        services.AddScoped<IUnitOfWork, LLBLGenUnitOfWork>();

        return services;
    }
}
