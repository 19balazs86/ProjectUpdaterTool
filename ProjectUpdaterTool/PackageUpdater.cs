using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ProjectUpdaterTool;

public static partial class PackageUpdater
{
    private static readonly string             _rootFolder       = Directory.GetCurrentDirectory();
    private static readonly List<SearchResult> _searchResults    = [new("*.csproj"), new("Directory.Packages.props")];
    private static readonly List<UpdateResult> _results          = [];
    private static readonly List<Package>      _packagesToUpdate = [];

    private static readonly HashSet<string> _excludedFolders = [".git", "bin", "obj", ".vs", ".idea"];

    private static readonly HashSet<string>.AlternateLookup<ReadOnlySpan<char>> _excludedFoldersLookup =
        _excludedFolders.GetAlternateLookup<ReadOnlySpan<char>>();

    public static async Task Update(bool isTestMode)
    {
        try
        {
            await readPackagesToUpdate();

            searchFiles();

            await workingOnFiles_ReplacePackages(isTestMode, _searchResults[0].Files);

            await workingOnFiles_ReplacePackages(isTestMode, _searchResults[1].Files);

            await using Stream resultJsonStream = File.Create(Path.Combine(_rootFolder, "PackagesToUpdateResult.json"));

            await JsonSerializer.SerializeAsync(resultJsonStream, _results, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync(ex.ToString());
        }
    }

    private static async Task workingOnFiles_ReplacePackages(bool isTestMode, IEnumerable<string> files)
    {
        foreach (string filePath in files)
        {
            (UpdateResult result, string fileContent) = replacePackages(filePath, isTestMode);

            if (result is null)
            {
                continue;
            }

            _results.Add(result);

            if (!isTestMode)
            {
                await File.WriteAllTextAsync(filePath, fileContent, Encoding.UTF8);
            }
        }
    }

    private static (UpdateResult result, string fileContent) replacePackages(string filePath, bool isTestMode)
    {
        string fileContent = File.ReadAllText(filePath, Encoding.UTF8);

        UpdateResult result = null;

        foreach (Package package in _packagesToUpdate)
        {
            //var regex = new Regex($"Include=\"{package.PackageName}\" Version=\"[^\"]+\"");

            var regex = new Regex($"Include=\"{package.PackageName}\" Version=\"{package.VersionOld}\"");

            if (!regex.IsMatch(fileContent))
            {
                continue;
            }

            result ??= new UpdateResult(filePath.Replace(_rootFolder, string.Empty));

            result.Packages.Add(package.PackageName);

            string withNewVersion = $"Include=\"{package.PackageName}\" Version=\"{package.VersionNew}\"";

            if (!isTestMode)
            {
                fileContent = regex.Replace(fileContent, withNewVersion);
            }
        }

        return (result, fileContent);
    }

    private static async Task readPackagesToUpdate()
    {
        string packagesFilePath = Path.Combine(_rootFolder, "PackagesToUpdate.txt");

        if (!File.Exists(packagesFilePath))
        {
            throw new FileNotFoundException($"The package file was not found at '{packagesFilePath}'");
        }

        string input = await File.ReadAllTextAsync(packagesFilePath);

        // NOTE: dotnet-outdated, when used with the --output flag, can generate a JSON report, which is easier to process than using regex
        Regex regex = getDotnetOutdatedRegex();

        foreach (Match match in regex.Matches(input))
        {
            var package = new Package
            {
                PackageName = match.Groups[1].Value,
                VersionOld  = match.Groups[2].Value,
                VersionNew  = match.Groups[3].Value
            };

            _packagesToUpdate.Add(package);
        }
    }

    private static void searchFiles()
    {
        Queue<string> directories = [];

        directories.Enqueue(_rootFolder);

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
                if (!isExcludedFolder(subFolder))
                {
                    directories.Enqueue(subFolder);
                }
            }
        }
    }

    private static bool isExcludedFolder(ReadOnlySpan<char> folderFullPath)
    {
        ReadOnlySpan<char> folderName = Path.GetFileName(folderFullPath); // For a full folder path, it retrieves only the folder name

        return _excludedFoldersLookup.Contains(folderName);
    }

    [GeneratedRegex(@"([^\s]+)\s+([^\s]+)\s+->\s+([^\s]+)")]
    private static partial Regex getDotnetOutdatedRegex();
    // private static partial Regex _dotnetOutdatedRegex { get; } // Supported in .NET 9
}
