using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

var builder = WebApplication.CreateBuilder(args);

// Habilitar logging detallado de Identity Model (solo en desarrollo)
if (builder.Environment.IsDevelopment())
{
    IdentityModelEventSource.ShowPII = true;
}

// Configurar servicios
builder.Services.AddControllersWithViews();

// Configurar sesiones (necesario para el testing)
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Configurar Microsoft Identity Web (nueva forma en .NET 9)
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(options =>
    {
        builder.Configuration.GetSection("AzureAd").Bind(options);
        // Configurar para preservar tokens REALES según documento del laboratorio
        options.SaveTokens = true;
        options.UseTokenLifetime = true;
        options.GetClaimsFromUserInfoEndpoint = true;
        options.ResponseType = OpenIdConnectResponseType.Code;
        options.UsePkce = true;
        
        options.Events = new OpenIdConnectEvents
        {
            OnTokenResponseReceived = context =>
            {
                // Capturar tokens cuando llegan del endpoint
                var accessToken = context.TokenEndpointResponse.AccessToken;
                var idToken = context.TokenEndpointResponse.IdToken;
                var refreshToken = context.TokenEndpointResponse.RefreshToken;
                
                // Crear lista de tokens para almacenar
                var tokens = new List<AuthenticationToken>();
                
                if (!string.IsNullOrEmpty(accessToken))
                    tokens.Add(new AuthenticationToken { Name = "access_token", Value = accessToken });
                if (!string.IsNullOrEmpty(idToken))
                    tokens.Add(new AuthenticationToken { Name = "id_token", Value = idToken });
                if (!string.IsNullOrEmpty(refreshToken))
                    tokens.Add(new AuthenticationToken { Name = "refresh_token", Value = refreshToken });
                
                // Almacenar tokens en las propiedades
                context.Properties.StoreTokens(tokens);
                
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                // Los tokens ya están guardados en OnTokenResponseReceived
                return Task.CompletedTask;
            }
        };
    });

// Configurar autorización
builder.Services.AddAuthorization(options =>
{
    // Política por defecto: usuario debe estar autenticado
    options.FallbackPolicy = options.DefaultPolicy;
});

// Configurar Razor Pages (necesario para Microsoft.Identity.Web)
builder.Services.AddRazorPages();

var app = builder.Build();

// Configurar pipeline de la aplicación
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // HSTS por defecto en .NET 9
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Configurar sesiones (debe ir después de UseRouting y antes de UseAuthentication)
app.UseSession();

// ⚠️ ORDEN CRÍTICO en .NET 9
app.UseAuthentication();
app.UseAuthorization();

// Configurar rutas
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Mapear Razor Pages (requerido por Microsoft.Identity.Web)
app.MapRazorPages();

app.Run();
