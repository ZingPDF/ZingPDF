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
                "Inter-regular"))
            .Invokes((Stream stream, string _, string _) => capturedBytes = ReadAllBytes(stream))
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
            "Inter-regular")).MustHaveHappenedOnceExactly();
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
