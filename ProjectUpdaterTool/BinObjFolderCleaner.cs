namespace ProjectUpdaterTool
{
    public static class BinObjFolderCleaner
    {
        public static void Clean_Bin_Obj_vs_Folders()
        {
            string currentDirectory = Directory.GetCurrentDirectory();

            string[] binFolders = Directory.GetDirectories(currentDirectory, "bin", SearchOption.AllDirectories);

            Console.WriteLine($"Deleting {binFolders.Length} of 'bin' folders.");

            foreach (string folderPath in binFolders)
                Directory.Delete(folderPath, true);

            string[] objFolders = Directory.GetDirectories(currentDirectory, "obj", SearchOption.AllDirectories);

            Console.WriteLine($"Deleting {binFolders.Length} of 'obj' folders.");

            foreach (string folderPath in objFolders)
                Directory.Delete(folderPath, true);

            string[] visualStudioFolders = Directory.GetDirectories(currentDirectory, ".vs", SearchOption.AllDirectories);

            Console.WriteLine($"Deleting {visualStudioFolders.Length} of '.vs' folders.");

            foreach (string folderPath in visualStudioFolders)
                Directory.Delete(folderPath, true);
        }
    }
}
