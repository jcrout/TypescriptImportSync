using System;

namespace TypescriptImportSync
{
    public class FileSystemWatcherMock : IFileWatcher
    {
        public static event EventHandler Created;

        public event EventHandler<FileSystemChangedArgs> FileSystemChanged;
        public event EventHandler Disposing;

        public FileSystemWatcherMock()
        {
            Created?.Invoke(this, EventArgs.Empty);
        }

        public void Dispose()
        {
            Disposing?.Invoke(this, EventArgs.Empty);
        }

        public void WatchDirectory(string path)
        {
        }

        public void Raise(FileSystemChangedArgs args)
        {
            var ev = this.FileSystemChanged;
            if (ev != null)
            {
                ev.Invoke(this, args);
            }
        }
    }
}