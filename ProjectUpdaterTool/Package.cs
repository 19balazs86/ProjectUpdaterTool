namespace ProjectUpdaterTool;

public sealed class Package
{
    public required string PackageName { get; set; }
    public required string VersionOld { get; set; }
    public required string VersionNew { get; set; }
}
