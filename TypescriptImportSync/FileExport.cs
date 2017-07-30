namespace TypescriptImportSync
{
    public struct FileExport
    {
        public string Type { get; }

        public string Name { get; }

        public FileExport(string name, string type)
        {
            this.Name = name;
            this.Type = type;
        }

        public override string ToString() => Type + " " + Name;
    }
}