﻿namespace ProjectUpdaterTool;

public static class Program
{
    public static async Task Main(string[] args)
    {
        bool isbinObjFolderCleaner = args.Contains("-clean");

        if (isbinObjFolderCleaner)
        {
            BinObjFolderCleaner.Clean_Bin_Obj_vs_Folders();
        }
        else
        {
            bool isTestMode = args.Contains("-test");

            await PackageUpdater.Update(isTestMode);
        }
    }
}
