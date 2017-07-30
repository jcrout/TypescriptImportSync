using System.Collections.Generic;

namespace TypescriptImportSync
{
    public class TSFileMemory : TSFileBase
    {
        private string _contents;

        public override List<RelativeImport> RelativeImports { get; set; }

        public override List<FileExport> Exports { get; set; }

        public override string Path { get; set; }

        public override string Contents
        {
            get
            {
                return _contents;
            }
            set
            {
                this._contents = value;
            }
        }

        public override string ToString() => Path;
    }
}