namespace ProjectUpdaterTool;

public sealed class UpdateResult
{
    public string CsprojFile { get; private set; }

    public List<string> Packages { get; } = [];

    public UpdateResult(string csprojFile)
    {
        CsprojFile = csprojFile;
    }
}

public sealed class SearchResult
{
    public string SearchPattern { get; private set; }

    public List<string> Files { get; } = [];

    public SearchResult(string searchPattern)
    {
        SearchPattern = searchPattern;
    }
}