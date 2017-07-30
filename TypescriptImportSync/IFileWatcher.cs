using System;

namespace TypescriptImportSync
{
    public interface IFileWatcher : IDisposable
    {
        event EventHandler Disposing;

        event EventHandler<FileSystemChangedArgs> FileSystemChanged;

        void WatchDirectory(string path);
    }
}