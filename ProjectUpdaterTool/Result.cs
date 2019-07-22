using System.Collections.Generic;

namespace ProjectUpdaterTool
{
  public class Result
  {
    public string CsprojFile { get; private set; }

    public List<string> Packages { get; private set; }

    public Result(string csprojFile)
    {
      CsprojFile = csprojFile;

      Packages = new List<string>();
    }
  }
}
