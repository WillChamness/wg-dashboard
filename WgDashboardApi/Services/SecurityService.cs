using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using WgDashboardApi.Controllers;
using WgDashboardApi.Data;
using WgDashboardApi.Exceptions;
using WgDashboardApi.Models;

namespace WgDashboardApi.Services
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

        public SecurityService(WireguardDbContext dbContext, IConfiguration config) 
        {
            this._context = dbContext;
        }

        public Task<User?> Authenticate(string? username, string? password)
        {
            if (username is null || password is null)
                throw new BadRequestException("Username and password expected but not found");

            return _context.Users.Where((user) => user.Username == username && user.Password == password)
                .FirstOrDefaultAsync();
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
                Password = password,
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
                existingUser.Password = password;
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
    }

}
