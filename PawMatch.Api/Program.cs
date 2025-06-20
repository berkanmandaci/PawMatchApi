using Microsoft.EntityFrameworkCore;
using PawMatch.Infrastructure;
using PawMatch.Infrastructure.Interfaces;
using PawMatch.Infrastructure.Repositories;
using PawMatch.Infrastructure.Providers;
using PawMatch.Application.Interfaces;
using PawMatch.Application.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

namespace PawMatch.Api;

public class Program
{
    public static void Main(string[] args)
    {
var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://0.0.0.0:80", "http://0.0.0.0:8080");

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "PawMatch API", Version = "v1" });

    // JWT Bearer Auth için:
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

// DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// DI
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddSingleton<IJwtProvider, JwtProvider>();
builder.Services.AddScoped<IMatchService, MatchService>();
builder.Services.AddScoped<IDiscoverService, DiscoverService>();
builder.Services.AddScoped<IUserSwipeRepository, UserSwipeRepository>();
builder.Services.AddSingleton<IStorageProvider, GoogleDriveStorageProvider>();
builder.Services.AddScoped<IPhotoService, PhotoService>();
builder.Services.AddScoped<IMessageRepository, MessageRepository>();
builder.Services.AddScoped<IMessageService, MessageService>();
builder.Services.AddScoped<IRealtimeNotificationService, PawMatch.Api.Services.SignalRNotificationService>();
builder.Services.AddScoped<IMatchRepository, MatchRepository>();

builder.Services.AddSignalR();

// CORS Policy ekle
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowWebApp",
        policy => policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod()
    );
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.ASCII.GetBytes("super-secret-key-which-is-at-least-32-chars!"))
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                // Eğer istek SignalR hub'a ise, token'ı query string'den al
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/chatHub"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// CORS middleware'i authentication'dan önce ekle
app.UseCors("AllowWebApp");

//app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapHub<PawMatch.Api.Hubs.ChatHub>("/chatHub");

// Sadece development ortamında otomatik migration uygula
if (app.Environment.IsDevelopment() && !app.Environment.IsEnvironment("Testing"))
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.Migrate();
        PawMatch.Infrastructure.AppDbContext.AppDbContextSeed.Seed(db);
    }
}

app.Run();
    }
}
