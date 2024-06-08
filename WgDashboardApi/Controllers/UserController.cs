using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WgDashboardApi.Data;
using WgDashboardApi.Exceptions;
using WgDashboardApi.Models;
using WgDashboardApi.Services;

namespace WgDashboardApi.Controllers
{
    [Route("api/users")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IIdentityService _identity;
        private readonly ISecurityService _security;
        private readonly IUserService _users;

        /*
        * Contructor for controller
        */
        public UserController(IIdentityService identityService, ISecurityService securityService, IUserService userServices)
        {
            this._identity = identityService;
            this._security = securityService;
            this._users = userServices;
        }

        /// <summary>
        /// <para>GET: /api/users</para>
        /// <para>Retrieves all users from the database</para>
        /// </summary>
        /// <returns>An HTTP 200 response</returns>
        [HttpGet]
        [Authorize(Roles = "admin")]
        [Produces("application/json")]
        [Consumes("application/json")]
        public ActionResult<IEnumerable<UserProfile>> GetAllUsers()
        {
            return Ok(_users.GetAllUsers());
        }

        /// <summary>
        /// <para>GET: /api/users/5</para>
        /// 
        /// <para>Gets a user by a specific ID</para>
        /// </summary>
        /// <param name="id">The ID of the user to retrieve, taken from the URL</param>
        /// <returns>An HTTP 200 or 404 response</returns>
        [HttpGet("{id}", Name = "GetUserById")]
        [Authorize(Roles = "admin,user")]
        [Produces("application/json")]
        [Consumes("application/json")]
        public async Task<ActionResult<UserProfile>> GetUserById(int id)
        {
            // guard against unauthorized user
            int requestingUserId = _identity.GetUserIdFromJwt(HttpContext);
            string requestingUserRole = _identity.GetUserRoleFromJwt(HttpContext);
            if (!_security.CheckUserAuthorized(id, requestingUserId, requestingUserRole))
                return NotFound($"User with ID {id} not found"); // return NotFound to hide that there might be a user

            // try to get the user's profile
            UserProfile? profile = await _users.GetUserProfileById(id);
            if (profile is null)
                return NotFound($"User with ID {id} not found");

            // success
            return Ok(profile);
        }

        /// <summary>
        /// <para>PUT: /api/users/5</para>
        /// 
        /// <para>Updates the user profile</para>
        /// </summary>
        /// <param name="id">The ID of the user to be updated, taken from the URL</param>
        /// <param name="updatedUserProfile">The updated user profile, taken from the body</param>
        /// <returns>An HTTP 200, 400, 403, or 500 response</returns>
        [HttpPut("{id}")]
        [Authorize(Roles = "admin,user")]
        [Produces("application/json")]
        [Consumes("application/json")]
        public async Task<ActionResult> UpdateUserProfile(int id, [FromBody] UpdateUserRequest updatedUserProfile)
        {
            // guard against bad input
            if (updatedUserProfile.Id != id)
                return BadRequest($"Conflicting IDs in URL and in body. URL ID: {id}, body ID: {updatedUserProfile.Id}");

            // guards against unauthorized users
            int requestingUsersId = _identity.GetUserIdFromJwt(HttpContext);
            string requestingUsersRole = _identity.GetUserRoleFromJwt(HttpContext);
            if (!_security.CheckUserAuthorized(id, requestingUsersId, requestingUsersRole)) 
                return NotFound($"User with ID {id} not found"); // return NotFound to hide that there might be a user
            if (requestingUsersRole != UserRoles.Admin && requestingUsersRole != updatedUserProfile.Role) // make sure the user isn't elevating their own privileges
                return Forbid($"Insufficient priviliges to change user's role");

            // try to update the profile
            try
            {
                await _users.UpdateUserProfile(id, updatedUserProfile.Username, updatedUserProfile.Name, updatedUserProfile.Role);
            }
            catch(BadRequestException bre)
                { return BadRequest(bre.Message); }
            catch(ResourceNotFoundException rnfe)
                { return NotFound(rnfe.Message); }
            catch(InternalServerErrorException isee)
                { return StatusCode(StatusCodes.Status500InternalServerError, isee.Message); }

            // success
            return NoContent();
        }

        /// <summary>
        /// Deletes the user's account
        /// </summary>
        /// <param name="id">The user ID of the account to be deleted, taken from the URL</param>
        /// <returns>An HTTP 204, 404, or 500 response</returns>
        [HttpDelete("{id}")]
        [Authorize(Roles = "admin,user")]
        [Produces("application/json")]
        [Consumes("application/json")]
        public async Task<ActionResult> DeleteUser(int id)
        {
            // guard against unauthorized user
            int requestingUserId = _identity.GetUserIdFromJwt(HttpContext);
            string requestingUserRole = _identity.GetUserRoleFromJwt(HttpContext);
            if (!_security.CheckUserAuthorized(id, requestingUserId, requestingUserRole))
                return NotFound($"User with ID {id} not found"); // return NotFound to hide that there might be a user

            // attempt to delete the user
            try
            {
                await _users.DeleteUserById(id);
            }
            catch (ResourceNotFoundException rnfe)
                { return NotFound(rnfe.Message); }
            catch (InternalServerErrorException isee)
                { return StatusCode(StatusCodes.Status500InternalServerError, isee.Message); }

            // success
            return NoContent();
        }
    }
}
