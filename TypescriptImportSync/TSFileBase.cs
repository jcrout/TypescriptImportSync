using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace TypescriptImportSync
{
    public abstract class TSFileBase : ITSFile
    {
        public abstract string Contents { get; set; }
        public abstract string Path { get; set; }
        public abstract List<RelativeImport> RelativeImports { get; set; }
        public abstract List<FileExport> Exports { get; set; }

        public virtual void ScanContents()
        {
            var text = this.Contents;
            this.RelativeImports = this.GetImports(text);
            this.Exports = this.GetExports(text);
        }

        protected virtual List<RelativeImport> GetImports(string text)
        {
            const string importPattern1 = @"import.*?from\s*['|""]([\.|//].*?)['|""]";
            const string importPattern2 = @"import\s*['|""]([\.|\/].*?)['|""]";

            var matches = Regex.Matches(text, importPattern1).Cast<Match>()
                          .Concat(Regex.Matches(text, importPattern2).Cast<Match>())
                          .Where(m => m.Groups.Count == 2)
                          .Select(m => ProcessTSImport(m.Groups[1]))
                          .ToList();

             return matches;
        }

        protected virtual List<FileExport> GetExports(string text)
        {
            const string exportPattern = @"export\s*(class|interface|function|const)\s*(\w+)";
            
            var matches = Regex.Matches(text, exportPattern).Cast<Match>()
                          .Where(m => m.Groups.Count == 3)
                          .Select(m => new FileExport(m.Groups[2].Value, m.Groups[1].Value))
                          .ToList();

            return matches;
        }

        protected virtual RelativeImport ProcessTSImport(Group importMatch)
        {
            return new RelativeImport(
                importMatch.Value.EndsWith(".ts") ? importMatch.Value.Substring(0, importMatch.Value.Length - 3) : importMatch.Value,
                importMatch.Index,
                importMatch.Index + importMatch.Length);
        }
    }
}