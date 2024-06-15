using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using WgDashboard.Website.Helpers;

namespace WgDashboard.Website.Services
{
    public class AuthStateProvider : AuthenticationStateProvider
    {
        private readonly HttpClient _httpClient;
        private readonly ILocalStorageService _localStorage;

        public AuthStateProvider(HttpClient httpClient, ILocalStorageService localStorage)
        {
            this._httpClient = httpClient;
            this._localStorage = localStorage;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            string? token = await _localStorage.GetItemAsStringAsync("WireguardApiToken");

            var identity = new ClaimsIdentity();
            _httpClient.DefaultRequestHeaders.Authorization = null;
            IEnumerable<Claim>? claims = null;
            Dictionary<string, string>? claimsDict = null;

            // parse the JWT token and set authentication state
            if(!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Replace("\"", ""));
                claims = ParseClaimsFromJwt(token) ?? new Claim[] { new Claim(ClaimTypes.Role, UserRoles.Anonymous) };
                claimsDict = ParseClaimsFromJwtAsDict(token) ?? new Dictionary<string, string>(); // just for readibility later
                identity = new ClaimsIdentity(claims, "jwt");
                SetAuthState(claimsDict);
            }
            else
            {
                AuthState.LogoutUser();
            }

            var user = new ClaimsPrincipal(identity);
            var state = new AuthenticationState(user);

            NotifyAuthenticationStateChanged(Task.FromResult(state));

            return state;
        }

        private static IEnumerable<Claim>? ParseClaimsFromJwt(string jwt)
        {
            var jwtSections = jwt.Split('.');
            if(jwtSections.Length != 3)
            {
                return null;
            }

            var payload = jwtSections[1];
            var jsonBytes = ParseBase64WithoutPadding(payload);
            var claims = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);
            var result = new Dictionary<string, string>();
            return claims?.Select((jwtClaim) => new Claim(jwtClaim.Key ?? "", jwtClaim.Value?.ToString() ?? ""));
        }

        private static Dictionary<string, string>? ParseClaimsFromJwtAsDict(string jwt)
        {
            var jwtSections = jwt.Split('.');
            if(jwtSections.Length != 3)
            {
                return null;
            }

            var payload = jwtSections[1];
            var jsonBytes = ParseBase64WithoutPadding(payload);
            var claims = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);
            var result = new Dictionary<string, string>();
            if (claims is null)
                return null;
            foreach(string key in claims.Keys)
            {
                result.Add(key, claims[key]?.ToString() ?? "");
            }
            return result;
        }

        private static byte[] ParseBase64WithoutPadding(string base64)
        {
            string result = new string(base64);
            switch(result.Length % 4)
            {
                case 2: 
                    result += "==";
                    break;
                case 3:
                    result += "=";
                    break;
            }

            return Convert.FromBase64String(result);
        }

        private void SetAuthState(Dictionary<string, string> claims) 
        {
            int id;
            if (!int.TryParse(claims.GetValueOrDefault(ClaimTypes.Sid, "0"), out id))
                id = 0;
            string username = claims.GetValueOrDefault(ClaimTypes.NameIdentifier, "");
            string userRole = claims.GetValueOrDefault(ClaimTypes.Role, UserRoles.Anonymous);
            string? name = claims.GetValueOrDefault(ClaimTypes.Name, "");
            if (name == "")
                name = null;
            long expiration;
            if (!long.TryParse(claims.GetValueOrDefault("exp", "-1"), out expiration))
                expiration = -1;

            AuthState.SetAuthenticatedUser(id, username, userRole, name, expiration);
        
        }

    }
}
