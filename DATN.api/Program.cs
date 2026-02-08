using System.Data.Common;
using System.Diagnostics;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Npgsql;
using SD.LLBLGen.Pro.DQE.PostgreSql;
using SD.LLBLGen.Pro.ORMSupportClasses;
using MyProject.Infrastructure;
using MyProject.Application.Mapping;
using MyProject.Infrastructure.Mapping;
using Microsoft.OpenApi;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;

// Enable Legacy Timestamp Behavior for Npgsql to handle UTC/Unspecified DateTime mismatch
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Add Swagger/OpenAPI with JWT support
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Study API",
        Version = "v1",
        Description = "API với JWT Authentication"
    });

    // Thêm JWT Bearer Authentication vào Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Nhập JWT token. Ví dụ: eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
    });

    //options.AddSecurityRequirement(new OpenApiSecurityRequirement
    //{
    //    {
    //        new OpenApiSecurityScheme
    //        {
    //            Reference = new OpenApiReference
    //            {
    //                Type = ReferenceType.SecurityScheme,
    //                Id = "Bearer"
    //            }
    //        },
    //        Array.Empty<string>()
    //    }
    //});
});

// Add JWT Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            if (context.Request.Cookies.ContainsKey("access_token"))
            {
                context.Token = context.Request.Cookies["access_token"];
            }
            return Task.CompletedTask;
        }
    };

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key is not configured")))
    };
});

builder.Services.AddAuthorization();

// Add MediatR - Register Application assembly where handlers are located
builder.Services.AddMediatR(cfg =>
{
    // Register Application assembly (contains handlers)
    cfg.RegisterServicesFromAssembly(typeof(MyProject.Application.Features.Game.Handler.GetGamesPagingHandler).Assembly);
    cfg.RegisterServicesFromAssembly(typeof(MyProject.Application.Features.Game.Handler.CreateGameCommandHandler).Assembly);
    cfg.RegisterServicesFromAssembly(typeof(MyProject.Application.Features.Game.Handler.GetGameByIdHandle).Assembly);
    cfg.RegisterServicesFromAssemblies(typeof(MyProject.Application.Features.Auth.Handlers.RegisterHandler).Assembly);
    cfg.RegisterServicesFromAssemblies(typeof(MyProject.Application.Features.Auth.Handlers.LoginHandler).Assembly);
    cfg.RegisterServicesFromAssembly(typeof(MyProject.Application.Features.Users.Handlers.GetUsersQueryHandler).Assembly);
    cfg.RegisterServicesFromAssemblies(typeof(MyProject.Application.Features.Users.Handlers.UpdateUserRolesHandler).Assembly);
});

// Add AutoMapper
builder.Services.AddAutoMapper(cfg =>
{
    cfg.AddProfile(typeof(GameMappingProfile));
    cfg.AddProfile(typeof(GameInfrastructureMappingProfile));
    cfg.AddProfile<MyProject.Infrastructure.Mapping.PublisherInfrastructureMappingProfile>();
    cfg.AddProfile<MyProject.Application.Mapping.PublisherProfile>();
    cfg.AddProfile<MyProject.Infrastructure.Mapping.UserInfrastructureMappingProfile>();
    cfg.AddProfile<MyProject.Application.Mapping.UserMappingProfile>();
    cfg.AddProfile<MyProject.Infrastructure.Mapping.RefreshTokenInfrastructureMappingProfile>();
});

builder.Services.AddInfrastructure(builder.Configuration);

// Register DbProviderFactories
DbProviderFactories.RegisterFactory("Npgsql", Npgsql.NpgsqlFactory.Instance);

// Configure LLBLGen Pro runtime configuration
RuntimeConfiguration.ConfigureDQE<PostgreSqlDQEConfiguration>(c =>
{
    c.AddDbProviderFactory(typeof(NpgsqlFactory));
    c.SetTraceLevel(TraceLevel.Verbose); // Optional for debugging
});

builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowSpecificOrigins", policy =>
            {
                policy
                    .WithOrigins("http://localhost:4200", "https://api/sepay.vn", "https://grocery-ecommerce.azurewebsites.net", "https://groceryecommerce.live")
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });

FirebaseApp.Create(new AppOptions
{
    Credential = GoogleCredential.FromFile("firebase-adminsdk.json")
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// QUAN TRỌNG: UseAuthentication() PHẢI đặt TRƯỚC UseAuthorization()
app.UseAuthentication();
app.UseCors("AllowSpecificOrigins");
app.UseAuthorization();

app.MapControllers();
app.Run();
