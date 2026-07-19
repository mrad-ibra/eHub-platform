using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace eHub.IntegrationTests;

public sealed class BookingsApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public BookingsApiTests(WebApplicationFactory<Program> factory)
    {
        // Keep smoke tests on InMemory booking adapters (no Postgres required).
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:DefaultConnection", "");
        });
    }

    [Fact]
    public async Task PostBookings_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/bookings");
        request.Headers.TryAddWithoutValidation("Idempotency-Key", "it-1");
        request.Content = JsonContent.Create(new
        {
            assetId = Guid.NewGuid(),
            startDate = "2026-08-01",
            endDate = "2026-08-05"
        });

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetBooking_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync($"/api/v1/bookings/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
