using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using WgDashboardApi.Data;
using WgDashboardApi.Models;

namespace WgDashboardApi.Services
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
        /// <param name="context">The HTTP context containing the JWT token</param>
        /// <returns>The user that matches the user's ID if the user is found; otherwise null</returns>
        public User? GetUserFromJwt(HttpContext context);


        /// <summary>
        /// Retrieves the user from the database via the user's ID in the JWT token
        /// </summary>
        /// <param name="context">The HTTP context containing the JWT token</param>
        /// <returns>A task that resolves to the user that matches the ID if the user is found; otherwise a task that resolves to null</returns>
        public Task<User?> GetUserFromJwtAsync(HttpContext context);


        /// <summary>
        /// Retrieves the user's ID from the JWT token
        /// </summary>
        /// <param name="context">The HTTP context containing the JWT token</param>
        /// <returns>The user's ID if the ID is found in the token; otherwise 0</returns>
        public int GetUserIdFromJwt(HttpContext context);


        /// <summary>
        /// Retrieves the user's username from the JWT token
        /// </summary>
        /// <param name="context">The HTTP context containing the JWT token</param>
        /// <returns>The user's username if the username is found in the token; otherwise null</returns>
        public string? GetUsernameFromJwt(HttpContext context);


        /// <summary>
        /// Retrieves the user's role from the JWT token
        /// </summary>
        /// <param name="context">The HTTP context containing the JWT token</param>
        /// <returns>The user's role if the role is found in the token; otherwise an anonymous role</returns>
        public string GetUserRoleFromJwt(HttpContext context);
    }


    /// <summary>
    /// Implementation for identity service
    /// </summary>
    public class IdentityService : IIdentityService
    {
        private readonly IConfiguration _config;
        private readonly WireguardDbContext _context;

        public IdentityService(IConfiguration apiConfig, WireguardDbContext dbContext)
        {
            this._config = apiConfig;
            this._context = dbContext;
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
                    expires: DateTime.Now.AddMinutes(30),
                    signingCredentials: credentials
                );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public User? GetUserFromJwt(HttpContext context)
        {
            // get the user's ID from the token
            var identity = context.User.Identity as ClaimsIdentity;
            if (identity == null)
                return null;

            var identityClaims = identity.Claims;
            int userId;
            if (!int.TryParse(identityClaims.FirstOrDefault(claim => claim.Type == ClaimTypes.Sid)?.Value, out userId))
                return null;

            return _context.Users.Where((user) => user.Id == userId).FirstOrDefault();
        }

        public Task<User?> GetUserFromJwtAsync(HttpContext context)
        {
            // get the user's ID from the token
            var identity = context.User.Identity as ClaimsIdentity;
            if (identity is null)
                return Task.FromResult<User?>(null);

            var identityClaims = identity.Claims;
            int userId;
            // guard against user not found from token
            if (!int.TryParse(identityClaims.FirstOrDefault(claim => claim.Type == ClaimTypes.Sid)?.Value, out userId))
                return Task.FromResult<User?>(null);

            return _context.Users.Where((user) => user.Id == userId).FirstOrDefaultAsync();
        }

        public int GetUserIdFromJwt(HttpContext context)
        {
            var identity = context.User.Identity as ClaimsIdentity;
            if (identity is null)
                return 0;

            IEnumerable<Claim> identityClaims = identity.Claims;

            int userId;
            if (!int.TryParse(identityClaims.FirstOrDefault((claim) => claim.Type == ClaimTypes.Sid)?.Value, out userId))
                return 0;
            else
                return userId;

        }

        public string? GetUsernameFromJwt(HttpContext context)
        {
            var identity = context.User.Identity as ClaimsIdentity;
            if (identity is null)
                return null;

            IEnumerable<Claim> identityClaims = identity.Claims;
            return identityClaims.FirstOrDefault((claim) => claim.Type == ClaimTypes.NameIdentifier)?.Value;
        }

        public string GetUserRoleFromJwt(HttpContext context)
        {
            var identity = context.User.Identity as ClaimsIdentity;
            if (identity is null)
                return UserRoles.Anonymous;

            IEnumerable<Claim> identityClaims = identity.Claims;
            return identityClaims.FirstOrDefault((claim) => claim.Type == ClaimTypes.Role)?.Value ?? UserRoles.Anonymous;
        }
    }
}
