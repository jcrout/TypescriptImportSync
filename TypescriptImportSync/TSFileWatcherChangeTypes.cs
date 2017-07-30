using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TypescriptImportSync
{
    //
    // Summary:
    //     Changes that might occur to a file or directory.
    public enum TSFileWatcherChangeTypes
    {
        //
        // Summary:
        //     The creation of a file or folder.
        FileCreated = 1,
        //
        // Summary:
        //     The deletion of a file or folder.
        FileDeleted = 2,
        //
        // Summary:
        //     The change of a file or folder. The types of changes include: changes to size,
        //     attributes, security settings, last write, and last access time.
        FileChanged = 4,
        //
        // Summary:
        //     The renaming of a file or folder.
        FileRenamed = 8,
        //
        // Summary:
        //     The renaming of a directory
        DirectoryRenamed = 16,
        //
        // Summary:
        //     The deletion of a directory
        DirectoryDeleted = 32,
        //
        // Summary:
        //     The creation of a directory
        DirectoryCreated = 64
    }
}
