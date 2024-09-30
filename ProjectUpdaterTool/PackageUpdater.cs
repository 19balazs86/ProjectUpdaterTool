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

            await workingOnCsprojFiles(isTestMode, _searchResults[0].Files);

            await workingOnPackagesPropsFiles(isTestMode, _searchResults[1].Files);

            using Stream resultsOutStream = File.Create(Path.Combine(_rootFolder, "PackagesToUpdateResult.json"));

            await JsonSerializer.SerializeAsync(resultsOutStream, _results, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.ToString());
        }
    }

    private static async Task workingOnCsprojFiles(bool isTestMode, IEnumerable<string> csprojFiles)
    {
        // string[] csprojFiles = Directory.GetFiles(_rootFolder, "*.csproj", SearchOption.AllDirectories);

        foreach (string csprojFilePath in csprojFiles)
        {
            (UpdateResult result, string fileContent) = replacePackages(csprojFilePath, isTestMode);

            if (result is null) continue;

            _results.Add(result);

            if (!isTestMode)
                await saveCsprojContent(fileContent, csprojFilePath);
        }
    }

    private static async Task workingOnPackagesPropsFiles(bool isTestMode, IEnumerable<string> packagesPropsFiles)
    {
        // string[] packagesPropsFiles = Directory.GetFiles(_rootFolder, "Directory.Packages.props", SearchOption.AllDirectories);

        foreach (string propFilePath in packagesPropsFiles)
        {
            (UpdateResult result, string fileContent) = replacePackages(propFilePath, isTestMode);

            if (result is null) continue;

            _results.Add(result);

            if (!isTestMode)
                await File.WriteAllTextAsync(propFilePath, fileContent, Encoding.UTF8);
        }
    }

    private static (UpdateResult result, string fileContent) replacePackages(string filePath, bool isTestMode)
    {
        string fileContent = File.ReadAllText(filePath);

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

    private static async Task saveCsprojContent(string csprojContent, string csprojFilePath)
    {
        using FileStream csprojOutStream = File.Create(csprojFilePath);

        // BOM
        csprojOutStream.WriteByte(0xEF);
        csprojOutStream.WriteByte(0xBB);
        csprojOutStream.WriteByte(0xBF);

        byte[] bytes = Encoding.UTF8.GetBytes(csprojContent);

        await csprojOutStream.WriteAsync(bytes);
        await csprojOutStream.FlushAsync();
    }

    private static void searchFiles(string rootFolder)
    {
        Stack<string> directories = [];

        directories.Push(rootFolder);

        while (directories.Count > 0)
        {
            string currentFolder = directories.Pop();

            foreach (SearchResult searchResult in _searchResults)
            {
                string[] desiredFiles = Directory.GetFiles(currentFolder, searchResult.SearchPattern);

                searchResult.Files.AddRange(desiredFiles);
            }

            foreach (string subFolder in Directory.GetDirectories(currentFolder))
            {
                if (Array.Exists(_excludedFolders, excludedFolderName => isFullPathEndsWith(subFolder, excludedFolderName)))
                {
                    continue;
                }

                directories.Push(subFolder);
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