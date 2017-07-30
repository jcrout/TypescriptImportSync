using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TypescriptImportSync
{
    public class FileSystemNodeIO : IFileSystemNode
    {
        private FileSystemInfo node;

        public FileSystemNodeIO(FileSystemInfo node)
        {
            this.node = node;
        }

        public string Path { get { return this.node.FullName; } }

        public bool IsFile { get { return this.node is FileInfo; } }

        public bool Exists { get { return this.node.Exists; } }

        public DateTime CreationTime { get { return this.node.CreationTime; } }

        public IEnumerable<IFileSystemNode> GetDirectories()
        {
            if (this.IsFile)
            {
                throw new InvalidOperationException("GetDirectories was called on a file node");
            }

            return (this.node as DirectoryInfo).GetDirectories().Select(d => new FileSystemNodeIO(d));
        }

        public IEnumerable<IFileSystemNode> GetFiles(string pattern = null)
        {
            if (this.IsFile)
            {
                throw new InvalidOperationException("GetDirectories was called on a file node");
            }

            return (this.node as DirectoryInfo).GetFiles(pattern).Select(d => new FileSystemNodeIO(d));
        }
    }
}