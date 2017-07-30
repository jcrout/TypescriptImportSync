namespace TypescriptImportSync
{
    public interface IFileSystemNodeFactory
    {
        IFileSystemNode GetFileNode(string path);
    }
}