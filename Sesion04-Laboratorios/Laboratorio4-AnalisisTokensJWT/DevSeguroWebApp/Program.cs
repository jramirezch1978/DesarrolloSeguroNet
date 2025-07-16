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

// Configurar Microsoft Identity Web con tokens guardados
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(options =>
    {
        builder.Configuration.Bind("AzureAd", options);
        options.SaveTokens = true; // HABILITAR ACCESO A TOKENS JWT
        options.ResponseType = OpenIdConnectResponseType.Code;
        options.UsePkce = true;
        
        // CRÍTICO PARA REFRESH TOKEN: Agregar scope offline_access explícitamente
        options.Scope.Clear();
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.Scope.Add("email");
        options.Scope.Add("offline_access"); // ESTE ES CRÍTICO PARA REFRESH TOKEN
        
        // Forzar consent para obtener refresh token
        options.Prompt = "consent";
        
        // Configuración específica para capturar tokens
        options.Events = new OpenIdConnectEvents
        {
            OnTokenResponseReceived = context =>
            {
                // Forzar que los tokens se almacenen en las propiedades de autenticación
                var tokens = new List<Microsoft.AspNetCore.Authentication.AuthenticationToken>();
                
                if (!string.IsNullOrEmpty(context.TokenEndpointResponse?.AccessToken))
                {
                    tokens.Add(new Microsoft.AspNetCore.Authentication.AuthenticationToken
                    {
                        Name = "access_token",
                        Value = context.TokenEndpointResponse.AccessToken
                    });
                }
                
                if (!string.IsNullOrEmpty(context.TokenEndpointResponse?.IdToken))
                {
                    tokens.Add(new Microsoft.AspNetCore.Authentication.AuthenticationToken
                    {
                        Name = "id_token", 
                        Value = context.TokenEndpointResponse.IdToken
                    });
                }
                
                if (!string.IsNullOrEmpty(context.TokenEndpointResponse?.RefreshToken))
                {
                    tokens.Add(new Microsoft.AspNetCore.Authentication.AuthenticationToken
                    {
                        Name = "refresh_token",
                        Value = context.TokenEndpointResponse.RefreshToken
                    });
                }
                
                context.Properties.StoreTokens(tokens);
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
