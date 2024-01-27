using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ProjectUpdaterTool;

public static class PackageUpdater
{
    private static readonly Regex _dotnetOutdatedRegex;
    private static readonly string _rootFolder;
    private static readonly List<Result> _results;
    private static IEnumerable<Package> _packagesToUpdate;

    static PackageUpdater()
    {
        _dotnetOutdatedRegex = new Regex(@"([^\s]+)\s+([^\s]+)\s+->\s+([^\s]+)");

        _rootFolder = Directory.GetCurrentDirectory();

        _results = new List<Result>();
    }

    public static async Task Update(bool isTestMode)
    {
        try
        {
            _packagesToUpdate = await getPackagesToUpdate();

            await workingOnCsprojFiles(isTestMode);

            await workingOnPackagesPropsFiles(isTestMode);

            using Stream resultsOutStream = File.Create(Path.Combine(_rootFolder, "PackagesToUpdateResult.json"));

            await JsonSerializer.SerializeAsync(resultsOutStream, _results, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.ToString());
        }
    }

    private static async Task workingOnCsprojFiles(bool isTestMode)
    {
        string[] csprojFiles = Directory.GetFiles(_rootFolder, "*.csproj", SearchOption.AllDirectories);

        foreach (string csprojFilePath in csprojFiles)
        {
            (Result result, string fileContent) = replacePackages(csprojFilePath, isTestMode);

            if (result is null) continue;

            _results.Add(result);

            if (!isTestMode)
                await saveCsprojContent(fileContent, csprojFilePath);
        }
    }

    private static async Task workingOnPackagesPropsFiles(bool isTestMode)
    {
        string[] packagesPropsFiles = Directory.GetFiles(_rootFolder, "Directory.Packages.props", SearchOption.AllDirectories);

        foreach (string propFilePath in packagesPropsFiles)
        {
            (Result result, string fileContent) = replacePackages(propFilePath, isTestMode);

            if (result is null) continue;

            _results.Add(result);

            if (!isTestMode)
                await File.WriteAllTextAsync(propFilePath, fileContent, Encoding.UTF8);
        }
    }

    private static (Result result, string fileContent) replacePackages(string filePath, bool isTestMode)
    {
        string fileContent = File.ReadAllText(filePath);

        Result result = null;

        foreach (Package package in _packagesToUpdate)
        {
            //var regex = new Regex($"Include=\"{package.PackageName}\" Version=\"[^\"]+\""); // Ignore old version

            var regex = new Regex($"Include=\"{package.PackageName}\" Version=\"{package.VersionOld}\"");

            if (regex.IsMatch(fileContent))
            {
                result ??= new Result(filePath.Replace(_rootFolder, string.Empty));

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

        await csprojOutStream.WriteAsync(bytes, 0, bytes.Length);
        await csprojOutStream.FlushAsync();
    }
}
