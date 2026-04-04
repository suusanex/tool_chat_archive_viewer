using System.Net;
using System.Net.Http.Headers;
using ChatArchiveViewer.CloudFetch.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace ChatArchiveViewer.CloudFetch.Tests.Services;

[TestFixture]
public sealed class BootstrapConfigProviderTests
{
    // TP-C001a/b: bootstrap.json を正常取得して必須フィールドと scopes を読み取る
    [Test]
    public async Task UT_IT_TP_C001__GetConfigAsync_ValidJson_ReturnsConfig()
    {
        const string json = """
                            {
                              "tenantId": "tenant",
                              "clientId": "client",
                              "manifestUrl": "https://example.com/manifest.json",
                              "scopes": ["scope-a","scope-b"]
                            }
                            """;
        using var client = CreateHttpClient(HttpStatusCode.OK, json);
        var sut = new BootstrapConfigProvider(client, NullLogger<BootstrapConfigProvider>.Instance, "https://example.invalid/bootstrap.json");

        var result = await sut.GetConfigAsync(CancellationToken.None);

        Assert.That(result.TenantId, Is.EqualTo("tenant"));
        Assert.That(result.ClientId, Is.EqualTo("client"));
        Assert.That(result.ManifestUrl, Is.EqualTo("https://example.com/manifest.json"));
        Assert.That(result.Scopes, Is.EquivalentTo(new[] { "scope-a", "scope-b" }));
        Assert.That(result.Authority, Is.EqualTo("https://login.microsoftonline.com/tenant"));
    }

    // TP-C002c: tenantId 欠損はバリデーション失敗になる
    [Test]
    public void UT_IT_TP_C002__GetConfigAsync_MissingTenantId_ThrowsException()
    {
        const string json = """
                            {
                              "clientId": "client",
                              "manifestUrl": "https://example.com/manifest.json"
                            }
                            """;
        using var client = CreateHttpClient(HttpStatusCode.OK, json);
        var sut = new BootstrapConfigProvider(client, NullLogger<BootstrapConfigProvider>.Instance, "https://example.invalid/bootstrap.json");

        Assert.That(async () => await sut.GetConfigAsync(CancellationToken.None), Throws.InstanceOf<InvalidDataException>());
    }

    private static HttpClient CreateHttpClient(HttpStatusCode statusCode, string content)
    {
        var handler = new StubHttpMessageHandler(
            _ =>
            {
                var response = new HttpResponseMessage(statusCode)
                {
                    Content = new StringContent(content)
                };
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                return response;
            });
        return new HttpClient(handler);
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            _ = cancellationToken;
            return Task.FromResult(responseFactory(request));
        }
    }
}
