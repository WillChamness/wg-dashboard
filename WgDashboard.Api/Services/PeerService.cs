using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using WgDashboard.Api.Data;
using WgDashboard.Api.Exceptions;
using WgDashboard.Api.Models;

namespace WgDashboard.Api.Services
{
    public interface IPeerService
    {
        
        /// <summary>
        /// Gets all peers
        /// </summary>
        /// <returns>An enumerable of peers</returns>
        public IEnumerable<PeerProfile> GetAllPeers();


        /// <summary>
        /// Gets the peer's profile by ID
        /// </summary>
        /// <param name="id">The ID of the peer</param>
        /// <returns>A peer profile</returns>
        public Task<PeerProfile?> GetPeerProfileById(int id);


        /// <summary>
        /// Gets the peer by ID
        /// </summary>
        /// <param name="id">The ID of the peer that is attached to the user</param>
        /// <returns>The user that owns the peer</returns>
        public Task<Peer?> GetPeerById (int id);


        /// <summary>
        /// Gets all peers attached to a user
        /// </summary>
        /// <param name="ownerId">The ID of the user that owns the peers</param>
        /// <returns>An enumerable of peer profiles that share the same owner</returns>
        public Task<IEnumerable<PeerProfile>> GetPeerProfilesByOwnerId(int ownerId);


        /// <summary>
        /// Adds a new peer
        /// </summary>
        /// <param name="peer">The peer to be added</param>
        /// <returns>A profile of the newly created peer</returns>
        /// <exception cref="InternalServerErrorException"></exception>
        /// <exception cref="BadRequestException"></exception>
        /// <exception cref="ResourceNotFoundException"></exception>
        public Task<PeerProfile> AddPeer(NewPeerRequest peer);


        /// <summary>
        /// Updates the peer in-place
        /// </summary>
        /// <param name="updatedPeer">The updated peer</param>
        /// <exception cref="InternalServerErrorException"></exception>
        /// <exception cref="BadRequestException"></exception>
        /// <exception cref="ResourceNotFoundException"></exception>
        public Task UpdatePeer(UpdatePeerRequest updatedPeer);


        /// <summary>
        /// Finds the peer and deletes it in-place
        /// </summary>
        /// <param name="peerToDelete">The existing peer to be deleted</param>
        /// <exception cref="InternalServerErrorException"></exception>
        /// <exception cref="ResourceNotFoundException"></exception>
        public Task DeletePeer(Peer peerToDelete);
    }

    public class PeerService : IPeerService
    {
        private readonly WireguardDbContext _context;
        private static int MAX_PEERS_PER_USER = 5;

        /*
         * Service constructor
         */
        public PeerService(WireguardDbContext dbContext)
        {
            this._context = dbContext;
        }

        public IEnumerable<PeerProfile> GetAllPeers()
        {
            IQueryable<PeerProfile> peers = _context.Users.Join(_context.Peers, (user) => user.Id, (peer) => peer.OwnerId,
                (user, peer) => new PeerProfile()
                {
                    Id = peer.Id,
                    PublicKey = peer.PublicKey,
                    AllowedIPs = peer.AllowedIPs,
                    DeviceDescription = peer.DeviceDescription,
                    OwnerName = user.Name,
                    OwnerUsername = user.Username,
                    DeviceType = peer.DeviceType,
                }
            );

            return peers;
        }


        public async Task<PeerProfile?> GetPeerProfileById(int id)
        {
            Peer? peer = await _context.Peers.FindAsync(id);
            if (peer is null)
                return null;

            User? user = await _context.Users.FindAsync(peer.OwnerId);
            if (user is null)
                throw new BadDataIntegrityException($"Peer with ID {id} does not have an owner");

            return new PeerProfile()
            {
                Id = peer.Id,
                PublicKey = peer.PublicKey,
                AllowedIPs = peer.AllowedIPs,
                DeviceDescription = peer.DeviceDescription,
                OwnerName = user.Name,
                OwnerUsername = user.Username,
                DeviceType = peer.DeviceType,
            };
        }


        public Task<Peer?> GetPeerById(int id) => _context.Peers.Where((peer) => peer.Id == id).FirstOrDefaultAsync();


        public async Task<IEnumerable<PeerProfile>> GetPeerProfilesByOwnerId(int ownerId)
        {
            User? owner = await _context.Users.FindAsync(ownerId);
            if(owner is null)
                return new List<PeerProfile>();

            IQueryable<PeerProfile> peerProfiles = _context.Users.Join(
                _context.Peers, (user) => user.Id, (peer) => peer.OwnerId,
                    (user, peer) => new PeerProfile()
                    {
                        Id = peer.Id,
                        PublicKey = peer.PublicKey,
                        AllowedIPs = peer.AllowedIPs,
                        DeviceDescription = peer.DeviceDescription,
                        OwnerName = user.Name,
                        OwnerUsername = user.Username,
                        DeviceType = peer.DeviceType,
                    }
                )
                .Where((profile) => profile.OwnerUsername == owner.Username); // profile doesn't contain owner ID

            return peerProfiles;
        }

        public async Task<PeerProfile> AddPeer(NewPeerRequest peer)
        {
            // guards against bad inputs
            if (peer.PublicKey is null)
                throw new BadRequestException("Public key expected but not found");
            if (peer.AllowedIPs is null)
                throw new BadRequestException("Allowed IPs expected but not found");
            if (peer.OwnerId <= 0)
                throw new BadRequestException("Owner ID not valid");
            if (!DeviceTypes.IsValidDeviceType(peer.DeviceType))
                throw new BadRequestException("Device type is not valid");

            // more guards against bad input
            var existingPk = _context.Peers.Where((p) => p.PublicKey == peer.PublicKey);
            var existingOwner = _context.Users.Where((user) => user.Id == peer.OwnerId);
            int numberOfPeers = _context.Peers.Where((p) => p.OwnerId == peer.OwnerId).Count();
            if (existingPk.Any())
                throw new BadRequestException("Public key already exists");
            if (!existingOwner.Any())
                throw new BadRequestException($"Owner with ID {peer.OwnerId} does not exist");
            if (numberOfPeers >= MAX_PEERS_PER_USER)
                throw new BadRequestException($"Too many peers attached to user with ID {peer.OwnerId}");

            // attempt to add new peer
            Peer? newPeer;
            try
            {
                var createdPeer = await _context.Peers.AddAsync(new Peer()
                {
                    PublicKey = peer.PublicKey,
                    AllowedIPs = peer.AllowedIPs,
                    OwnerId = peer.OwnerId,
                    DeviceDescription = peer.DeviceDescription,
                    DeviceType = peer.DeviceType,
                });
                await _context.SaveChangesAsync();
                newPeer = createdPeer.Entity;
            }
            catch(DbUpdateException)
                { throw new InternalServerErrorException("Could not update database due to an error!"); }
            catch(OperationCanceledException)
                { throw new InternalServerErrorException("Update the the database cancelled!"); }

            if (newPeer is null)
                throw new InternalServerErrorException("Could not update the database!");

            User owner = (await existingOwner.FirstOrDefaultAsync())!; // verified not null by guard clauses 
            return new PeerProfile()
            {
                Id = newPeer.Id,
                PublicKey = newPeer.PublicKey,
                AllowedIPs = newPeer.AllowedIPs,
                DeviceDescription = newPeer.DeviceDescription,
                OwnerName = owner.Name,
                OwnerUsername = owner.Username,
                DeviceType = newPeer.DeviceType,
            };
        }

        public async Task UpdatePeer(UpdatePeerRequest updatedPeer)
        {
            // guards against bad input
            if (updatedPeer.PublicKey is null)
                throw new BadRequestException("Public key required but not found");
            if (updatedPeer.AllowedIPs is null)
                throw new BadRequestException("Allowed IPs required but not found");
            if (!DeviceTypes.IsValidDeviceType(updatedPeer.DeviceType))
                throw new BadRequestException("Device type is not valid");

            // find peer in DB
            Peer? existingPeer = await _context.Peers.Where((p) => p.Id == updatedPeer.Id).FirstOrDefaultAsync();
            if (existingPeer is null)
                throw new ResourceNotFoundException($"Peer with ID {updatedPeer.Id} not found");

            try
            {
                existingPeer.PublicKey = updatedPeer.PublicKey;
                existingPeer.DeviceDescription = updatedPeer.DeviceDescription;
                existingPeer.DeviceType = updatedPeer.DeviceType;
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
                { throw new InternalServerErrorException("Could not update the database due to an error!"); }
            catch (OperationCanceledException)
                { throw new InternalServerErrorException("Could not update the database: operation was cancelled"); }
        }


        public async Task DeletePeer(Peer peerToDelete)
        {
            Peer? existingPeer = await _context.Peers.FindAsync(peerToDelete.Id);
            if (existingPeer is null)
                throw new ResourceNotFoundException($"Peer with ID {peerToDelete.Id} not found");

            try
            {
                _context.Peers.Remove(existingPeer);
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
