﻿using Azure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WgDashboard.Api.Data;
using WgDashboard.Api.Exceptions;
using WgDashboard.Api.Models;
using WgDashboard.Api.Services;

namespace WgDashboard.Api.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthenticationController : ControllerBase
    {
        private readonly ISecurityService _security;
        private readonly IIdentityService _identity;

        public AuthenticationController(ISecurityService securityService, IIdentityService identityService)
        {
            this._security = securityService;
            this._identity = identityService;
        }

        /// <summary>
        /// <para>POST: /api/auth/login</para>
        /// 
        /// <para>Creates a JWT token if the user is authenticated</para>
        /// </summary>
        /// <param name="credentials">The user's username and password</param>
        /// <returns>An HTTP 200 or 400 response</returns>
        [AllowAnonymous]
        [HttpPost("login")]
        [Produces("application/json")]
        [Consumes("application/json")]
        public async Task<ActionResult<string>> LoginUser([FromBody] LoginRequest credentials)
        {
            // try to get the user
            User? user;
            try
            {
                user = await _security.Authenticate(credentials.Username, credentials.Password);
            }
            catch (BadRequestException bre)
                { return BadRequest(bre.Message); }
            if(user is null)
                return BadRequest("Incorrect username or password");

            // success: return a new access and refresh token
            await _identity.SetRefreshToken(user, HttpContext);

            // do this last since jwt is short-lived
            string jwt = _identity.GenerateToken(new UserProfile()
            {
                Id = user.Id,
                Name = user.Name,
                Username = user.Username,
                Role = user.Role,
            });
            return Ok(jwt);
        }

        /// <summary>
        /// <para>POST: /api/auth/signup</para>
        /// 
        /// <para>Creates a new user</para>
        /// </summary>
        /// <param name="details">The user's basic details</param>
        /// <returns>An HTTP 201 or 400 response</returns>
        [AllowAnonymous]
        [HttpPost("signup")]
        [Produces("application/json")]
        [Consumes("application/json")]
        public async Task<ActionResult<UserProfile>> AddUser([FromBody] SignupRequest details)
        {
            // attempt to create a new user
            User newUser;
            try 
            {
                newUser = await _security.AddUser(details.Username, details.Password, details.Name);
            }
            catch(BadRequestException bre)
                { return BadRequest(bre.Message); }
            catch(InternalServerErrorException isee)
                { return StatusCode(StatusCodes.Status500InternalServerError, isee.Message); }

            var newUserProfile = new UserProfile()
            {
                Id = newUser.Id,
                Username = newUser.Username, 
                Role = newUser.Role, 
                Name = newUser.Name, 
            };
            var actionName = nameof(UserController.GetUserById);
            var controllerName = "User"; // UserController => "User" as the controller name
            var routeValue = new {id = newUserProfile.Id };

            return CreatedAtAction(actionName, controllerName, routeValue, newUserProfile);
        }

        /// <summary>
        /// <para>PATCH: /api/auth/passwd/5</para>
        /// 
        /// <para>Updates the user's password</para>
        /// </summary>
        /// <param name="id">The user's ID, taken from the URL</param>
        /// <param name="passwordRequest">The user's ID and new password, taken from the body</param>
        /// <returns></returns>
        [HttpPatch("passwd/{id}")]
        [Authorize(Roles = "admin,user")]
        [Produces("application/json")]
        [Consumes("application/json")]
        public async Task<ActionResult> ChangePassword(int id, [FromBody] ChangePasswordRequest passwordRequest)
        {
            // guard against bad input
            if (passwordRequest.Id != id)
                return BadRequest($"Conflicting IDs in URL and body. URL ID: {id}, Body ID: {passwordRequest.Id}");

            // guard against unauthorized (including requesing user not existing)
            int userId = _identity.GetUserIdFromJwt(HttpContext);
            string userRole = _identity.GetUserRoleFromJwt(HttpContext);
            if (!_security.CheckUserAuthorized(id, userId, userRole))
                return NotFound($"User with ID {id} not found"); // return NotFound to hide the fact that there might be a user

            // attempt to update password
            try 
            {
                await _security.UpdateUserPassword(id, passwordRequest.Password);
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
        /// 
        /// </summary>
        /// <returns></returns>
        [HttpPost("refresh")]
        [AllowAnonymous]
        [Produces("application/json")]
        public async Task<ActionResult> RefreshToken()
        {
            string jwt;
            try
            {
                jwt = await _identity.RefreshToken(HttpContext);
            }
            catch (NotAuthorizedException nae)
                { return Unauthorized(nae.Message); }

            return Ok(jwt);
        }


        [HttpDelete("revoke")]
        [AllowAnonymous]
        public async Task<ActionResult> RevokeRefreshToken()
        {
            try
            {
                await _identity.RevokeRefreshToken(HttpContext);
            }
            catch (NotAuthorizedException)
                { return Unauthorized(); }

            return NoContent();
        }
    }
}
