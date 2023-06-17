using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ProjectUpdaterTool;

public static class PackageUpdater
{
    private static readonly Regex _dotnetOutdatedRegex = new Regex(@"([^\s]+)\s+([^\s]+)\s+->\s+([^\s]+)");

    public static async Task Update(bool isTestMode)
    {
        try
        {
            string currentDirectory = Directory.GetCurrentDirectory();

            IEnumerable<Package> packagesToUpdate = await getPackages(currentDirectory);

            string[] csprojFiles = Directory.GetFiles(currentDirectory, "*.csproj", SearchOption.AllDirectories);

            var results = new List<Result>();

            foreach (string csprojFilePath in csprojFiles)
            {
                string csprojContent = File.ReadAllText(csprojFilePath);

                Result result = null;

                foreach (Package package in packagesToUpdate)
                {
                    var regex = new Regex($"Include=\"{package.PackageName}\" Version=\"{package.VersionOld}\"");

                    if (regex.IsMatch(csprojContent))
                    {
                        result ??= new Result(csprojFilePath.Replace(currentDirectory, string.Empty));

                        result.Packages.Add(package.PackageName);

                        string withNewVersion = $"Include=\"{package.PackageName}\" Version=\"{package.VersionNew}\"";

                        if (!isTestMode)
                            csprojContent = regex.Replace(csprojContent, withNewVersion);
                    }
                }

                if (result is null) continue;

                results.Add(result);

                if (!isTestMode)
                    await saveCsprojContent(csprojContent, csprojFilePath);
            }

            using Stream resultsOutStream = File.Create(Path.Combine(currentDirectory, "PackagesToUpdateResult.json"));

            await JsonSerializer.SerializeAsync(resultsOutStream, results, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.ToString());
        }
    }

    private static async Task<IEnumerable<Package>> getPackages(string currentDirectory)
    {
        var packages = new List<Package>();

        string input = await File.ReadAllTextAsync(Path.Combine(currentDirectory, "PackagesToUpdate.txt"));

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
