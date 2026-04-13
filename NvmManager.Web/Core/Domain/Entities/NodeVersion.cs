namespace NvmManager.Web.Core.Domain.Entities;

/// <summary>
/// Representa uma versão do Node.js gerenciada pelo NVM.
/// </summary>
public sealed class NodeVersion
{
    public string Version { get; }
    public bool IsActive { get; }
    public bool IsLts { get; }
    public string? Architecture { get; }

    public NodeVersion(string version, bool isActive = false, bool isLts = false, string? architecture = null)
    {
        if (string.IsNullOrWhiteSpace(version))
            throw new ArgumentException("Version cannot be null or empty.", nameof(version));

        Version     = version.Trim().TrimStart('v');
        IsActive    = isActive;
        IsLts       = isLts;
        Architecture = architecture;
    }

    public string DisplayVersion => $"v{Version}";

    public override string ToString() => DisplayVersion;
}
