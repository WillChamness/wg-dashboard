using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using System.Diagnostics.CodeAnalysis;
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
            {
                await LogoutUser();
            }
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

        }
    }
}
