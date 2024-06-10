using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using WgDashboard.Website.Models;

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

            // parse the JWT token and set authentication state
            if(token is not null && token != "")
            {
                claims = ParseClaimsFromJwt(token) ?? new Claim[] { new Claim(ClaimTypes.Role, UserRoles.Anonymous) };
                int id = 0;
                string username = "";
                string userRole = UserRoles.Anonymous;
                string? name = null;
                foreach(Claim claim in claims)
                {
                    if (claim.Type == ClaimTypes.Sid)
                    {
                        if (!int.TryParse(claim.Value, out id))
                            id = 0;
                    }
                    else if (claim.Type == ClaimTypes.NameIdentifier)
                        username = claim.Value ?? "";
                    else if (claim.Type == ClaimTypes.Role)
                        userRole = claim.Value ?? UserRoles.Anonymous;
                    else if (claim.Type == ClaimTypes.Name)
                        name = claim.Value;
                }
                identity = new ClaimsIdentity(claims, "jwt");
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Replace("\"", ""));

                AuthState.SetAuthenticatedUser(id, username, userRole, name);
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

        public async void LogoutUser()
        {
            await _localStorage.RemoveItemAsync("WireguardApiToken");
            await GetAuthenticationStateAsync();
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
            return claims?.Select((jwtClaim) => new Claim(jwtClaim.Key ?? "", jwtClaim.Value?.ToString() ?? ""));
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

    }
}
