using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Client;
using System.Security.Cryptography.X509Certificates;
using WgDashboardApi.Exceptions;
using WgDashboardApi.Models;
using WgDashboardApi.Services;

namespace WgDashboardApi.Controllers
{
    [Route("api/peers")]
    [ApiController]
    public class PeersController : ControllerBase
    {
        private readonly IIdentityService _identity;
        private readonly ISecurityService _security;
        private readonly IUserService _users;
        private readonly IPeerService _peers;

        /*
         * Constructor for controller
         */
        public PeersController(IIdentityService identityService, ISecurityService securityService, IUserService userService, IPeerService peerService)
        {
            this._identity = identityService;
            this._security = securityService;
            this._users = userService;
            this._peers = peerService;
        }

        /// <summary>
        /// <para>GET: /api/peers</para>
        ///
        /// <para>Retrieves all peers</para>
        /// </summary>
        /// <returns>An HTTP 200 response</returns>
        [HttpGet]
        [Authorize(Roles = "admin")]
        [Produces("application/json")]
        [Consumes("application/json")]
        public ActionResult<IEnumerable<PeerProfile>> GetAllPeers()
        {
            return Ok(_peers.GetAllPeers()); 
        }

        /// <summary>
        /// <para>GET: /api/peers/5</para>
        /// 
        /// <para>Retrieves the peer's profiles given the peer's ID</para>
        /// </summary>
        /// <param name="id">The peer's ID, taken from the URL</param>
        /// <returns>An HTTP 200, 404, or 500 response</returns>
        [HttpGet("{id}")]
        [Authorize(Roles = "admin,user")]
        [Produces("application/json")]
        [Consumes("application/json")]
        public async Task<ActionResult<Peer>> GetPeerById(int id)
        {
            int requestingUserId = _identity.GetUserIdFromJwt(HttpContext);
            string requestingUserRole = _identity.GetUserRoleFromJwt(HttpContext);
            if (!_security.CheckUserAuthorized(id, requestingUserId, requestingUserRole))
                return NotFound($"Peer with ID {id} not found"); // return NotFound to hide that there might be a user

            PeerProfile? peerProfile;
            // try to get the peer's profile
            try
            {
                peerProfile = await _peers.GetPeerProfileById(id);
            }
            catch (BadDataIntegrityException bdie)
                { return StatusCode(StatusCodes.Status500InternalServerError, bdie.Message); }

            // guard against peer not found
            if (peerProfile is null)
                return NotFound($"Peer with ID {id} not found");

            // success
            return Ok(peerProfile);
        }

        /// <summary>
        /// <para>GET: /api/peers/owner/5</para>
        /// 
        /// <para>Searches for all peers whose owner's id is the given id</para>
        /// </summary>
        /// <param name="ownerId">The user's ID, taken from the URL</param>
        /// <returns>An HTTP 200 or 404 response</returns>
        [HttpGet("owner/{ownerId}")]
        [Authorize(Roles = "admin,user")]
        [Produces("application/json")]
        [Consumes("application/json")]
        public async Task<ActionResult<IEnumerable<PeerProfile>>> GetPeersByOwnerId(int ownerId)
        {
            // guard against unauthorized user
            int userId = _identity.GetUserIdFromJwt(HttpContext);
            string userRole = _identity.GetUserRoleFromJwt(HttpContext);
            if (!_security.CheckUserAuthorized(ownerId, userId, userRole))
                return NotFound($"No peers attached to user with ID {ownerId}");

            // attempt to get peers
            IEnumerable<PeerProfile> peers = await _peers.GetPeerProfilesByOwnerId(ownerId);

            // guard against no peers
            if(peers.Count() == 0)
                return NotFound($"No peers attached to user with ID {ownerId}");

            // success
            return Ok(peers);
        }

        /// <summary>
        /// <para>GET: /api/peers/owner/myuser</para>
        /// 
        /// <para>Searches for all peers whose owner's username is the one given</para>
        /// </summary>
        /// <param name="ownerUsername">The user's username, taken from the URL</param>
        /// <returns>An HTTP 200 or 404 response</returns>
        [HttpGet("/owner/username/{ownerUsername}")]
        [Authorize(Roles = "admin,user")]
        [Produces("application/json")]
        [Consumes("application/json")]
        public async Task<ActionResult<IEnumerable<PeerProfile>>> GetPeersByOwnerUsername(string ownerUsername)
        {
            // need to get the ID of the owner via the username to check if authorized
            UserProfile? owner = await _users.GetUserProfileByUsername(ownerUsername);
            if (owner is null)
                return NotFound($"No peers attached to user with username {ownerUsername}");

            // guard against unauthorized user
            int userId = _identity.GetUserIdFromJwt(HttpContext);
            string userRole = _identity.GetUserRoleFromJwt(HttpContext);
            if(!_security.CheckUserAuthorized(owner.Id, userId, userRole))
                return NotFound($"No peers attached to user with username {ownerUsername}");

            // attempt to get peers
            IEnumerable<PeerProfile> peers = await _peers.GetPeerProfilesByOwnerId(owner.Id);
            // guard against no peers
            if(peers.Count() == 0)
                return NotFound($"No peers attached to user with username {ownerUsername}");

            // success
            return Ok(peers);
        }

        /// <summary>
        /// <para>POST: /api/peers</para>
        ///
        /// <para>Attaches a new peer to an existing user</para>
        /// </summary>
        /// <param name="newPeer">The new peer to add</param>
        /// <returns>An HTTP 201, 400, 403, 404, or 500 response</returns>
        [HttpPost]
        [Authorize(Roles = "admin,user")]
        [Produces("application/json")]
        [Consumes("application/json")]
        public async Task<ActionResult<PeerProfile>> AddPeer([FromBody] Peer newPeer)
        {
            // guard against unauthorized user
            int userId = _identity.GetUserIdFromJwt(HttpContext);
            string userRole = _identity.GetUserRoleFromJwt(HttpContext);
            if (!_security.CheckUserAuthorized(newPeer.OwnerId, userId, userRole))
                return BadRequest("User's ID does not match peer's owner ID");

            // attempt to attach peer to user
            PeerProfile createdPeer;
            try
            {
                createdPeer = await _peers.AddPeer(newPeer);
            }
            catch(BadRequestException bre)
                { return BadRequest(bre.Message); }
            catch(ResourceNotFoundException rnfe)
                { return NotFound(rnfe.Message);}
            catch(InternalServerErrorException isee)
                { return StatusCode(StatusCodes.Status500InternalServerError, (isee.Message)); }

            // success: return the created peer
            var actionName = nameof(GetPeerById);
            var routeValue = new { id = createdPeer.Id };

            return CreatedAtAction(actionName, routeValue, createdPeer);
        }

        /// <summary>
        /// <para>PUT: /api/peers/5</para>
        /// 
        /// <para>Updates the user profile</para>
        /// </summary>
        /// <param name="id">The ID of the peer, taken from the URL</param>
        /// <param name="updatedPeer">The peer to replace the one in the database</param>
        /// <returns>An HTTP 204, 400, 404, or 500 response</returns>
        [HttpPut("{id}")]
        [Authorize(Roles = "admin,user")]
        [Produces("application/json")]
        [Consumes("application/json")]
        public async Task<ActionResult> UpdatePeer(int id, [FromBody]Peer updatedPeer)
        {
            // guard against unauthorized user
            int userId = _identity.GetUserIdFromJwt(HttpContext);
            string userRole = _identity.GetUserRoleFromJwt(HttpContext);
            if (!_security.CheckUserAuthorized(updatedPeer.OwnerId, userId, userRole))
                return NotFound($"Peer with ID {id} not found"); // return not found to hide that there might be something there

            // guard against mismatching inputs
            if (id != updatedPeer.Id)
                return BadRequest(new { error = "Conflicting IDs in URL and in body", urlId = id, bodyId = updatedPeer.Id });

            // attempt to update peer
            try
            {
                await _peers.UpdatePeer(updatedPeer);
            }
            catch (BadRequestException bre)
                { return BadRequest(bre.Message); }
            catch(ResourceNotFoundException rnfe)
                { return NotFound(rnfe.Message); }
            catch(InternalServerErrorException isee)
                { return StatusCode(StatusCodes.Status500InternalServerError, isee.Message); }

            // success
            return NoContent();
        }

        /// <summary>
        /// Deletes a peer from the database
        /// </summary>
        /// <param name="id">The ID of the peer to delete</param>
        /// <returns></returns>
        [HttpDelete("{id}")]
        [Authorize(Roles = "admin,user")]
        [Produces("application/json")]
        [Consumes("application/json")]
        public async Task<ActionResult> DeletePeer(int id)
        {
            // guard against peer not existing
            Peer? peer = await _peers.GetPeerById(id); // need to check peer not null before checking if authorized
            if (peer is null)
                return NotFound($"Peer with ID {id} not found");

            int userId = _identity.GetUserIdFromJwt(HttpContext);
            string userRole = _identity.GetUserRoleFromJwt(HttpContext);
            if (!_security.CheckUserAuthorized(peer.OwnerId, userId, userRole))
                return NotFound($"Peer with ID {id} not found"); // return not found to hide that there might be a peer

            try
            {
                await _peers.DeletePeer(peer);
            }
            catch(ResourceNotFoundException rnfe)
                { return NotFound(rnfe.Message); }
            catch(InternalServerErrorException isee)
                { return StatusCode(StatusCodes.Status500InternalServerError, isee.Message); }

            return NoContent();
        }
    }
}
