using System;
using System.Collections.Generic;

namespace TypescriptImportSync
{
    public interface IFileSystemNode
    {
        string Path { get; }

        bool IsFile { get; }

        bool Exists { get; }

        DateTime CreationTime { get; }

        IEnumerable<IFileSystemNode> GetFiles(string pattern = null);

        IEnumerable<IFileSystemNode> GetDirectories();
    }
}