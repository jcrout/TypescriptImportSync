namespace TypescriptImportSync
{
    public struct IncompleteImport
    {
        public string[] Imports { get; }

        public string EndText { get; }

        public int StartIndex { get; }

        public int EndIndex { get; }

        public IncompleteImport(string[] imports, string endText, int startIndex, int endIndex)
        {
            this.Imports = imports;
            this.EndText = endText;
            this.StartIndex = startIndex;
            this.EndIndex = endIndex;
        }

        public override string ToString() => string.Join(", ", Imports);
    }

    public struct RelativeImport
    {
        public string Name { get; }

        public int StartIndex { get; }

        public int EndIndex { get; }

        public RelativeImport(string name, int startIndex, int endIndex)
        {
            this.Name = name;
            this.StartIndex = startIndex;
            this.EndIndex = endIndex;
        }

        public override string ToString() => Name;
    }
}