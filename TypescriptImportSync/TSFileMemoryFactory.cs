namespace TypescriptImportSync
{
    public class TSFileMemoryFactory : ITSFileFactory
    {
        private IFileContentManager manager;
        public TSFileMemoryFactory(IFileContentManager manager)
        {
            this.manager = manager;
        }

        public ITSFile Create(string path)
        {
            var f = new TSFileMemory() { Path = path };
            f.Contents = this.manager.ReadAllText(path);

            return f;
        }
    }
}