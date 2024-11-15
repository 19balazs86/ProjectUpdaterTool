namespace ProjectUpdaterTool;

public static class BinObjFolderCleaner
{
    private static readonly Dictionary<string, List<string>> _foldersToDelete = new()
    {
        ["bin"] = [],
        ["obj"] = [],
        [".vs"] = []
    };

    private static readonly Dictionary<string, List<string>>.AlternateLookup<ReadOnlySpan<char>> _alternateLookup =
        _foldersToDelete.GetAlternateLookup<ReadOnlySpan<char>>();


    public static void Clean_Bin_Obj_vs_Folders()
    {
        string currentDirectory = Directory.GetCurrentDirectory();

        findFoldersToDelete(currentDirectory);

        foreach (var item in _foldersToDelete)
        {
            Console.WriteLine($"Deleting {item.Value.Count} of '{item.Key}' folders.");

            foreach (string folderPath in item.Value)
                Directory.Delete(folderPath, true);
        }
    }

    private static void findFoldersToDelete(string rootFullPath)
    {
        ReadOnlySpan<char> actualFolderName = Path.GetFileName(rootFullPath.AsSpan()); // For a full folder path, it retrieves only the folder name

        if (actualFolderName is ".git") // Ignore the .git folder
        {
            return;
        }

        if (_alternateLookup.TryGetValue(actualFolderName, out List<string> folderList))
        {
            folderList.Add(rootFullPath);

            return;
        }

        foreach (string subFolderPath in Directory.GetDirectories(rootFullPath))
        {
            findFoldersToDelete(subFolderPath);
        }
    }
}
