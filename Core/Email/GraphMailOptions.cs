namespace UmbracoBase.Core.Email;

/// <summary>
/// Settings for sending site email through Microsoft Graph (appsettings.json
/// section "GraphMail"). Used on the live server because the district network
/// blocks outbound SMTP; Graph goes out over normal HTTPS (port 443).
///
/// Leave the section out (or TenantId/ClientId/SenderMailbox empty) and the
/// site keeps using Umbraco's normal SMTP sender, so local dev is unchanged.
/// </summary>
public sealed class GraphMailOptions
{
    public const string SectionName = "GraphMail";

    /// <summary>Entra "Directory (tenant) ID" of the app registration.</summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>Entra "Application (client) ID" of the app registration.</summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Thumbprint of the certificate in the server's LocalMachine\My store whose
    /// public key was uploaded to the app registration. Preferred over a secret.
    /// </summary>
    public string CertificateThumbprint { get; set; } = string.Empty;

    /// <summary>Client secret value - only used when no thumbprint is set.</summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>Mailbox every message is sent from, e.g. shs@ggusd.us.</summary>
    public string SenderMailbox { get; set; } = string.Empty;

    public bool HasCertificate => !string.IsNullOrWhiteSpace(CertificateThumbprint);

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(TenantId)
        && !string.IsNullOrWhiteSpace(ClientId)
        && !string.IsNullOrWhiteSpace(SenderMailbox)
        && (HasCertificate || !string.IsNullOrWhiteSpace(ClientSecret));
}
