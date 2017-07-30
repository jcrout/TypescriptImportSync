using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TypescriptImportSync
{
    public abstract class FileContentManagerBase : IFileContentManager
    {
        private IFileSystemNodeFactory nodeFactory;

        public virtual string ReadAllText(string path, int maxRetries = 10)
        {
            var retries = 0;
            while (true)
            {
                try
                {
                    return _ReadAllText(path);
                }
                catch (Exception)
                {
                    retries++;
                    if (retries == maxRetries)
                    {
                        throw;
                    }

                    System.Threading.Thread.Sleep(5);
                }
            }
        }

        public virtual void WriteAllText(string path, string contents, int maxRetries = 10)
        {
            var retries = 0;
            while (true)
            {
                try
                {
                    _WriteAllText(path, contents);
                }
                catch (Exception)
                {
                    retries++;
                    if (retries == maxRetries)
                    {
                        throw;
                    }

                    System.Threading.Thread.Sleep(5);
                }
            }
        }

        public FileContentManagerBase(IFileSystemNodeFactory nodeFactory)
        {
            this.nodeFactory = nodeFactory;
        }

        public virtual bool FileExists(string path)
        {
            return System.IO.File.Exists(path);
        }

        public virtual bool DirectoryExists(string path)
        {
            return System.IO.Directory.Exists(path);
        }

        protected virtual void _WriteAllText(string path, string contents)
        {
            //using (var sw = new FileStream(path, FileMode.)
            File.WriteAllText(path, contents);
        }

        protected virtual string _ReadAllText(string path)
        {
            return File.ReadAllText(path);
        }

        public IEnumerable<string> GetDirectories(string path)
        {
            return new DirectoryInfo(path).GetDirectories().Select(d => d.FullName);
        }

        public IEnumerable<string> GetFiles(string path, string pattern = null)
        {
            return new DirectoryInfo(path).GetFiles(pattern).Select(d => d.FullName);
        }

        public IFileSystemNode GetFileNode(string path)
        {
            return this.nodeFactory.GetFileNode(path);
        }
    }
}