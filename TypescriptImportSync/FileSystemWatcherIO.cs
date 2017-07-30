using System;
using System.IO;

namespace TypescriptImportSync
{
    public class FileSystemWatcherIO : IFileWatcher
    {
        private static readonly char[] trimChars = new char[] { '\\' };
        private FileSystemWatcher watcher;
      
        public event EventHandler<FileSystemChangedArgs> FileSystemChanged;
        public event EventHandler Disposing;

        public void WatchDirectory(string path)
        {
            if (this.watcher != null)
            {
                this.watcher.Dispose();
                this.watcher = null;
            }

            this.watcher = new FileSystemWatcher(path);
            watcher.Error += Watcher_Error;
            watcher.Deleted += Fsw_Deleted;
            watcher.Created += Fsw_Created;
            watcher.Renamed += Fsw_Renamed;
            watcher.Changed += Fsw_Changed;
            watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.DirectoryName | NotifyFilters.FileName;
            watcher.EnableRaisingEvents = true;
            watcher.IncludeSubdirectories = true;
        }

        private void Watcher_Error(object sender, ErrorEventArgs e)
        {           
            this.Dispose();
        }

        private void _FileSystemChanged(TSFileWatcherChangeTypes type, string oldPath, string newPath)
        {
            FileSystemChanged?.Invoke(this, new FileSystemChangedArgs(type, oldPath, newPath));
        }

        private bool IsTsChange(string path)
        {
            return path.ToLower().EndsWith(".ts") && File.Exists(path);
        }

        private void Fsw_Renamed(object sender, RenamedEventArgs e)
        {
            if (IsTsChange(e.FullPath))
            {
                _FileSystemChanged(TSFileWatcherChangeTypes.FileRenamed, e.OldFullPath, e.FullPath);
            }
            else if (Directory.Exists(e.FullPath))
            {
                _FileSystemChanged(TSFileWatcherChangeTypes.DirectoryRenamed, e.OldFullPath?.TrimEnd(trimChars), e.FullPath.TrimEnd(trimChars));
            }
        }

        private void Fsw_Deleted(object sender, FileSystemEventArgs e)
        {
            if (e.FullPath.ToLower().EndsWith(".ts"))
            {
                _FileSystemChanged(TSFileWatcherChangeTypes.FileDeleted, null, e.FullPath);
            }
            else if (Directory.Exists(e.FullPath))
            {
                _FileSystemChanged(TSFileWatcherChangeTypes.DirectoryDeleted, null, e.FullPath.TrimEnd(trimChars));
            }
        }

        private void Fsw_Changed(object sender, FileSystemEventArgs e)
        {
            if (IsTsChange(e.FullPath))
            {
                _FileSystemChanged(TSFileWatcherChangeTypes.FileChanged, null, e.FullPath);
            }
        }

        private void Fsw_Created(object sender, FileSystemEventArgs e)
        {
            if (IsTsChange(e.FullPath))
            {
                _FileSystemChanged(TSFileWatcherChangeTypes.FileCreated, null, e.FullPath);
            }
            else if (Directory.Exists(e.FullPath))
            {
                _FileSystemChanged(TSFileWatcherChangeTypes.DirectoryCreated, null, e.FullPath);
            }
        }

        public void Dispose()
        {
            if (this.watcher != null)
            {
                this.watcher.Dispose();
                this.watcher = null;                
            }

            Disposing?.Invoke(this, EventArgs.Empty);
        }
    }
}