using System.Net;
using System.Net.Http;
using System.Text;
using FakeItEasy;
using FluentAssertions;
using Xunit;
using ZingPDF.GoogleFonts;

namespace ZingPDF.Tests.Unit.GoogleFonts;

public class GoogleFontsClientTests
{
    [Fact]
    public async Task DownloadFontAsync_UsesRequestedVariant()
    {
        var expectedBytes = Encoding.ASCII.GetBytes("font-bytes");
        using var httpClient = new HttpClient(new StubHttpMessageHandler(request =>
        {
            if (request.RequestUri!.AbsoluteUri.StartsWith("https://www.googleapis.com/webfonts/v1/webfonts", StringComparison.Ordinal))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        """
                        {
                          "items": [
                            {
                              "family": "Inter",
                              "category": "sans-serif",
                              "files": {
                                "regular": "https://example.test/inter-regular.ttf",
                                "700": "https://example.test/inter-700.ttf"
                              }
                            }
                          ]
                        }
                        """,
                        Encoding.UTF8,
                        "application/json")
                };
            }

            if (request.RequestUri.AbsoluteUri == "https://example.test/inter-700.ttf")
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent(expectedBytes)
                };
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }));

        var client = new GoogleFontsClient("api-key", httpClient);

        await using var fontStream = await client.DownloadFontAsync(new GoogleFontRequest
        {
            Family = "Inter",
            Variant = "700"
        });

        fontStream.ToArray().Should().Equal(expectedBytes);
    }

    [Fact]
    public async Task RegisterGoogleFontAsync_DelegatesToPdfRegistration()
    {
        var expectedBytes = Encoding.ASCII.GetBytes("font-bytes");
        using var httpClient = new HttpClient(new StubHttpMessageHandler(request =>
        {
            if (request.RequestUri!.AbsoluteUri.StartsWith("https://www.googleapis.com/webfonts/v1/webfonts", StringComparison.Ordinal))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        """
                        {
                          "items": [
                            {
                              "family": "Inter",
                              "category": "sans-serif",
                              "files": {
                                "regular": "https://example.test/inter-regular.ttf"
                              }
                            }
                          ]
                        }
                        """,
                        Encoding.UTF8,
                        "application/json")
                };
            }

            if (request.RequestUri.AbsoluteUri == "https://example.test/inter-regular.ttf")
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent(expectedBytes)
                };
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }));

        var client = new GoogleFontsClient("api-key", httpClient);
        var pdf = A.Fake<IPdf>();
        byte[]? capturedBytes = null;

        A.CallTo(() => pdf.RegisterTrueTypeFontAsync(
                A<Stream>.Ignored,
                "GF",
                null))
            .Invokes((Stream stream, string _, string? _) => capturedBytes = ReadAllBytes(stream))
            .Returns(Task.FromResult<ZingPDF.Text.PdfFont>(null!));

        var result = await pdf.RegisterGoogleFontAsync(
            client,
            new GoogleFontRequest { Family = "Inter" },
            resourceName: "GF");

        result.Should().BeNull();
        capturedBytes.Should().Equal(expectedBytes);
        A.CallTo(() => pdf.RegisterTrueTypeFontAsync(
            A<Stream>.Ignored,
            "GF",
            null)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task KeylessDownload_RequestsCombinedTrueTypeVariant_WithoutDocumentTextOrKey()
    {
        var requests = new List<string>();
        using var http = new HttpClient(new StubHttpMessageHandler(request =>
        {
            requests.Add(request.RequestUri!.AbsoluteUri);
            if (request.RequestUri.Host == "fonts.googleapis.com")
            {
                request.Headers.UserAgent.ToString().Should().Be("Mozilla/5.0");
                return new(HttpStatusCode.OK) { Content = new StringContent("@font-face { src: url(https://fonts.gstatic.com/example.ttf) format('truetype'); }") };
            }
            return new(HttpStatusCode.OK) { Content = new ByteArrayContent([0, 1, 0, 0]) };
        }));
        var client = new GoogleFontsClient(http);
        using var stream = await client.DownloadFontAsync(new() { Family = "Noto Sans", Variant = "700italic" });
        stream.Position.Should().Be(0); stream.ToArray().Should().Equal(0, 1, 0, 0);
        requests.Should().Equal("https://fonts.googleapis.com/css?family=Noto+Sans:700italic&subset=latin,latin-ext,greek,greek-ext,cyrillic,cyrillic-ext,vietnamese", "https://fonts.gstatic.com/example.ttf");
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.GetFamilyAsync("Noto Sans"));
    }

    [Theory]
    [InlineData("url(https://example.test/font.ttf) format('truetype')")]
    [InlineData("url(https://fonts.gstatic.com/font.woff2) format('woff2')")]
    [InlineData("url(https://fonts.gstatic.com/a.ttf) format('truetype'); url(https://fonts.gstatic.com/b.ttf) format('truetype')")]
    public async Task KeylessDownload_RejectsNonTrueTypeSplitOrUntrustedResponses(string css)
    {
        using var http = new HttpClient(new StubHttpMessageHandler(_ => new(HttpStatusCode.OK) { Content = new StringContent(css) }));
        await Assert.ThrowsAsync<NotSupportedException>(() => new GoogleFontsClient(http).DownloadFontAsync(new() { Family = "Noto Sans" }));
    }

    [Fact]
    public async Task RegisterUnicodeGoogleFontAsync_UsesUnicodeRegistration()
    {
        using var http = new HttpClient(new StubHttpMessageHandler(request => request.RequestUri!.Host == "fonts.googleapis.com"
            ? new(HttpStatusCode.OK) { Content = new StringContent("url(https://fonts.gstatic.com/font.ttf) format('truetype')") }
            : new(HttpStatusCode.OK) { Content = new ByteArrayContent([0, 1, 0, 0]) }));
        var pdf = A.Fake<IPdf>(); byte[]? bytes = null;
        A.CallTo(() => pdf.RegisterUnicodeTrueTypeFontAsync(A<Stream>.Ignored, "Unicode", null))
            .Invokes((Stream input, string _, string? _) => bytes = ReadAllBytes(input))
            .Returns(Task.FromResult<ZingPDF.Text.PdfFont>(null!));
        await pdf.RegisterUnicodeGoogleFontAsync(new GoogleFontsClient(http), new() { Family = "Noto Sans" }, "Unicode");
        bytes.Should().Equal(0, 1, 0, 0);
        A.CallTo(() => pdf.RegisterTrueTypeFontAsync(A<Stream>.Ignored, A<string?>.Ignored, A<string?>.Ignored)).MustNotHaveHappened();
    }

    private static byte[] ReadAllBytes(Stream stream)
    {
        var originalPosition = stream.Position;
        stream.Position = 0;

        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        stream.Position = originalPosition;

        return memory.ToArray();
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(responder(request));
    }
}
