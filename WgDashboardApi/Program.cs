using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.IdentityModel.Protocols.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using WgDashboardApi.Data;
using WgDashboardApi.Services;

// reader for appsettings.json
var config = new ConfigurationBuilder()
        .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
        .AddJsonFile("appsettings.json")
        .Build();

var builder = WebApplication.CreateBuilder(args);

// set address, port, and protocol to listen on
string listenAddress = config["ListenSettings:Address"] ?? "127.0.0.1";
string listenProtocol = config["ListenSettings:Protocol"] ?? "http";

int listenPort = 0;
if (!int.TryParse(config["ListenSettings:Port"], out listenPort))
    listenPort = 3000;

Console.WriteLine($"API URL: {listenProtocol}://{listenAddress}:{listenPort}");
builder.WebHost.UseUrls($"{listenProtocol}://{listenAddress}:{listenPort}");

// Add services to the container.
// CORS
string browserUrl = config["DnsSettings:BrowserUrlToApi"] ?? "http://127.0.0.1:3000";
builder.Services.AddCors(opts =>
{
    opts.AddPolicy(name: "DefaultCorsPolicy", policy =>
    {
        policy.WithOrigins(browserUrl)
        .AllowAnyMethod()
        .AllowAnyHeader()
        .SetIsOriginAllowed(origin => true);
    });
});

// JWT-based authentication and authorization
string? jwtSigningKey = config["JwtSettings:Key"];
if (jwtSigningKey is null)
    throw new InvalidConfigurationException("JWT signing key is a required setting");
builder.Services.AddAuthentication(opts =>
{
    opts.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    opts.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    opts.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(opts =>
{
    opts.TokenValidationParameters = new TokenValidationParameters()
    {
        ValidIssuer = config["JwtSettings:Issuer"],
        ValidAudience = config["JwtSettings:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSigningKey)),
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
    };
});
builder.Services.AddAuthentication();

// DB for entity framework
if(builder.Environment.IsDevelopment())
{
    builder.Services.AddDbContext<WireguardDbContext>(opts => opts.UseInMemoryDatabase("TestDb"));
}
else
{
    string? connectionString = builder.Configuration.GetConnectionString("WireguardDb");
    if (connectionString is null)
        throw new InvalidConfigurationException("Connection string is a required setting");
    builder.Services.AddDbContext<WireguardDbContext>(opts =>
    {
        opts.UseSqlServer(connectionString);
    });
}

// dependency injections
builder.Services.AddScoped<ISecurityService, SecurityService>();
builder.Services.AddScoped<IIdentityService, IdentityService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IPeerService, PeerService>();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("DefaultCorsPolicy");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
