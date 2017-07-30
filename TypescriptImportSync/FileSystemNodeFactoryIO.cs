using System.IO;

namespace TypescriptImportSync
{
    public class FileSystemNodeFactoryIO : IFileSystemNodeFactory
    {
        public IFileSystemNode GetFileNode(string path)
        {
            return new FileSystemNodeIO(File.Exists(path) ? (FileSystemInfo)new FileInfo(path) : (FileSystemInfo)new DirectoryInfo(path));
        }
    }
}