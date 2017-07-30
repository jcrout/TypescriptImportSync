using System;
using System.Collections.Generic;

namespace TypescriptImportSync
{
    public class FileNodeMock : IFileSystemNode
    {
        public FileNodeMock(string path)
        {
            this.Path = path;
        }

        public string Path { get; }

        public bool IsFile => throw new NotImplementedException();

        public bool Exists => throw new NotImplementedException();

        public DateTime CreationTime => throw new NotImplementedException();

        public IEnumerable<IFileSystemNode> GetDirectories()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IFileSystemNode> GetFiles(string pattern = null)
        {
            throw new NotImplementedException();
        }
    }

    public class FileNodeFactoryMock : IFileSystemNodeFactory
    {
        public IFileSystemNode GetFileNode(string path)
        {
            return new FileNodeMock(path);
        }
    }

    public class FileContentManagerMock : FileContentManagerBase
    {
        private Dictionary<string, string> fakeFileSystem;

        public FileContentManagerMock(IFileSystemNodeFactory nodeFactory) : base(nodeFactory)
        {
            this.fakeFileSystem = new Dictionary<string, string>();
        }

        public void AddFakeFile(string path, string contents)
        {
            if (!this.fakeFileSystem.ContainsKey(path))
            {
                this.fakeFileSystem.Add(path, contents);
            }            
        }

        public override bool FileExists(string path)
        {
            if (this.fakeFileSystem.ContainsKey(path))
            {
                return true;
            }
            else
            {
                return base.FileExists(path);
            }     
        }

        protected override string _ReadAllText(string path)
        {
            if (this.fakeFileSystem.ContainsKey(path))
            {
                return this.fakeFileSystem[path];
            }
            else
            {
                return base._ReadAllText(path);
            }
        }

        protected override void _WriteAllText(string path, string contents)
        {
            if (this.fakeFileSystem.ContainsKey(path))
            {
                this.fakeFileSystem[path] = contents;
            }
            else
            {
                base._WriteAllText(path, contents);
            }
        }
    }
}