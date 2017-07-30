using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Ser = Newtonsoft.Json.JsonConvert;

namespace TypescriptImportSync
{
    public class TSObserver
    {
        private Configuration config;
        private List<TSObserverUnit> watchers;
        private object syncLock = new object();

        public TSObserver(Configuration config = null)
        {
            this.config = config ?? Configuration.Default;
        }

        public void Monitor(params string[] paths)
        {
            if (paths == null || paths.Length == 0)
            {
                throw new ArgumentException("At least one path is required.", nameof(paths));
            }

            lock(syncLock)
            {
                var newPaths = paths.Select(p => p.TrimEnd("\\".ToArray()));
                if (this.watchers == null)
                {
                    this.watchers = new List<TSObserverUnit>();
                }
                else
                {
                    var conflictingWatchers = this.watchers.Where(w => newPaths.Any(p => p.Contains(w.Path))).ToArray();
                    if (conflictingWatchers.Length > 0)
                    {
                        throw new ArgumentException(
                            String.Format(
                                "Paths or their parent directories are already being watched.{0}{0}Conflicting paths:{0}{1}",
                                Environment.NewLine,
                                String.Join(Environment.NewLine, conflictingWatchers.Select(w => w.Path))),
                            nameof(paths));
                    }
                }

                var newWatchers = newPaths.Select(p => new TSObserverUnit(p, config, this.config.FileWatcherFactory.Create())).ToList();
                this.watchers.AddRange(newWatchers);
            }
        }
        
        public void StopMonitoring(params string[] paths)
        {
            if (this.watchers == null)
            {
                throw new InvalidOperationException(String.Format("{0} must be called before {1}", nameof(Monitor), nameof(StopMonitoring)));
            }

            lock (syncLock)
            {
                var newPaths = paths.Select(p => p.TrimEnd("\\".ToArray()));
                var matchingWatchers = this.watchers.Where(w => newPaths.Any(p => p.Contains(w.Path))).ToArray();
                foreach (var watcher in matchingWatchers)
                {
                    watcher.Dispose();
                    this.watchers.Remove(watcher);
                }
            }
        }

        public void Dispose()
        {
            if (this.watchers != null)
            {
                foreach (var watcher in this.watchers)
                {
                    watcher.Dispose();
                }

                this.watchers = null;
            }
        }
    }
}