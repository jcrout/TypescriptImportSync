using System.Collections.Generic;

namespace TypescriptImportSync
{
    public class FileContentManagerMock : FileContentManagerBase
    {
        private Dictionary<string, string> fakeFileSystem;

        public FileContentManagerMock()
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