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
        string actualFolderName = Path.GetFileName(rootFullPath); // For a full folder path, it retrieves only the folder name

        if (".git" == actualFolderName) // Ignore the .git folder
        {
            return;
        }

        if (foldersToDelete.TryGetValue(actualFolderName, out List<string> folderList))
        {
            folderList.Add(rootFullPath);

            return;
        }

        foreach (string subFolderPath in Directory.GetDirectories(rootFullPath))
        {
            findFoldersToDelete(subFolderPath, foldersToDelete);
        }
    }
}
