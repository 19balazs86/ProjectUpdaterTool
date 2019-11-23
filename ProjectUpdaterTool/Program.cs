using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ProjectUpdaterTool
{
  public class Program
  {
    public static async Task Main(string[] args)
    {
      bool isTestMode = args.Contains("-test");

      try
      {
        string currentDirectory = Directory.GetCurrentDirectory();

        IEnumerable<Package> packagesToUpdate = JsonSerializer.Deserialize<IEnumerable<Package>>(
          File.ReadAllText(Path.Combine(currentDirectory, "PackagesToUpdate.json")));

        string[] csprojFiles = Directory.GetFiles(currentDirectory, "*.csproj", SearchOption.AllDirectories);

        List<Result> results = new List<Result>();

        foreach (string csprojFilePath in csprojFiles)
        {
          string csprojContent = File.ReadAllText(csprojFilePath);

          Result result = null;

          foreach (Package package in packagesToUpdate)
          {
            Regex regex = new Regex($"Include=\"{package.PackageName}\" Version=\"{package.VersionOld}\"");

            if (regex.IsMatch(csprojContent))
            {
              if (result is null)
                result = new Result(csprojFilePath.Replace(currentDirectory, string.Empty));

              result.Packages.Add(package.PackageName);

              string withNewVersion = $"Include=\"{package.PackageName}\" Version=\"{package.VersionNew}\"";

              if (!isTestMode)
                csprojContent = regex.Replace(csprojContent, withNewVersion);
            }
          }

          if (result is null) continue;

          results.Add(result);

          if (isTestMode) continue;

          using FileStream csprojOutStream = File.Create(csprojFilePath);

          // BOM
          csprojOutStream.WriteByte(0xEF);
          csprojOutStream.WriteByte(0xBB);
          csprojOutStream.WriteByte(0xBF);

          byte[] bytes = Encoding.UTF8.GetBytes(csprojContent);

          await csprojOutStream.WriteAsync(bytes, 0, bytes.Length);
          await csprojOutStream.FlushAsync();
        }

        using Stream resultsOutStream = File.Create(Path.Combine(currentDirectory, "PackagesToUpdateResult.json"));

        await JsonSerializer.SerializeAsync(resultsOutStream, results, new JsonSerializerOptions { WriteIndented = true });
      }
      catch (Exception ex)
      {
        Console.Error.WriteLine(ex.ToString());
      }
    }
  }
}
