using System.Diagnostics;

namespace ProjectUpdaterTool;

[DebuggerDisplay("{PackageName} | {VersionOld} -> {VersionNew}")]
public sealed class Package
{
    public required string PackageName { get; init; }
    public required string VersionOld { get; init; }
    public required string VersionNew { get; init; }
}
