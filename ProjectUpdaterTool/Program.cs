namespace ProjectUpdaterTool
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            bool isbinObjFolderCleaner = args.Contains("-BinObjFolderCleaner");

            if (isbinObjFolderCleaner)
            {
                BinObjFolderCleaner.CleanBinObjFolders();
            }
            else
            {
                bool isTestMode = args.Contains("-test");

                await PackageUpdater.Update(isTestMode);
            }
        }
    }
}
