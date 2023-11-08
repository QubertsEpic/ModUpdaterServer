using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    internal class FolderTracker
    {
        public string FolderLocation { get; private set; }
        private string ApplicationLocation = AppDomain.CurrentDomain.BaseDirectory;
        private string FolderName = "Mods";
        private string FullPath => System.IO.Path.Combine(ApplicationLocation, FolderName);
        public FolderTracker() 
        {
            FindFolder();
            FolderLocation = FullPath;
        }

        private void FindFolder()
        {
            if (!Directory.Exists(FullPath))
            {
                Console.WriteLine("Path " + FullPath + " does not exist. Creating.");
                CreateFolder();
            }
        }

        private void CreateFolder()
        {
            if(!Directory.Exists(FullPath)) 
            {
                Directory.CreateDirectory(FullPath);
            }
        }
    }

    public enum FolderState
    {
        Uninitialised,
        Ready,
        Error
    }
}
