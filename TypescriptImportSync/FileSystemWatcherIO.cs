using System;
using System.IO;

namespace TypescriptImportSync
{
    public class FileSystemWatcherIO : IFileWatcher
    {
        private static readonly char[] trimChars = new char[] { '\\' };
        private FileSystemWatcher watcher;

        public event EventHandler<FileSystemChangedArgs> FileChanged;

        public void WatchDirectory(string path)
        {
            if (this.watcher != null)
            {
                this.watcher.Dispose();
                this.watcher = null;
            }

            this.watcher = new FileSystemWatcher(path);
            watcher.Deleted += Fsw_Deleted;
            watcher.Created += Fsw_Created;
            watcher.Renamed += Fsw_Renamed;
            watcher.Changed += Fsw_Changed;
            watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.DirectoryName | NotifyFilters.FileName;
            watcher.EnableRaisingEvents = true;
            watcher.IncludeSubdirectories = true;
        }

        private void FileSystemChanged(EventHandler<FileSystemChangedArgs> ev, TSFileWatcherChangeTypes type, string oldPath, string newPath)
        {
            if (ev == null)
            {
                return;
            }

            ev.Invoke(this, new FileSystemChangedArgs(type, oldPath, newPath));
        }

        private bool IsTsChange(string path)
        {
            return path.ToLower().EndsWith(".ts") && File.Exists(path);
        }

        private void Fsw_Renamed(object sender, RenamedEventArgs e)
        {
            if (IsTsChange(e.FullPath))
            {
                FileSystemChanged(this.FileChanged, TSFileWatcherChangeTypes.FileRenamed, e.OldFullPath, e.FullPath);
            }
            else if (Directory.Exists(e.FullPath))
            {
                FileSystemChanged(this.FileChanged, TSFileWatcherChangeTypes.DirectoryRenamed, e.OldFullPath?.TrimEnd(trimChars), e.FullPath.TrimEnd(trimChars));
            }
        }

        private void Fsw_Deleted(object sender, FileSystemEventArgs e)
        {
            if (e.FullPath.ToLower().EndsWith(".ts"))
            {
                FileSystemChanged(this.FileChanged, TSFileWatcherChangeTypes.FileDeleted, null, e.FullPath);
            }
            else if (Directory.Exists(e.FullPath))
            {
                FileSystemChanged(this.FileChanged, TSFileWatcherChangeTypes.DirectoryDeleted, null, e.FullPath.TrimEnd(trimChars));
            }
        }

        private void Fsw_Changed(object sender, FileSystemEventArgs e)
        {
            if (IsTsChange(e.FullPath))
            {
                FileSystemChanged(this.FileChanged, TSFileWatcherChangeTypes.FileChanged, null, e.FullPath);
            }
        }

        private void Fsw_Created(object sender, FileSystemEventArgs e)
        {
            if (IsTsChange(e.FullPath))
            {
                FileSystemChanged(this.FileChanged, TSFileWatcherChangeTypes.FileCreated, null, e.FullPath);
            }
        }

        public void Dispose()
        {
            if (this.watcher != null)
            {
                this.watcher.Dispose();
                this.watcher = null;
            }
        }
    }
}