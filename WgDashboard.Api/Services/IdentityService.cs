using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using WgDashboard.Api.Data;
using WgDashboard.Api.Exceptions;
using WgDashboard.Api.Models;

namespace WgDashboard.Api.Services
{
    /// <summary>
    /// Interface for identity service
    /// </summary>
    public interface IIdentityService
    {
        /// <summary>
        /// Checks whether the user exists in the database
        /// </summary>
        /// <param name="id">The user's ID</param>
        /// <returns>A task resolving to true if the user exists; otherwise a task resolving to false</returns>
        public Task<bool> CheckUserExistsAsync(int id);


        /// <summary>
        /// Generates a JWT token for an authenticated user
        /// </summary>
        /// <param name="profile">The authenticated user's profile</param>
        /// <returns>A string representing the JWT token</returns>
        public string GenerateToken(UserProfile profile);


        /// <summary>
        /// Retrieves the user from the database via the user's ID in the JWT token
        /// </summary>
        /// <param name="httpContext">The HTTP context containing the JWT token</param>
        /// <returns>The user that matches the user's ID if the user is found; otherwise null</returns>
        public User? GetUserFromJwt(HttpContext httpContext);


        /// <summary>
        /// Retrieves the user from the database via the user's ID in the JWT token
        /// </summary>
        /// <param name="httpContext">The HTTP context containing the JWT token</param>
        /// <returns>A task that resolves to the user that matches the ID if the user is found; otherwise a task that resolves to null</returns>
        public Task<User?> GetUserFromJwtAsync(HttpContext httpContext);


        /// <summary>
        /// Retrieves the user's ID from the JWT token
        /// </summary>
        /// <param name="httpContext">The HTTP context containing the JWT token</param>
        /// <returns>The user's ID if the ID is found in the token; otherwise 0</returns>
        public int GetUserIdFromJwt(HttpContext httpContext);


        /// <summary>
        /// Retrieves the user's username from the JWT token
        /// </summary>
        /// <param name="httpContext">The HTTP context containing the JWT token</param>
        /// <returns>The user's username if the username is found in the token; otherwise null</returns>
        public string? GetUsernameFromJwt(HttpContext httpContext);


        /// <summary>
        /// Retrieves the user's role from the JWT token
        /// </summary>
        /// <param name="httpContext">The HTTP context containing the JWT token</param>
        /// <returns>The user's role if the role is found in the token; otherwise an anonymous role</returns>
        public string GetUserRoleFromJwt(HttpContext httpContext);

        /// <summary>
        /// Mutates the HttpContext such that the refresh token is added as a cookie
        /// </summary>
        /// <param name="user"></param>
        /// <param name="httpContext"></param>
        /// <returns></returns>
        public Task SetRefreshToken(User user, HttpContext httpContext);

        /// <summary>
        /// Generates a new JWT token. Also mutates the HttpContext to rotate the refresh token.
        /// </summary>
        /// <param name="httpContext"></param>
        /// <returns>A JWT token</returns>
        public Task<string> RefreshToken(HttpContext httpContext);
        
        public Task RevokeRefreshToken(HttpContext httpContext);
    }


    /// <summary>
    /// Implementation for identity service
    /// </summary>
    public class IdentityService : IIdentityService
    {
        private readonly IConfiguration _config;
        private readonly WireguardDbContext _context;
        private readonly IWebHostEnvironment _env;
        private const double refreshTokenExpiry = 2.0;

        public IdentityService(IConfiguration apiConfig, WireguardDbContext dbContext, IWebHostEnvironment environment)
        {
            this._config = apiConfig;
            this._context = dbContext;
            this._env = environment;
        }

        public async Task<bool> CheckUserExistsAsync(int id)
        {
            User? existingUser = await _context.Users.Where((user) => user.Id == id).FirstOrDefaultAsync();
            return existingUser != null;
        }

        public string GenerateToken(UserProfile userProfile)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["JwtSettings:Key"]!)); // key validated to be not null by Program.cs
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            var claims = new Claim[]
            {
                new Claim(ClaimTypes.Sid, userProfile.Id.ToString()),
                new Claim(ClaimTypes.NameIdentifier, userProfile.Username),
                new Claim(ClaimTypes.Name, userProfile.Name ?? ""),
                new Claim(ClaimTypes.Role, userProfile.Role),
            };

            var token = new JwtSecurityToken(
                    _config["JwtSettings:Issuer"],
                    _config["JwtSettings:Audience"],
                    claims,
                    expires: DateTime.Now.AddMinutes(15),
                    signingCredentials: credentials
                );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public User? GetUserFromJwt(HttpContext httpContext)
        {
            // get the user's ID from the token
            var identity = httpContext.User.Identity as ClaimsIdentity;
            if (identity == null)
                return null;

            var identityClaims = identity.Claims;
            int userId;
            if (!int.TryParse(identityClaims.FirstOrDefault(claim => claim.Type == ClaimTypes.Sid)?.Value, out userId))
                return null;

            return _context.Users.Where((user) => user.Id == userId).FirstOrDefault();
        }

        public Task<User?> GetUserFromJwtAsync(HttpContext httpContext)
        {
            // get the user's ID from the token
            var identity = httpContext.User.Identity as ClaimsIdentity;
            if (identity is null)
                return Task.FromResult<User?>(null);

            var identityClaims = identity.Claims;
            int userId;
            // guard against user not found from token
            if (!int.TryParse(identityClaims.FirstOrDefault(claim => claim.Type == ClaimTypes.Sid)?.Value, out userId))
                return Task.FromResult<User?>(null);

            return _context.Users.Where((user) => user.Id == userId).FirstOrDefaultAsync();
        }

        public int GetUserIdFromJwt(HttpContext httpContext)
        {
            var identity = httpContext.User.Identity as ClaimsIdentity;
            if (identity is null)
                return 0;

            IEnumerable<Claim> identityClaims = identity.Claims;

            int userId;
            if (!int.TryParse(identityClaims.FirstOrDefault((claim) => claim.Type == ClaimTypes.Sid)?.Value, out userId))
                return 0;
            else
                return userId;

        }

        public string? GetUsernameFromJwt(HttpContext httpContext)
        {
            var identity = httpContext.User.Identity as ClaimsIdentity;
            if (identity is null)
                return null;

            IEnumerable<Claim> identityClaims = identity.Claims;
            return identityClaims.FirstOrDefault((claim) => claim.Type == ClaimTypes.NameIdentifier)?.Value;
        }

        public string GetUserRoleFromJwt(HttpContext httpContext)
        {
            var identity = httpContext.User.Identity as ClaimsIdentity;
            if (identity is null)
                return UserRoles.Anonymous;

            IEnumerable<Claim> identityClaims = identity.Claims;
            return identityClaims.FirstOrDefault((claim) => claim.Type == ClaimTypes.Role)?.Value ?? UserRoles.Anonymous;
        }

        private string GenerateRandomRefreshToken()
        {
            var randomNumber = new byte[64];
            using var generator = RandomNumberGenerator.Create();
            generator.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);

        }

        public async Task SetRefreshToken(User user, HttpContext httpContext)
        {
            string refreshToken = GenerateRandomRefreshToken();

            DateTime expirationDate = DateTime.Now.AddDays(refreshTokenExpiry);

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiry = expirationDate;
            await _context.SaveChangesAsync();

            httpContext.Response.Cookies.Append("RefreshToken", refreshToken, new CookieOptions()
            {
                Expires = expirationDate,
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
            });
        }

        public async Task<string> RefreshToken(HttpContext httpContext)
        {
            User? user = await _context.Users.Where((u) => u.RefreshToken == httpContext.Request.Cookies["RefreshToken"]).FirstOrDefaultAsync();
            if (user is null || user.RefreshToken is null)
                throw new NotAuthorizedException("Bad refresh token");

            if (DateTime.Now > user.RefreshTokenExpiry)
                throw new NotAuthorizedException("Bad refresh token");

            string refreshToken = GenerateRandomRefreshToken();
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiry = DateTime.Now.AddDays(refreshTokenExpiry);
            await _context.SaveChangesAsync();

            httpContext.Response.Cookies.Append("RefreshToken", refreshToken, new CookieOptions()
            {
                Expires = user.RefreshTokenExpiry,
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
            });

            // do this last since jwt is short-lived
            string jwt = GenerateToken(new UserProfile()
            {
                Id = user.Id,
                Username = user.Username,
                Name = user.Name,
                Role = user.Role,
            });
            return jwt;
        }

        public async Task RevokeRefreshToken(HttpContext httpContext)
        {
            User? user = await _context.Users.Where((u) => u.RefreshToken == httpContext.Request.Cookies["RefreshToken"]).FirstOrDefaultAsync();
            if (user is null || user.RefreshToken is null)
                throw new NotAuthorizedException("Bad refresh token");

            if (DateTime.Now > user.RefreshTokenExpiry)
                throw new NotAuthorizedException("Bad refresh token");

            user.RefreshTokenExpiry = new DateTime();
            await _context.SaveChangesAsync();
        }
    }
}
