using System;
using System.IO;

namespace TypescriptImportSync
{
    public abstract class FileContentManagerBase : IFileContentManager
    {
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

        public virtual bool FileExists(string path)
        {
            return System.IO.File.Exists(path);
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
    }
}