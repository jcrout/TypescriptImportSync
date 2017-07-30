namespace TypescriptImportSync
{
    public class TSFileIOFactory : ITSFileFactory
    {
        public ITSFile Create(string path)
        {
            return new TSFileIO() { Path = path };
        }
    }
}