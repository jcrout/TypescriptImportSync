using System.Collections.Generic;

namespace TypescriptImportSync
{
    public interface ITSFile
    {
        string Contents { get; set; }
        string Path { get; set; }
        List<RelativeImport> RelativeImports { get; set; }
        List<FileExport> Exports { get; set; }

        void ScanContents();
    }
}