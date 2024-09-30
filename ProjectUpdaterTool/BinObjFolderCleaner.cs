namespace ProjectUpdaterTool;

public static class BinObjFolderCleaner
{
    public static void Clean_Bin_Obj_vs_Folders()
    {
        string currentDirectory = Directory.GetCurrentDirectory();

        var foldersToDelete = new Dictionary<string, List<string>>
        {
            ["bin"] = [],
            ["obj"] = [],
            [".vs"] = []
        };

        findFoldersToDelete(currentDirectory, foldersToDelete);

        foreach (var item in foldersToDelete)
        {
            Console.WriteLine($"Deleting {item.Value.Count} of '{item.Key}' folders.");

            foreach (string folderPath in item.Value)
                Directory.Delete(folderPath, true);
        }
    }

    private static void findFoldersToDelete(string rootFullPath, Dictionary<string, List<string>> foldersToDelete)
    {
        if (rootFullPath.endsWith(".git"))
        {
            return;
        }

        foreach (var item in foldersToDelete)
        {
            if (rootFullPath.endsWith(item.Key))
            {
                item.Value.Add(rootFullPath);

                return;
            }
        }

        foreach (string subFolderPath in Directory.GetDirectories(rootFullPath))
            findFoldersToDelete(subFolderPath, foldersToDelete);
    }

    private static bool endsWith(this string folderFullPath, string folderName)
    {
        return folderFullPath.EndsWith(Path.DirectorySeparatorChar + folderName);
    }
}
