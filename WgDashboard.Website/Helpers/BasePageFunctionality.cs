using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using System.Diagnostics.CodeAnalysis;
using System.Net;
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

        protected override async Task OnInitializedAsync()
        {
            if (AuthState.Expired)
                await LogoutUser();
            else
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
            NavigationManager.NavigateTo("/login", true);
        }

        private async Task RefreshJwt()
        {
            string url = WebsiteConfig.GetSection("WireguardApiConfig").GetValue<string>("AuthRoute")
                ?? "/api/auth";
            url += "/refresh";

            HttpResponseMessage response = await client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                string jwt = (await response.Content.ReadAsStringAsync()).Replace("\"", "");
                await LocalStorage.SetItemAsync("WireguardApiToken", jwt);
                await AuthStateProvider.GetAuthenticationStateAsync();
            }
            else if (response.StatusCode == HttpStatusCode.Unauthorized)
                await LogoutUser();
            else
            {
                string error = await response.Content.ReadAsStringAsync();
                Console.WriteLine(error);
            }
        }
    }
}
