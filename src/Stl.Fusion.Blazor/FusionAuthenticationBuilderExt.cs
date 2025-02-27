using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Stl.Fusion.Authentication;

namespace Stl.Fusion.Blazor;

public static class FusionAuthenticationBuilderExt
{
    public static FusionAuthenticationBuilder AddBlazor(
        this FusionAuthenticationBuilder fusionAuth,
        Action<AuthorizationOptions>? configure = null)
    {
        configure ??= _ => {}; // .NET 5.0 doesn't allow to pass null to AddAuthorizationCore
        var services = fusionAuth.Services;
        if (services.HasService<ClientAuthHelper>())
            return fusionAuth;

        fusionAuth.Fusion.AddBlazorUIServices();
        services.AddAuthorizationCore(configure);
        services.RemoveAll(typeof(AuthenticationStateProvider));
        services.TryAddSingleton(_ => new AuthStateProvider.Options());
        services.TryAddScoped<AuthenticationStateProvider>(c => new AuthStateProvider(
            c.GetRequiredService<AuthStateProvider.Options>(), c));
        services.TryAddScoped(c => (AuthStateProvider)c.GetRequiredService<AuthenticationStateProvider>());
        services.TryAddScoped(c => new ClientAuthHelper(c));
        return fusionAuth;
    }
}
