using System;

namespace TypescriptImportSync
{
    public interface IFileWatcher : IDisposable
    {
        void WatchDirectory(string path);

        event EventHandler<FileSystemChangedArgs> FileChanged;
    }
}