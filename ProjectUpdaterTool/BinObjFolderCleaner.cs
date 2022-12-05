namespace ProjectUpdaterTool
{
    public static class BinObjFolderCleaner
    {
        public static async Task Clean_Bin_Obj_vs_Folders()
        {
            string currentDirectory = Directory.GetCurrentDirectory();

            string[] binFolders = await getFolderFullPath(currentDirectory, "bin").ToArrayAsync();

            Console.WriteLine($"Deleting {binFolders.Length} of 'bin' folders.");

            foreach (string folderPath in binFolders)
                Directory.Delete(folderPath, true);

            string[] objFolders = await getFolderFullPath(currentDirectory, "obj").ToArrayAsync();

            Console.WriteLine($"Deleting {objFolders.Length} of 'obj' folders.");

            foreach (string folderPath in objFolders)
                Directory.Delete(folderPath, true);

            string[] visualStudioFolders = await getFolderFullPath(currentDirectory, ".vs").ToArrayAsync();

            Console.WriteLine($"Deleting {visualStudioFolders.Length} of '.vs' folders.");

            foreach (string folderPath in visualStudioFolders)
                Directory.Delete(folderPath, true);
        }

        private static async IAsyncEnumerable<string> getFolderFullPath(string rootFullPath, string folderName)
        {
            // 11 min video: https://youtu.be/qTetsXmZLk0
            // You can use like this with System.Linq.Async
            // await getFolderFullPath(currentDirectory, "bin").WhereAwait().Take(10).ToArrayAsync()

            if (isFullPathEndsWith(rootFullPath, folderName))
            {
                yield return rootFullPath;
            }
            else
            {
                foreach (string subFolderPath in Directory.GetDirectories(rootFullPath))
                {
                    // yield break;

                    await foreach (string innerFolderPath in getFolderFullPath(subFolderPath, folderName))
                        yield return innerFolderPath;
                }
            }
        }

        private static bool isFullPathEndsWith(ReadOnlySpan<char> fullPath, string folderName)
        {
            return fullPath.EndsWith(Path.DirectorySeparatorChar + folderName);
        }
    }
}
