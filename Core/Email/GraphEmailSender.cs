using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json.Nodes;
using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Mail;
using Umbraco.Cms.Core.Models.Email;

namespace UmbracoBase.Core.Email;

/// <summary>
/// Umbraco IEmailSender that sends through Microsoft Graph's
/// /users/{mailbox}/sendMail using an Entra app registration (application
/// permission Mail.Send). Every message goes out from GraphMailOptions.SenderMailbox;
/// the EmailMessage.From value is ignored because Graph only sends as the
/// mailbox in the URL. Reply-To is passed through, so contact-relay replies
/// still reach the original sender.
///
/// Registered by GraphMailComposer only when the "GraphMail" section is
/// configured; otherwise Umbraco's SMTP sender stays in place.
/// Failures throw - every caller in this site already catches and logs.
/// </summary>
public sealed class GraphEmailSender : IEmailSender
{
    public const string HttpClientName = "GraphMail";
    private const string GraphScope = "https://graph.microsoft.com/.default";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly GraphMailOptions _options;
    private readonly ILogger<GraphEmailSender> _logger;
    private readonly Lazy<TokenCredential> _credential;

    public GraphEmailSender(
        IHttpClientFactory httpClientFactory,
        IOptions<GraphMailOptions> options,
        ILogger<GraphEmailSender> logger)
        : this(httpClientFactory, options.Value, logger, credential: null)
    {
    }

    /// <summary>Test seam: pass a fake credential instead of a real Entra one.</summary>
    internal GraphEmailSender(
        IHttpClientFactory httpClientFactory,
        GraphMailOptions options,
        ILogger<GraphEmailSender> logger,
        TokenCredential? credential)
    {
        _httpClientFactory = httpClientFactory;
        _options = options;
        _logger = logger;
        // Built once and reused: Azure.Identity caches the access token inside
        // the credential, so a new one per send would fetch a token every time.
        // PublicationOnly: a failed load (cert missing / no key permission) is
        // retried on the next send instead of being cached until app restart.
        _credential = credential is not null
            ? new Lazy<TokenCredential>(credential)
            : new Lazy<TokenCredential>(() => CreateCredential(options), LazyThreadSafetyMode.PublicationOnly);
    }

    public bool CanSendRequiredEmail() => _options.IsConfigured;

    public Task SendAsync(EmailMessage message, string emailType)
        => SendAsync(message, emailType, enableNotification: false, expires: null);

    public Task SendAsync(EmailMessage message, string emailType, bool enableNotification)
        => SendAsync(message, emailType, enableNotification, expires: null);

    public async Task SendAsync(EmailMessage message, string emailType, bool enableNotification, DateTime? expires)
    {
        if (!_options.IsConfigured)
        {
            throw new InvalidOperationException("GraphMail is not configured (TenantId, ClientId, SenderMailbox and a certificate or secret are required).");
        }

        var token = await _credential.Value.GetTokenAsync(new TokenRequestContext(new[] { GraphScope }), CancellationToken.None);

        var url = $"https://graph.microsoft.com/v1.0/users/{Uri.EscapeDataString(_options.SenderMailbox)}/sendMail";
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(BuildPayload(message).ToJsonString(), Encoding.UTF8, "application/json"),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);

        var client = _httpClientFactory.CreateClient(HttpClientName);
        using var response = await client.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            // Graph error bodies are JSON {error:{code,message}} - no secrets, safe to log.
            var body = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException(
                $"Graph sendMail ({emailType}) failed: {(int)response.StatusCode} {response.ReasonPhrase}. {body}");
        }

        _logger.LogInformation("Graph email {EmailType} sent from {Mailbox}.", emailType, _options.SenderMailbox);
    }

    /// <summary>Builds the Graph sendMail request body. Internal for tests.</summary>
    internal static JsonObject BuildPayload(EmailMessage message)
    {
        var graphMessage = new JsonObject
        {
            ["subject"] = message.Subject ?? string.Empty,
            ["body"] = new JsonObject
            {
                ["contentType"] = message.IsBodyHtml ? "HTML" : "Text",
                ["content"] = message.Body ?? string.Empty,
            },
            ["toRecipients"] = Recipients(message.To),
        };

        if (message.Cc is { Length: > 0 }) { graphMessage["ccRecipients"] = Recipients(message.Cc); }
        if (message.Bcc is { Length: > 0 }) { graphMessage["bccRecipients"] = Recipients(message.Bcc); }
        if (message.ReplyTo is { Length: > 0 }) { graphMessage["replyTo"] = Recipients(message.ReplyTo); }

        if (message.HasAttachments && message.Attachments is not null)
        {
            var attachments = new JsonArray();
            foreach (var attachment in message.Attachments)
            {
                using var buffer = new MemoryStream();
                attachment.Stream.CopyTo(buffer);
                attachments.Add(new JsonObject
                {
                    ["@odata.type"] = "#microsoft.graph.fileAttachment",
                    ["name"] = attachment.FileName,
                    ["contentBytes"] = Convert.ToBase64String(buffer.ToArray()),
                });
            }
            graphMessage["attachments"] = attachments;
        }

        return new JsonObject
        {
            ["message"] = graphMessage,
            // Keeps a copy in the mailbox's Sent Items, so staff can see what went out.
            ["saveToSentItems"] = true,
        };
    }

    private static JsonArray Recipients(IEnumerable<string>? addresses)
    {
        var array = new JsonArray();
        foreach (var address in addresses ?? Enumerable.Empty<string>())
        {
            if (string.IsNullOrWhiteSpace(address)) { continue; }
            array.Add(new JsonObject { ["emailAddress"] = new JsonObject { ["address"] = address.Trim() } });
        }
        return array;
    }

    private static TokenCredential CreateCredential(GraphMailOptions options)
    {
        if (!options.HasCertificate)
        {
            return new ClientSecretCredential(options.TenantId, options.ClientId, options.ClientSecret);
        }

        // The private key stays in the Windows certificate store; the IIS app pool
        // identity needs Read on it (certlm.msc > Manage Private Keys).
        var thumbprint = options.CertificateThumbprint.Replace(" ", string.Empty).Trim();
        using var store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
        store.Open(OpenFlags.ReadOnly);
        var matches = store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, validOnly: false);
        if (matches.Count == 0)
        {
            throw new InvalidOperationException(
                $"GraphMail certificate with thumbprint {thumbprint} was not found in LocalMachine\\My.");
        }

        return new ClientCertificateCredential(options.TenantId, options.ClientId, matches[0]);
    }
}
