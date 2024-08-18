using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using WgDashboard.Website.Services;

namespace WgDashboard.Website.Helpers
{
    public class BasePageFunctionality : ComponentBase
    {
        protected bool ok = true;
        protected string notOkReason = "";

        // AllowNull is just to prevent warnings. these shouldn't ever be null
        [Inject, AllowNull]
        protected IJSRuntime JSRuntime { get; init; }
        [Inject, AllowNull]
        protected NavigationManager NavigationManager { get; init; }
        [Inject, AllowNull]
        protected ILocalStorageService LocalStorage { get; init; }
        [Inject, AllowNull]
        protected AuthenticationStateProvider AuthStateProvider { get; init; }
        [Inject, AllowNull]
        protected IConfiguration WebsiteConfig { get; init; }
        [Inject, AllowNull]
        protected HttpClient client { get; init; }

        private class FetchResults
        {
            public int StatusCode { get; set; }
            public string ResponseBody { get; set; } = "";
            public bool IsSuccessStatusCode { get => 200 <= StatusCode && StatusCode <= 299; }
        }

        protected override async Task OnInitializedAsync()
        {
            if (AuthState.Expired)
                await RefreshJwt();
            await base.OnInitializedAsync();            
        }

        protected ValueTask ScrollToTop()
        {
            string js = "window.scrollToId";
            string id = "top";
            return JSRuntime.InvokeVoidAsync(js, id);
        }

        protected async Task SetErrorMessage(string message)
        {
            ok = false;
            notOkReason = message;
            await InvokeAsync(StateHasChanged);
            await ScrollToTop();
        }

        protected Task ClearErrorMessage()
        {
            ok = true;
            notOkReason = "";
            return InvokeAsync(StateHasChanged);
        }

        protected async Task LogoutUser()
        {
            await LocalStorage.RemoveItemAsync("WireguardApiToken");
            await AuthStateProvider.GetAuthenticationStateAsync();

            string BASE_URL = WebsiteConfig.GetSection("WireguardApiConfig").GetValue<string>("BaseUrl") ?? "http://localhost:3000";
            const string PATH = "/api/auth/revoke";
            string revokePath = BASE_URL + PATH;

            int statusCode = await JSRuntime.InvokeAsync<int>("window.revokeRefreshToken", revokePath);
            if(statusCode < 200 || 299 < statusCode)
            {
                await SetErrorMessage("Could not logout! Maybe the server is offline?");
            }
            else
                NavigationManager.NavigateTo("/login", true);
        }

        private async Task RefreshJwt()
        {
            string BASE_URL = WebsiteConfig.GetSection("WireguardApiConfig").GetValue<string>("BaseUrl") ?? "http://localhost:3000";
            const string PATH = "/api/auth/refresh";

            string refreshPath = BASE_URL + PATH;

            FetchResults response = await JSRuntime.InvokeAsync<FetchResults>("window.refreshToken", refreshPath);

            if (response.IsSuccessStatusCode)
            {
                string jwt = response.ResponseBody.Replace("\"", "");
                await LocalStorage.SetItemAsync("WireguardApiToken", jwt);
                await AuthStateProvider.GetAuthenticationStateAsync();
            }
            else if (response.StatusCode == (int)HttpStatusCode.Unauthorized)
                await LogoutUser();
            else
            {
                string error = response.ResponseBody;
                Console.WriteLine(error);
            }
        }

        protected async Task<HttpResponseMessage> SendHttpRequest<DtoType>(string route, HttpMethod httpMethod, DtoType requestDto)
        {
            var json = JsonSerializer.Serialize(requestDto);
            HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");
            HttpRequestMessage request = new HttpRequestMessage(httpMethod, route) {
                Content = content,
            };
            HttpResponseMessage response = await client.SendAsync(request);

            // if expired, refresh and try again.
            HttpStatusCode status = response.StatusCode;
            if (status == HttpStatusCode.Unauthorized)
            {
                await RefreshJwt(); // this should short-circut code if cannot refresh
                // don't do it recursively. might result in infinite loop
                var request2 = new HttpRequestMessage(httpMethod, route) { Content = content };
                HttpResponseMessage response2 = await client.SendAsync(request2);
                if (response2.StatusCode == HttpStatusCode.Unauthorized) // sanity check
                    await LogoutUser();
                else
                    response = response2;
            }

            return response;
        }

        protected async Task<HttpResponseMessage> SendHttpRequest(string route, HttpMethod httpMethod)
        {
            HttpRequestMessage request = new HttpRequestMessage(httpMethod, route);
            HttpResponseMessage response = await client.SendAsync(request);

            // if expired, refresh and try again.
            HttpStatusCode status = response.StatusCode;
            if (status == HttpStatusCode.Unauthorized)
            {
                await RefreshJwt(); // this should short-circut code if cannot refresh
                // don't do it recursively. might result in infinite loop
                var request2 = new HttpRequestMessage(httpMethod, route);
                HttpResponseMessage response2 = await client.SendAsync(request2);
                if (response2.StatusCode == HttpStatusCode.Unauthorized) // sanity check
                    await LogoutUser();
                else
                    response = response2;
            }

            return response;
        }
    }
}
