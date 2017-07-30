namespace TypescriptImportSync
{
    public class FileWatcherMockFactory : IFileWatcherFactory
    {
        public IFileWatcher Create()
        {
            return new FileSystemWatcherMock();
        }
    }
}