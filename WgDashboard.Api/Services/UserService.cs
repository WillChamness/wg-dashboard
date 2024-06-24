using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using WgDashboard.Api.Data;
using WgDashboard.Api.Exceptions;
using WgDashboard.Api.Models;

namespace WgDashboard.Api.Services
{
    public interface IUserService
    {
        public IEnumerable<UserProfile> GetAllUsers();
        public Task<UserProfile?> GetUserProfileById(int id);
        public Task<UserProfile?> GetUserProfileByUsername(string username);
        public Task UpdateUserProfile(int id, string? newUsername, string? newName, string? newRole);
        public Task DeleteUserById(int id);
    }

    public class UserService : IUserService
    {
        private readonly WireguardDbContext _context;

        public UserService(WireguardDbContext dbContext)
        {
            this._context = dbContext;
        }

        public IEnumerable<UserProfile> GetAllUsers()
        {
            IQueryable<UserProfile> profiles = _context.Users.Select((user) => new UserProfile()
            {
                Id = user.Id,
                Username = user.Username,
                Name = user.Name,
                Role = user.Role,
            });

            return profiles;
        }

        public async Task<UserProfile?> GetUserProfileById(int id)
        {
            User? user = await _context.Users.FindAsync(id);
            if (user is null)
                return null;

            return new UserProfile()
            {
                Id = user.Id,
                Username = user.Username,
                Name = user.Name,
                Role = user.Role,
            };
        }
        public async Task<UserProfile?> GetUserProfileByUsername(string username)
        {
            User? user = await _context.Users.Where((user) => user.Username == username).FirstOrDefaultAsync();
            if (user is null)
                return null;

            return new UserProfile()
            {
                Id = user.Id,
                Username = user.Username,
                Name = user.Name,
                Role = user.Role,
            };

        }

        public async Task UpdateUserProfile(int id, string? newUsername, string? newName, string? newRole)
        {
            // guards against bad input
            if (newUsername is null || newUsername == "")
                throw new BadRequestException("Username expected but not found");
            if (!UserRoles.IsValidRole(newRole))
                throw new BadRequestException("Role not found or is not valid");
            if (_context.Users.Where((user) => user.Username == newUsername).Any())
                throw new BadRequestException("Username already taken");

            // guard against user not existing
            User? existingUser = await _context.Users.FindAsync(id);
            if (existingUser is null)
                throw new ResourceNotFoundException($"User with ID {id} not found");

            try
            {
                existingUser.Username = newUsername;
                existingUser.Name = newName;
                existingUser.Role = newRole!; // validated not null by UserRoles.IsValidRole()
                await _context.SaveChangesAsync();
            }
            catch(DbUpdateException)
                { throw new InternalServerErrorException("Could not update the database!"); }
            catch(OperationCanceledException)
                { throw new InternalServerErrorException("Could not update the database: operation cancelled"); }
        }

        public async Task DeleteUserById(int id)
        {
            User? existingUser = await _context.Users.FindAsync(id);

            if (existingUser is null)
                throw new ResourceNotFoundException($"Could not find user with ID {id}");

            try
            {
                // detach all peers attached to user
                var peers = _context.Peers.Where((peer) => peer.OwnerId == id);
                foreach(Peer peer in peers)
                {
                    _context.Peers.Remove(peer);
                }
                // delete the user
                _context.Users.Remove(existingUser);

                // attempt to save the DB
                await _context.SaveChangesAsync();
            }
            catch(DbUpdateException)
                { throw new InternalServerErrorException("Could not update the database!"); }
            catch(OperationCanceledException)
                { throw new InternalServerErrorException("Coul dnot update the database: operation cancelled"); }
        }
    }
}
