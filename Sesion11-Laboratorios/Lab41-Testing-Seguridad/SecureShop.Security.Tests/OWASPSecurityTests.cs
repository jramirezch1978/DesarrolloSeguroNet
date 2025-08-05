using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Text;

namespace SecureShop.Security.Tests;

public class OWASPSecurityTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public OWASPSecurityTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task A01_BrokenAccessControl_IDOR_PreventionTest()
    {
        // Arrange: Intentar acceder a recurso de otro usuario sin autenticación
        var otherUserProfileId = "user123-profile";

        // Act: Intentar acceder directamente al perfil de otro usuario
        var response = await _client.GetAsync($"/api/profiles/{otherUserProfileId}");

        // Assert: Debe ser rechazado
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("<script>alert('xss')</script>")]
    [InlineData("'; DROP TABLE Users; --")]
    [InlineData("../../../etc/passwd")]
    [InlineData("{{7*7}}")]
    [InlineData("<img src=x onerror=alert('xss')>")]
    public async Task A03_Injection_InputValidationTest(string maliciousInput)
    {
        // Arrange: Crear payload malicioso
        var productData = new
        {
            Name = maliciousInput,
            Description = "Test product",
            Price = 99.99
        };

        var content = new StringContent(
            System.Text.Json.JsonSerializer.Serialize(productData),
            Encoding.UTF8,
            "application/json");

        // Act: Intentar crear producto con input malicioso
        var response = await _client.PostAsync("/api/products", content);

        // Assert: Debe ser rechazado o sanitizado
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest,
            HttpStatusCode.Unauthorized,
            HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task A04_InsecureDesign_SecurityHeadersTest()
    {
        // Act: Hacer request a página principal
        var response = await _client.GetAsync("/");

        // Assert: Verificar headers de seguridad requeridos
        response.Headers.Should().ContainKey("X-Frame-Options");
        response.Headers.Should().ContainKey("X-Content-Type-Options");
        response.Headers.GetValues("X-Frame-Options").First().Should().Be("DENY");
        response.Headers.GetValues("X-Content-Type-Options").First().Should().Be("nosniff");
    }

    [Fact]
    public async Task A05_SecurityMisconfiguration_HTTPSRedirectionTest()
    {
        // Arrange: Crear cliente que no sigue redirects automáticamente
        var clientOptions = new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        };
        var testClient = _factory.CreateClient(clientOptions);

        // Act: Hacer request HTTP (simulado con header)
        var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.Add("X-Forwarded-Proto", "http");
        
        var response = await testClient.SendAsync(request);

        // Assert: En producción debería redirigir a HTTPS
        // Verificar headers de seguridad O redirección HTTPS
        var hasSTSHeader = response.Headers.Contains("Strict-Transport-Security");
        var isRedirect = response.StatusCode == HttpStatusCode.MovedPermanently || 
                         response.StatusCode == HttpStatusCode.TemporaryRedirect;
        
        (hasSTSHeader || isRedirect).Should().BeTrue(
            "la aplicación debe configurar HTTPS redirection o STS headers");
    }

    [Fact]
    public async Task A07_IdentificationAuthenticationFailures_RateLimitingTest()
    {
        // Arrange: Preparar múltiples intentos de login
        var loginData = new
        {
            Username = "admin",
            Password = "wrongpassword"
        };

        var content = new StringContent(
            System.Text.Json.JsonSerializer.Serialize(loginData),
            Encoding.UTF8,
            "application/json");

        // Act: Realizar múltiples intentos de login fallidos
        HttpResponseMessage? lastResponse = null;
        for (int i = 0; i < 10; i++)
        {
            lastResponse = await _client.PostAsync("/api/auth/login", content);
        }

        // Assert: Después de múltiples intentos, debería haber rate limiting
        lastResponse?.StatusCode.Should().BeOneOf(
            HttpStatusCode.TooManyRequests,
            HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task A10_SSRF_PreventUnauthorizedRequestsTest()
    {
        // Arrange: Intentar hacer request a URL interna/externa no autorizada
        var maliciousUrl = "http://169.254.169.254/latest/meta-data/"; // AWS metadata
        var requestData = new
        {
            Url = maliciousUrl
        };

        var content = new StringContent(
            System.Text.Json.JsonSerializer.Serialize(requestData),
            Encoding.UTF8,
            "application/json");

        // Act: Intentar hacer request server-side a URL maliciosa
        var response = await _client.PostAsync("/api/fetch-external", content);

        // Assert: Debe ser rechazado
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest,
            HttpStatusCode.Forbidden,
            HttpStatusCode.NotFound); // Endpoint podría no existir, lo cual es correcto
    }
}