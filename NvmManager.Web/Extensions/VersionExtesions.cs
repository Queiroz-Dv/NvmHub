namespace NvmManager.Web.Extensions
{
    public static class VersionExtesions
    {
        public static string? ConcatenateVersion(this string? version)
        {
            return version == null ? null : string.Concat("v", version);
        }
    }
}
