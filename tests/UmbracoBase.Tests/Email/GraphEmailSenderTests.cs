using System.Net;
using System.Text.Json.Nodes;
using Azure.Core;
using Microsoft.Extensions.Logging.Abstractions;
using Umbraco.Cms.Core.Models.Email;
using UmbracoBase.Core.Email;
using Xunit;

namespace UmbracoBase.Tests.Email;

public class GraphEmailSenderTests
{
    private static GraphMailOptions Configured() => new()
    {
        TenantId = "tenant-id",
        ClientId = "client-id",
        CertificateThumbprint = "ABC123",
        SenderMailbox = "shs@ggusd.us",
    };

    private static EmailMessage Message(string[]? replyTo = null, bool html = false) => new(
        from: "ignored@example.com",
        to: new[] { "alum@example.com" },
        cc: null,
        bcc: null,
        replyTo: replyTo,
        subject: "Hello",
        body: "Body text",
        isBodyHtml: html,
        attachments: null);

    [Fact]
    public async Task Posts_to_sender_mailbox_with_bearer_token()
    {
        var handler = new RecordingHandler(HttpStatusCode.Accepted);
        var sender = new GraphEmailSender(new FakeFactory(handler), Configured(), NullLogger<GraphEmailSender>.Instance, new FakeCredential());

        await sender.SendAsync(Message(), "Test");

        Assert.Equal("https://graph.microsoft.com/v1.0/users/shs%40ggusd.us/sendMail", handler.Request!.RequestUri!.ToString());
        Assert.Equal(HttpMethod.Post, handler.Request.Method);
        Assert.Equal("Bearer", handler.Request.Headers.Authorization!.Scheme);
        Assert.Equal("fake-token", handler.Request.Headers.Authorization.Parameter);

        var json = JsonNode.Parse(handler.Body!)!;
        Assert.Equal("alum@example.com", (string?)json["message"]!["toRecipients"]![0]!["emailAddress"]!["address"]);
        Assert.True((bool)json["saveToSentItems"]!);
    }

    [Fact]
    public void Payload_maps_body_type_and_reply_to()
    {
        var text = GraphEmailSender.BuildPayload(Message(replyTo: new[] { "sender@example.com" }));
        Assert.Equal("Text", (string?)text["message"]!["body"]!["contentType"]);
        Assert.Equal("sender@example.com", (string?)text["message"]!["replyTo"]![0]!["emailAddress"]!["address"]);
        Assert.Null(text["message"]!["ccRecipients"]);

        var html = GraphEmailSender.BuildPayload(Message(html: true));
        Assert.Equal("HTML", (string?)html["message"]!["body"]!["contentType"]);
        Assert.Null(html["message"]!["replyTo"]);
    }

    [Fact]
    public async Task Graph_error_throws_with_status_and_body()
    {
        var handler = new RecordingHandler(HttpStatusCode.Forbidden, "{\"error\":{\"code\":\"ErrorAccessDenied\"}}");
        var sender = new GraphEmailSender(new FakeFactory(handler), Configured(), NullLogger<GraphEmailSender>.Instance, new FakeCredential());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => sender.SendAsync(Message(), "Test"));
        Assert.Contains("403", ex.Message);
        Assert.Contains("ErrorAccessDenied", ex.Message);
    }

    [Fact]
    public void Unconfigured_options_report_cannot_send()
    {
        var sender = new GraphEmailSender(new FakeFactory(new RecordingHandler(HttpStatusCode.Accepted)), new GraphMailOptions(), NullLogger<GraphEmailSender>.Instance, new FakeCredential());
        Assert.False(sender.CanSendRequiredEmail());

        var secretOnly = new GraphMailOptions { TenantId = "t", ClientId = "c", ClientSecret = "s", SenderMailbox = "shs@ggusd.us" };
        Assert.True(secretOnly.IsConfigured);
    }

    private sealed class FakeCredential : TokenCredential
    {
        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
            => new("fake-token", DateTimeOffset.UtcNow.AddHours(1));

        public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
            => new(GetToken(requestContext, cancellationToken));
    }

    private sealed class FakeFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    private sealed class RecordingHandler(HttpStatusCode status, string responseBody = "") : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }
        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            Body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(status) { Content = new StringContent(responseBody) };
        }
    }
}
