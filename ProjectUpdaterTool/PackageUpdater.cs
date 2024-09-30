using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ProjectUpdaterTool;

public static partial class PackageUpdater
{
    private static readonly string[] _excludedFolders  = [".git", "bin", "obj", ".vs"];
    private static readonly Regex _dotnetOutdatedRegex = getDotnetOutdatedRegex();
    private static readonly string _rootFolder         = Directory.GetCurrentDirectory();

    private static readonly List<SearchResult> _searchResults = [new SearchResult("*.csproj"), new SearchResult("Directory.Packages.props")];

    private static readonly List<UpdateResult> _results = [];

    private static IEnumerable<Package> _packagesToUpdate;

    public static async Task Update(bool isTestMode)
    {
        try
        {
            _packagesToUpdate = await getPackagesToUpdate();

            searchFiles(_rootFolder);

            await workingOnFiles_ReplacePackages(isTestMode, _searchResults[0].Files);

            await workingOnFiles_ReplacePackages(isTestMode, _searchResults[1].Files);

            using Stream resultsOutStream = File.Create(Path.Combine(_rootFolder, "PackagesToUpdateResult.json"));

            await JsonSerializer.SerializeAsync(resultsOutStream, _results, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.ToString());
        }
    }

    private static async Task workingOnFiles_ReplacePackages(bool isTestMode, IEnumerable<string> files)
    {
        foreach (string filePath in files)
        {
            (UpdateResult result, string fileContent) = replacePackages(filePath, isTestMode);

            if (result is null) continue;

            _results.Add(result);

            if (!isTestMode)
                await File.WriteAllTextAsync(filePath, fileContent, Encoding.UTF8);
        }
    }

    private static (UpdateResult result, string fileContent) replacePackages(string filePath, bool isTestMode)
    {
        string fileContent = File.ReadAllText(filePath, Encoding.UTF8);

        UpdateResult result = null;

        foreach (Package package in _packagesToUpdate)
        {
            //var regex = new Regex($"Include=\"{package.PackageName}\" Version=\"[^\"]+\""); // Ignore old version

            var regex = new Regex($"Include=\"{package.PackageName}\" Version=\"{package.VersionOld}\"");

            if (regex.IsMatch(fileContent))
            {
                result ??= new UpdateResult(filePath.Replace(_rootFolder, string.Empty));

                result.Packages.Add(package.PackageName);

                string withNewVersion = $"Include=\"{package.PackageName}\" Version=\"{package.VersionNew}\"";

                if (!isTestMode)
                    fileContent = regex.Replace(fileContent, withNewVersion);
            }
        }

        return (result, fileContent);
    }

    private static async Task<IEnumerable<Package>> getPackagesToUpdate()
    {
        var packages = new List<Package>();

        string input = await File.ReadAllTextAsync(Path.Combine(_rootFolder, "PackagesToUpdate.txt"));

        foreach (Match match in _dotnetOutdatedRegex.Matches(input))
        {
            var package = new Package
            {
                PackageName = match.Groups[1].Value,
                VersionOld  = match.Groups[2].Value,
                VersionNew  = match.Groups[3].Value
            };

            packages.Add(package);
        }

        return packages;
    }

    private static void searchFiles(string rootFolder)
    {
        Queue<string> directories = [];

        directories.Enqueue(rootFolder);

        while (directories.Count > 0)
        {
            string currentFolder = directories.Dequeue();

            foreach (SearchResult searchResult in _searchResults)
            {
                string[] desiredFiles = Directory.GetFiles(currentFolder, searchResult.SearchPattern);

                searchResult.Files.AddRange(desiredFiles);
            }

            foreach (string subFolder in Directory.GetDirectories(currentFolder))
            {
                if (_excludedFolders.Any(excludedFolderName => isFullPathEndsWith(subFolder, excludedFolderName)))
                {
                    continue;
                }

                directories.Enqueue(subFolder);
            }
        }
    }

    private static bool isFullPathEndsWith(this ReadOnlySpan<char> folderFullPath, string folderName)
    {
        return folderFullPath.EndsWith(Path.DirectorySeparatorChar + folderName);
    }

    [GeneratedRegex(@"([^\s]+)\s+([^\s]+)\s+->\s+([^\s]+)")]
    private static partial Regex getDotnetOutdatedRegex();
}