using System;
using System.Collections.Generic;
using System.IO;

namespace TypescriptImportSync
{
    public class TSFileIO : TSFileBase
    {
        public override List<RelativeImport> RelativeImports { get; set; }

        public override string Path { get; set; }

        public override List<FileExport> Exports { get; set; }

        public override string Contents
        {
            get
            {
                var retries = 0;
                while (true)
                {
                    try
                    {
                        return File.ReadAllText(this.Path);
                    }
                    catch (Exception)
                    {
                        retries++;
                        if (retries == 10)
                        {
                            throw;
                        }

                        System.Threading.Thread.Sleep(5);
                    }
                }
            }
            set
            {
                System.IO.File.WriteAllText(this.Path, value ?? "");
            }
        }

        public override string ToString() => Path;
    }
}