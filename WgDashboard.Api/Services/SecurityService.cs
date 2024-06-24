using BCrypt.Net;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using WgDashboard.Api.Controllers;
using WgDashboard.Api.Data;
using WgDashboard.Api.Exceptions;
using WgDashboard.Api.Models;
using Microsoft.IdentityModel.Tokens;
using System.Diagnostics.Eventing.Reader;
using WgDashboard.Api.Helpers;

namespace WgDashboard.Api.Services
{
    /// <summary>
    /// Interface for the security service
    /// </summary>
    public interface ISecurityService
    {
        /// <summary>
        /// Authenticates the user, given a username and password
        /// </summary>
        /// <param name="username">The user's username</param>
        /// <param name="password">The user's password</param>
        /// <returns>The user that is associated with the credentials if given a valid username and password; otherwise null</returns>
        public Task<User?> Authenticate(string? username, string? password);


        /// <summary>
        /// Creates a new user, given a username and password
        /// </summary>
        /// <param name="username">The new user's username</param>
        /// <param name="password">The new user's password</param>
        /// <param name="name">The new user's name</param>
        /// <exception cref="BadRequestException"></exception>
        /// <exception cref="InternalServerErrorException"></exception>
        public Task<User> AddUser(string? username, string? password, string? name);


        /// <summary>
        /// Checks whether it's ok to proceed with the user's request to access a user's profile
        /// </summary>
        /// <param name="userIdToCheckAgainst">The id of the user to be accessed</param>
        /// <param name="usersActualId">The user's actual role</param>
        /// <param name="usersActualRole">The user's actual role</param>
        /// <returns>True if the user is authorized; otherwise false</returns>
        public bool CheckUserAuthorized(int idToCheckAgainst, int usersActualId, string usersActualRole);


        /// <summary>
        /// <para>Updates the user's password given a user's ID</para>
        /// 
        /// <para>Assumes that the user does actually exist</para>
        /// </summary>
        /// <param name="userId">The user's ID</param>
        /// <param name="newPassword">The user's new password</param>
        /// <exception cref="InternalServerErrorException"></exception>
        /// <exception cref="BadRequestException"></exception>
        /// <exception cref="ResourceNotFoundException"></exception>
        public Task UpdateUserPassword(int userId, string newPassword);
    }

    /// <summary>
    /// Implementation for the security service
    /// </summary>
    public class SecurityService : ISecurityService
    {
        private readonly WireguardDbContext _context;
        private readonly IConfiguration _config;
        private static readonly int BCRYPT_WORK_FACTOR = 12;

        public SecurityService(WireguardDbContext dbContext, IConfiguration config, IWebHostEnvironment environment)
        {
            this._context = dbContext;
            this._config = config;

            // add a default admin on API startup to prevent lockouts by deleting all admins or forgetting all admin passwords
            // a new security service is created every time this service is called. keep track of the first time API was initialized 
            // to prevent arbitrarily creating a user with a possibly weak password
            if (!SecurityInitialSettings.Initialized)
            {
                SecurityInitialSettings.SetSettings(config, environment);

                if (!_context.Users.Where((user) => user.Role == UserRoles.Admin).Any() && !SecurityInitialSettings.CreateAdmin)
                    Console.WriteLine("WARNING: No admins found, but settings specify not to initialize any admin.");
                else if (SecurityInitialSettings.CreateAdmin)
                {
                    var newUser = new User()
                    {
                        Username = SecurityInitialSettings.InitialUsername,
                        Password = GenerateHashedPassword(SecurityInitialSettings.InitialPassword),
                        Name = SecurityInitialSettings.InitialName,
                        Role = UserRoles.Admin,
                    };
                    if (_context.Users.Where((user) => user.Username == SecurityInitialSettings.InitialUsername).Any())
                        Console.WriteLine($"WARNING: Cannot create admin because username '{SecurityInitialSettings.InitialUsername}' already exists");
                    else
                    {
                        _context.Users.Add(newUser);
                        _context.SaveChanges();
                    }
                }
            }
        }

        public async Task<User?> Authenticate(string? username, string? password)
        {
            if (username is null || password is null)
                throw new BadRequestException("Username and password expected but not found");

            User? user = await _context.Users.Where((user) => user.Username == username)
                .FirstOrDefaultAsync();

            if (user is null)
                return null;

            bool passwordOk = await VerifyPasswordAsync(password, user.Password);
            if (!passwordOk)
                return null;
            else
                return user;
        }

        public async Task<User> AddUser(string? username, string? password, string? name)
        {
            // guard against bad input
            if (username is null || password is null || username == "" || password == "")
                throw new BadRequestException("Username and password expected but not found");

            // guard against username exists already
            var existingUsers = _context.Users.Where((user) => user.Username == username);
            if (existingUsers.Any())
                throw new BadRequestException($"'{username}' already taken");

            var newUser = new User()
            {
                Username = username,
                Password = await GenerateHashedPasswordAsync(password),
                Name = name,
                Role = UserRoles.User,
            };

            User createdEntry;
            try
            {
                var entityEntry = await _context.Users.AddAsync(newUser);
                if (entityEntry is null)
                    throw new InternalServerErrorException("Unable to add user!");
                await _context.SaveChangesAsync();
                createdEntry = entityEntry.Entity;
            }
            catch(DbUpdateException)
            {
                { throw new InternalServerErrorException("Could not update the database due to an error!"); }
            }
            catch(OperationCanceledException)
            {
                { throw new InternalServerErrorException("Could not update the database: operation was cancelled"); }
            }

            return createdEntry;
        }

        public bool CheckUserAuthorized(int idToCheckAgainst, int usersActualId, string usersActualRole) => 
            usersActualRole == UserRoles.Admin || usersActualId == idToCheckAgainst; // if admin, don't care what the user's ID is

        public async Task UpdateUserPassword(int userId, string password)
        {
            // guard against bad input
            if (password is null || password == "")
                throw new BadRequestException("Password expected but not found");
            User? existingUser = (await _context.Users.Where((user) => user.Id == userId).FirstOrDefaultAsync()); 
            if (existingUser is null)
                throw new ResourceNotFoundException($"User with ID {userId} not found");

            // try to update password 
            try
            {
                existingUser.Password = await GenerateHashedPasswordAsync(password);
                await _context.SaveChangesAsync();
            }
            catch(DbUpdateException)
            {
                { throw new InternalServerErrorException("Could not update the database due to an error!"); }
            }
            catch(OperationCanceledException)
            {
                { throw new InternalServerErrorException("Could not update the database: operation was cancelled"); }
            }
        }

        private string GenerateHashedPassword(string password)
        {
            string hashedPassword = BCrypt.Net.BCrypt.EnhancedHashPassword(password, BCRYPT_WORK_FACTOR);
            return hashedPassword;
        }

        private bool VerifyPassword(string unhashedPassword, string usersHashedPassword)
        {
            bool verified = BCrypt.Net.BCrypt.EnhancedVerify(unhashedPassword, usersHashedPassword);
            return verified;
        }

        private Task<string> GenerateHashedPasswordAsync(string password)
        {
            Task<string> passwordHashTask = Task.Run(() => BCrypt.Net.BCrypt.EnhancedHashPassword(password, BCRYPT_WORK_FACTOR));
            return passwordHashTask;
        }

        private Task<bool> VerifyPasswordAsync(string unhashedPassword, string usersHashedPassword)
        {
            Task<bool> verifyTask = Task.Run(() => BCrypt.Net.BCrypt.EnhancedVerify(unhashedPassword, usersHashedPassword));
            return verifyTask;
        }
    }
}
