namespace ProjectUpdaterTool;

public sealed class UpdateResult(string _csprojFile)
{
    public string CsprojFile { get; private set; } = _csprojFile;

    public List<string> Packages { get; } = [];
}

public sealed class SearchResult(string _searchPattern)
{
    public string SearchPattern { get; private set; } = _searchPattern;

    public List<string> Files { get; } = [];
}
