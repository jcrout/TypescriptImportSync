namespace TypescriptImportSync
{
    public class FileWatcherFactory : IFileWatcherFactory
    {
        public IFileWatcher Create()
        {
            return new FileSystemWatcherIO();
        }
    }
}