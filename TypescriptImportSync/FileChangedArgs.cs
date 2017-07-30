using System;
using System.IO;

namespace TypescriptImportSync
{
    public class FileSystemChangedArgs : EventArgs
    {
        public TSFileWatcherChangeTypes ChangeType { get; set; }

        public string OldName { get; }

        public string NewName { get; }

        public FileSystemChangedArgs(TSFileWatcherChangeTypes changeType, string oldName, string newName)
        {
            this.ChangeType = changeType;
            this.OldName = oldName;
            this.NewName = newName;
        }

        public override string ToString() => ChangeType.ToString() + ": " + NewName;
    }
}