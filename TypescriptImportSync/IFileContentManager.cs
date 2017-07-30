namespace TypescriptImportSync
{
    public interface IFileContentManager
    {
        string ReadAllText(string path, int maxRetries = 10);
        void WriteAllText(string path, string contents, int maxRetries = 10);
        bool FileExists(string path);
    }
}