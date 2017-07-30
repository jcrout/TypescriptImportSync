using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace TypescriptImportSync
{
    internal class TSObserverUnit : IDisposable
    {
        private static readonly char[] filePathSplit = new[] { '\\' };

        public event EventHandler BatchUpdateBegin;
        public event EventHandler BatchUpdateEnd;
        public string Path { get; }

        private ConcurrentQueue<FileSystemChangedArgs> internalChangeQueue = new ConcurrentQueue<FileSystemChangedArgs>();
        private BlockingCollection<FileSystemChangedArgs> changeQueue = new BlockingCollection<FileSystemChangedArgs>();
        private List<FileSystemChangedArgs> batchQueue = new List<FileSystemChangedArgs>();
        private Dictionary<string, ITSFile> files = new Dictionary<string, ITSFile>();

        private List<ITSFile> selfChangedFiles = new List<ITSFile>();
        private ITSFileFactory tsFileFactory;
        private IFileWatcher watcher;
        private IFileContentManager fileContentManager;
        private ILogger logger;
        private Task monitorTask;
        private CancellationTokenSource cancelToken;
        private int MinimumBatchDelay = 1000;
        private bool batchUpdateStart = false;
        private volatile bool disposed = false;
        private volatile bool initialized = false;

        internal TSObserverUnit(string path, Configuration config, IFileWatcher watcher)
        {
            this.Path = path;
            this.internalChangeQueue = new ConcurrentQueue<FileSystemChangedArgs>();
            this.changeQueue = new BlockingCollection<FileSystemChangedArgs>(internalChangeQueue);

            this.tsFileFactory = config.TsFileFactory;
            this.watcher = watcher;
            this.logger = config.Logger;
            this.fileContentManager = config.FileContentManager;
            this.MinimumBatchDelay = config.BatchDelay;
            this.cancelToken = new CancellationTokenSource();
            this.watcher.FileSystemChanged += Watcher_FileChanged;
            this.watcher.WatchDirectory(path);
            this.monitorTask = Task.Run(() => MonitorChanges(), cancelToken.Token);

            BuildFileList(this.fileContentManager.GetFileNode(path));

            lock (this.changeQueue)
            {
                this.initialized = true;
            }

            this.Log("Watching directory " + path);
        }

        private void Watcher_FileChanged(object sender, FileSystemChangedArgs e)
        {
            this.changeQueue.Add(e);
        }

        private void MonitorChanges()
        {
            DateTime lastMessage = default(DateTime);
            try
            {
                var token = this.cancelToken.Token;
                while (!disposed)
                {
                    var fileChange = this.changeQueue.Take(token);

                    //Console.WriteLine("a");
                    if (!this.initialized)
                    {
                        lock (this.changeQueue)
                        {
                            while (!this.initialized)
                            {
                                System.Threading.Thread.Sleep(1);
                            }
                        }
                    }

                    lastMessage = DateTime.Now;
                    lock (batchQueue)
                    {
                        batchQueue.Add(fileChange);
                    }

                    Task.Run(async () =>
                    {
                        await Task.Delay(this.MinimumBatchDelay);

                        var timeChange = DateTime.Now.Subtract(lastMessage).TotalMilliseconds;
                        if (timeChange < this.MinimumBatchDelay - 5)
                        {
                            // another file has been added within the mininum batch delay time frame, cancel processing and wait for next one
                            return;
                        }
                        else
                        {
                            lock (batchQueue)
                            {
                                if (batchQueue.Count > 0)
                                {
                                    try
                                    {
                                        this.ProcessBatchChanges(batchQueue);
                                    }
                                    finally
                                    {
                                        batchQueue.Clear();
                                    }
                                }
                            }
                        }
                    });
                }
            }
            catch
            {
            }
        }

        private void ProcessBatchChanges(List<FileSystemChangedArgs> changes)
        {
            lock (this.files)
            {
                // get rid of FileSystemWatcher issue where some events trigger twice
                var uniqueChanges = changes.GroupBy(c => new { c.ChangeType, c.NewName, c.OldName }).Select(c => new FileSystemChangedArgs(c.Key.ChangeType, c.Key.OldName, c.Key.NewName));
                var filesToUpdate = new List<ITSFile>();
                var directoriesCreated = new List<string>();
                foreach (var fileChange in uniqueChanges)
                {
                    switch (fileChange.ChangeType)
                    {
                        case TSFileWatcherChangeTypes.FileChanged:

                            var existingFile = this.files.FirstOrDefault(f => f.Key == fileChange.NewName);
                            if (existingFile.Value != null)
                            {
                                if (this.selfChangedFiles.Contains(existingFile.Value))
                                {
                                    this.selfChangedFiles.Remove(existingFile.Value);
                                }
                                else if (!filesToUpdate.Contains(existingFile.Value))
                                {
                                    existingFile.Value.ScanContents();
                                    filesToUpdate.Add(existingFile.Value);
                                }
                            }
                            break;

                        case TSFileWatcherChangeTypes.FileDeleted:

                            try
                            {
                                this.files.Remove(fileChange.NewName);
                            }
                            catch { } // ignore cases where the file wasn't actually found, not important                        
                            break;
                        case TSFileWatcherChangeTypes.FileCreated:

                            var file = this.tsFileFactory.Create(fileChange.NewName);
                            file.ScanContents();
                            filesToUpdate.Add(file);

                            var lastSlashIndex = file.Path.LastIndexOf('\\');
                            var fName = file.Path.Substring(lastSlashIndex);
                            var match = this.files.Keys.FirstOrDefault(k => k.EndsWith(fName));
                            if (match != null)
                            {
                                if (!this.fileContentManager.FileExists(files[match].Path))
                                {
                                    this.files.Remove(match);
                                }
                            }

                            if (this.files.ContainsKey(fileChange.NewName))
                            {
                                this.files[fileChange.NewName] = file;
                            }
                            else
                            {
                                this.files.Add(fileChange.NewName, file);
                            }
                            break;
                        case TSFileWatcherChangeTypes.FileRenamed:

                            if (!String.IsNullOrEmpty(fileChange.OldName) && fileChange.OldName.EndsWith(".ts") && this.files.ContainsKey(fileChange.OldName))
                            {
                                this.files.Remove(fileChange.OldName);
                            }

                            var newFile = this.tsFileFactory.Create(fileChange.NewName);
                            newFile.ScanContents();
                            filesToUpdate.Add(newFile);

                            this.files.Add(fileChange.NewName, newFile);
                            break;
                        case TSFileWatcherChangeTypes.DirectoryRenamed:

                            var affectedFiles = this.files.Values.Where(f => f.Path.StartsWith(fileChange.OldName)).ToArray();
                            foreach (var affectedFile in affectedFiles)
                            {
                                var newPath = fileChange.NewName + affectedFile.Path.Substring(fileChange.OldName.Length);
                                this.files.Remove(affectedFile.Path);
                                this.files.Add(newPath, affectedFile);

                                affectedFile.Path = newPath;
                                filesToUpdate.Add(affectedFile);
                            }
                            break;
                        case TSFileWatcherChangeTypes.DirectoryDeleted:

                            var affectedFilesFromDelete = this.files.Values.Where(f => f.Path.StartsWith(fileChange.NewName)).ToArray();
                            foreach (var affectedFile in affectedFilesFromDelete)
                            {
                                this.files.Remove(affectedFile.Path);
                            }
                            break;
                        case TSFileWatcherChangeTypes.DirectoryCreated:
                            directoriesCreated.Add(fileChange.NewName);
                            break;
                    }
                }

                if (directoriesCreated.Count > 0)
                {
                    var tempDeletedFiles = new List<ITSFile>();
                    foreach (var createdDirectory in directoriesCreated)
                    {
                        var segments = createdDirectory.Split(filePathSplit);
                        var directoryName = '\\' + segments.Last();
                        var potentiallyAffectedFiles = this.files.Where(f => f.Value.Path.Contains(directoryName));

                        foreach (var potentiallyAffectedFile in potentiallyAffectedFiles)
                        {
                            var fileNode = this.fileContentManager.GetFileNode(potentiallyAffectedFile.Key);
                            if (!fileNode.Exists)
                            {
                                tempDeletedFiles.Add(potentiallyAffectedFile.Value);
                            }
                        }

                        var directoryNode = this.fileContentManager.GetFileNode(createdDirectory);
                        for (var i = tempDeletedFiles.Count - 1; i >= 0; i--)
                        {
                            var tempDeletedFile = tempDeletedFiles[i];
                            var potentialNewNode = this.GetNewFileNode(tempDeletedFile.Path, createdDirectory, directoryName);
                            if (potentialNewNode != null)
                            {
                                var oldPath = tempDeletedFile.Path;
                                try
                                {
                                    files.Add(potentialNewNode.Path, tempDeletedFile);
                                    tempDeletedFile.Path = potentialNewNode.Path;
                                    filesToUpdate.Add(tempDeletedFile);

                                    tempDeletedFiles.RemoveAt(i);
                                    files.Remove(oldPath);
                                }
                                catch { } // ignore if this file has already been updated from another                         
                            }                        
                        }
                    }

                    foreach (var deletedFileNotFoundElsewhere in tempDeletedFiles)
                    {
                        files.Remove(deletedFileNotFoundElsewhere.Path);
                    }
                }

                if (filesToUpdate.Count > 0)
                {
                    this.Log("Processing {0} changes in batch.", filesToUpdate.Count);

                    batchUpdateStart = false;
                    foreach (var file in filesToUpdate)
                    {
                        ProcessChanges(file);
                    }

                    this.RaiseBatchUpdateEnd();
                }
            }
        }

        private IFileSystemNode GetNewFileNode(string path, string newPath, string directoryPath)
        {
            var pIndex = 0;
            while (true)
            {
                var idx = path.IndexOf(directoryPath, pIndex);
                if (idx == -1)
                {
                    return null;
                }
                else
                {
                    var endIndex = idx + directoryPath.Length;
                    var newPart = path.Substring(endIndex);
                    var aNewPath = newPath + newPart;
                    var node = this.fileContentManager.GetFileNode(aNewPath);

                    if (node.Exists && !this.files.ContainsKey(aNewPath))
                    {
                        return node;
                    }

                    pIndex = endIndex;
                }
            }
        }

        private void RaiseBatchUpdateBegin()
        {
            if (this.batchUpdateStart)
            {
                return;
            }

            this.batchUpdateStart = true;
            var ev = this.BatchUpdateBegin;
            //Console.WriteLine("abc begin");
            if (ev != null)
            {
                ev.Invoke(this, EventArgs.Empty);
            }
        }

        private void RaiseBatchUpdateEnd()
        {
            if (!this.batchUpdateStart)
            {
                return;
            }

            var ev = this.BatchUpdateEnd;
            //Console.WriteLine("abc end");
            if (ev != null)
            {
                ev.Invoke(this, EventArgs.Empty);
            }
        }

        private IEnumerable<IncompleteImport> GetIncompleteImports(string contents)
        {
            const string incompleteImportPattern = @"import\s*{\s*(.*?)}\s*(;|\r\n?|\n|$)";
            var matches = Regex
                .Matches(contents, incompleteImportPattern)
                .Cast<Match>()
                .Where(m => m.Groups.Count == 3)
                .Select(m => new IncompleteImport(
                    m.Groups[1].Value.Split(',').Select(s => s.Trim()).ToArray(),
                    m.Groups[2].Value,
                    m.Index,
                    m.Index + m.Length));

            return matches;

        }

        private void ProcessChanges(ITSFile file)
        {
            var lastSlashIndex = file.Path.LastIndexOf('\\');               // .....\\AspNetCoreSpa\\Client\\app\\core\\shared\\shared.module.ts
            var fileName = file.Path.Substring(lastSlashIndex + 1);         // shared.module.ts
            var fileBaseName = fileName.Substring(0, fileName.Length - 3);  // shared.module

            UpdateFilesOwnContents(file);

            UpdateFilesReferencingThis(file, fileBaseName);
        }

        private void RegisterFileChanged(ITSFile file)
        {
            this.selfChangedFiles.Add(file);
        }

        private void UpdateFilesOwnContents(ITSFile file)
        {
            const string RelativeStartPattern = @"^[\.|\/]*";

            var fileContents = file.Contents;
            var newFileOutput = "";
            var lastSlashIndex = 0;

            // update file's own relative imports
            if (file.RelativeImports.Any())
            {
                var builder = new StringBuilder();
                var index = 0;
                foreach (var import in file.RelativeImports)
                {
                    var importEnding = Regex.Replace(import.Name, RelativeStartPattern, "").Replace('/', '\\') + ".ts";
                    var existingMatches = this.files.Where(f => f.Key.EndsWith(importEnding)).ToList();
                    KeyValuePair<string, ITSFile> existingMatch = default(KeyValuePair<string, ITSFile>);
                    for (var i = existingMatches.Count - 1; i >= 0; i--)
                    {
                        if (!this.fileContentManager.FileExists(existingMatches[i].Value.Path))
                        {
                            this.files.Remove(existingMatches[i].Key);
                            existingMatches.RemoveAt(i);
                        }
                        else
                        {
                            existingMatch = existingMatches[i];
                        }
                    }

                    if (existingMatches.Count > 1)
                    {
                        var latestFile = existingMatches.Select(e => this.fileContentManager.GetFileNode(e.Key)).Where(e => e.Exists).OrderByDescending(e => e.CreationTime).First();
                        existingMatch = existingMatches.First(m => m.Key == latestFile.Path);
                    }
                    else if (existingMatches.Count == 0)
                    {
                        lastSlashIndex = importEnding.LastIndexOf('\\');
                        if (lastSlashIndex != -1)
                        {
                            var searchSegment = importEnding.Substring(lastSlashIndex);
                            existingMatches = this.files.Where(f => f.Key.EndsWith(searchSegment)).ToList();
                            if (existingMatches.Count == 1)
                            {
                                existingMatch = existingMatches.First();
                            }
                        }
                    }

                    if (existingMatches.Count == 0)
                    {
                        builder.Append(fileContents.Substring(index, import.StartIndex - index));
                        builder.Append(import.Name);
                        index = import.EndIndex;
                    }
                    else
                    {
                        var relPath = existingMatch.Value != null ?
                            GetRelativePathTo(file.Path, existingMatch.Value.Path) : import.Name;
                        if (relPath != import.Name && relPath.Length == import.Name.Length - 2 && relPath.Substring(0, 2) == ".." && import.Name.Substring(0, 2) == "./")
                        {
                            // if the original case had ./../ instead of just ../, allow it - amounts to same thing
                            relPath = import.Name;
                        }

                        builder.Append(fileContents.Substring(index, import.StartIndex - index));
                        builder.Append(relPath);
                    }

                    index = import.EndIndex;
                }

                builder.Append(fileContents.Substring(index));
                newFileOutput = builder.ToString();
            }
            else
            {
                newFileOutput = fileContents;
            }

            newFileOutput = UpdateIncompleteImports(file, newFileOutput);

            // if the produced output is different, save the file
            if (!String.IsNullOrEmpty(newFileOutput) && !String.Equals(fileContents, newFileOutput))
            {
                this.Log("Updating " + file.Path);
                this.RaiseBatchUpdateBegin();
                this.RegisterFileChanged(file);
                file.Contents = newFileOutput;
                file.ScanContents();
            }
        }

        protected virtual string UpdateIncompleteImports(ITSFile file, string newFileOutput)
        {
            var incompleteImports = this.GetIncompleteImports(newFileOutput);
            if (!incompleteImports.Any())
            {
                return newFileOutput;
            }

            var builder = new StringBuilder();
            var index = 0;
            foreach (var incompleteImport in incompleteImports)
            {
                var newText = "";
                var incompleteItemNames = incompleteImport.Imports;
                var fileContainingImports = this.files
                    .Values
                    .Where(f =>
                        f.Exports.
                        Select(e => e.Name)
                        .Intersect(incompleteItemNames)
                        .ToArray()
                        .Length == incompleteItemNames.Length)
                    .ToArray();

                if (fileContainingImports.Length == 1)
                {
                    var relativePath = this.GetRelativePathTo(file.Path, fileContainingImports[0].Path);
                    newText = String.Format("import {{ {0} }} from '{1}'{2}",
                        String.Join(", ", incompleteItemNames),
                        relativePath,
                        incompleteImport.EndText);
                }
                else
                {
                    newText = newFileOutput.Substring(incompleteImport.StartIndex, incompleteImport.EndIndex - incompleteImport.StartIndex);
                }

                builder.Append(newFileOutput.Substring(index, incompleteImport.StartIndex - index));
                builder.Append(newText);

                index = incompleteImport.EndIndex;
            }

            builder.Append(newFileOutput.Substring(index));

            return builder.ToString();
        }

        protected virtual void UpdateFilesReferencingThis(ITSFile file, string fileBaseName)
        {
            //update other files' relative imports of this file
            var fileBaseNameSearch = "/" + fileBaseName;
            var affectedImportFiles = this.files.Where(f => f.Value.RelativeImports.Any(i => i.Name.EndsWith(fileBaseNameSearch) || i.Name == fileBaseName)).ToArray();
            if (affectedImportFiles.Length > 0)
            {
                foreach (var affectedImportFile in affectedImportFiles)
                {
                    if (!this.fileContentManager.FileExists(affectedImportFile.Key))
                    {
                        this.files.Remove(affectedImportFile.Key);
                        continue;
                    }

                    var relPath = GetRelativePathTo(affectedImportFile.Key, file.Path);
                    var affectedImports = affectedImportFile.Value.RelativeImports.Where(i => i.Name.EndsWith(fileBaseNameSearch) || i.Name == fileBaseName);
                    var txt = affectedImportFile.Value.Contents;
                    var builder = new StringBuilder();
                    var index = 0;
                    foreach (var affectedImport in affectedImports)
                    {
                        builder.Append(txt.Substring(index, affectedImport.StartIndex - index));
                        builder.Append(relPath);
                        index = affectedImport.EndIndex;
                    }

                    builder.Append(txt.Substring(index));
                    var output = builder.ToString();

                    // if the produced output is different, save the file
                    if (!String.Equals(txt, output))
                    {
                        this.Log("Updating " + affectedImportFile.Key);
                        this.RaiseBatchUpdateBegin();
                        this.RegisterFileChanged(file);
                        affectedImportFile.Value.Contents = output;
                        affectedImportFile.Value.ScanContents();
                    }
                }
            }
        }

        private void Log(string format, params object[] args)
        {
            this.Log(String.Format(format, args));
        }

        private void Log(string message)
        {
            if (this.logger != null)
            {
                this.logger.Log("TSObserver (" + DateTime.Now.ToString("h:mm tt") + "): " + message);
            }
        }

        private string GetRelativePathTo(string from, string to)
        {
            var fileInfoFrom = this.fileContentManager.GetFileNode(from);
            var fileInfoTo = this.fileContentManager.GetFileNode(to);

            var fileSegmentsFrom = GetPathSegments(fileInfoFrom);
            var fileSegmentsTo = GetPathSegments(fileInfoTo);

            var builder = new StringBuilder();
            for (var i = fileSegmentsFrom.Count - 1; i >= 0; i--)
            {
                var matchIndex = IsSegmentMatch(fileSegmentsTo, fileSegmentsFrom, i);
                if (matchIndex == -1)
                {
                    builder.Append("../");
                }
                else
                {
                    if (i == fileSegmentsFrom.Count - 1)
                    {
                        // located in same directory
                        builder.Append("./");
                    }


                    var partBase = String.Join("/", fileSegmentsTo.Skip(matchIndex + 1));
                    if (fileSegmentsFrom.SequenceEqual(fileSegmentsTo))
                    {
                        partBase = null;
                    }

                    var fName = fileInfoTo.Path.Substring(fileInfoTo.Path.LastIndexOf('\\') + 1);
                    var partName = (!String.IsNullOrEmpty(partBase) ? partBase + "/" : "") + fName.Substring(0, fName.Length - 3);
                    builder.Append(partName);
                    break;
                }
            }

            var output = builder.ToString();
            return output;
        }

        private int IsSegmentMatch(List<string> segmentsTo, List<string> segmentsFrom, int startIndex)
        {
            var startSegment = segmentsFrom[startIndex];
            for (var i = segmentsTo.Count - 1; i >= 0; i--)
            {
                if (segmentsTo[i] == startSegment && i < segmentsFrom.Count)
                {
                    if (segmentsTo.Take(i).SequenceEqual(segmentsFrom.Take(i)))
                    {
                        return i;
                    }
                }
            }

            return -1;
        }

        private List<string> GetPathSegments(IFileSystemNode file)
        {
            var segments = file.Path.Split(filePathSplit);
            return segments.Take(segments.Length - 1).ToList();
        }

        private void BuildFileList(IFileSystemNode sDir)
        {
            try
            {
                foreach (var f in sDir.GetFiles("*.ts"))
                {
                    var tsFile = this.tsFileFactory.Create(f.Path);
                    tsFile.ScanContents();
                    this.files.Add(f.Path, tsFile);
                }

                foreach (var d in sDir.GetDirectories())
                {
                    BuildFileList(d);
                }
            }
            catch (System.Exception excpt)
            {
                Console.WriteLine(excpt.Message);
            }
        }

        public void Dispose()
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
            this.cancelToken.Cancel();
            this.watcher.Dispose();
            this.changeQueue.Dispose();
            this.files = null;
            this.monitorTask = null;

            this.Log("Terminating directory watch for directory " + Path);
        }
    }
}