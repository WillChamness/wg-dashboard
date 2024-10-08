using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Protocols.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using WgDashboard.Api.Data;
using WgDashboard.Api.Helpers;
using WgDashboard.Api.Services;


var builder = WebApplication.CreateBuilder(args);

// reader for appsettings.json
var config = new ConfigurationBuilder()
        .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
        .AddJsonFile(builder.Environment.IsDevelopment() ? "appsettings.Development.json" : "appsettings.json")
        .Build();

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
        .AllowCredentials() // TODO: figure out why this doesnt allow the browser to store the cookie
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
        ValidateIssuer = builder.Environment.IsProduction(),
        ValidateAudience = builder.Environment.IsProduction(),
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
builder.Services.AddSingleton<ISecurityInitialSettings, SecurityInitialSettings>();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
var scope = app.Services.CreateScope();
var services = scope.ServiceProvider;

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    var context = services.GetRequiredService<WireguardDbContext>();
    context.Database.Migrate();
}

app.UseCors("DefaultCorsPolicy");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
