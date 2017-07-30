using System;

namespace TypescriptImportSync
{
    public class FileSystemWatcherMock : IFileWatcher
    {
        public static event EventHandler Created;

        public event EventHandler<FileSystemChangedArgs> FileChanged;

        public FileSystemWatcherMock()
        {
            var createdEvent = Created;
            if (createdEvent != null)
            {
                createdEvent.Invoke(this, EventArgs.Empty);
            }
        }

        public void Dispose()
        {
        }

        public void WatchDirectory(string path)
        {
        }

        public void Raise(FileSystemChangedArgs args)
        {
            var ev = this.FileChanged;
            if (ev != null)
            {
                ev.Invoke(this, args);
            }
        }
    }
}