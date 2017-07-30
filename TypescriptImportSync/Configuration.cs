using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TypescriptImportSync
{
    public class Configuration
    {
        private static IFileContentManager defaultFileContentManager = new FileContentManager();
        private static IFileWatcherFactory defaultFileWatcherFactory = new FileWatcherFactory();
        private static ITSFileFactory defaultTsFileFactory = new TSFileIOFactory();
        private static ILogger defaultLogger = new ConsoleLogger();

        public IFileContentManager FileContentManager { get; set; }
        public IFileWatcherFactory FileWatcherFactory { get; set; }
        public ITSFileFactory TsFileFactory { get; set; }
        public ILogger Logger { get; set; }
        public int BatchDelay { get; set; } = 1000;

        public static Configuration Default
        {
            get
            {
                return GetDefaultConfiguration();
            }
        }

        private static Configuration GetDefaultConfiguration()
        {
            var config = new Configuration
            {
                FileContentManager = defaultFileContentManager,
                FileWatcherFactory = defaultFileWatcherFactory,
                TsFileFactory = defaultTsFileFactory,
                Logger = defaultLogger,
                BatchDelay = 1000
            };

            return config;
        }
    }
}
