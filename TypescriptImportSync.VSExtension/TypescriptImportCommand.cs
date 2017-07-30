using System;
using System.ComponentModel.Design;
using System.Globalization;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System.IO;
using EnvDTE;
using System.Collections.Generic;
using System.Linq;
using TypescriptImportSync;

namespace TypescriptImportSync.VSExtension
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class TypescriptImportCommand : IDisposable
    {
        private const string CommandText_StopMonitoring = "Stop Monitoring Typescript";
        private const string CommandText_StartMonitoring = "Monitor Typescript";

        private List<string> watchedDirectories = new List<string>();
        private TSObserver observer;
        private string lastCommandText = CommandText_StartMonitoring;
        
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0100;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("30d2798d-e0da-4938-aefd-f2602a7e946a");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly Package package;

        /// <summary>
        /// Initializes a new instance of the <see cref="TypescriptImportCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        private TypescriptImportCommand(Package package)
        {
            this.package = package ?? throw new ArgumentNullException("package");

            if (this.ServiceProvider.GetService(typeof(IMenuCommandService)) is OleMenuCommandService commandService)
            {
                var menuCommandID = new CommandID(CommandSet, CommandId);
                var menuItem = new OleMenuCommand(this.MenuItemCallback, menuCommandID);
                menuItem.BeforeQueryStatus += OnBeforeQueryStatus;
                commandService.AddCommand(menuItem);
            }
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static TypescriptImportCommand Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private IServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static void Initialize(Package package)
        {
            Instance = new TypescriptImportCommand(package);
        }

        private static EnvDTE80.DTE2 GetDTE2()
        {
            return Package.GetGlobalService(typeof(DTE)) as EnvDTE80.DTE2;
        }

        private string GetSourceFilePath()
        {
            try
            {
                EnvDTE80.DTE2 _applicationObject = GetDTE2();
                UIHierarchy uih = _applicationObject.ToolWindows.SolutionExplorer;
                Array selectedItems = (Array)uih.SelectedItems;
                if (null != selectedItems)
                {
                    foreach (UIHierarchyItem selItem in selectedItems)
                    {
                        ProjectItem prjItem = selItem.Object as ProjectItem;
                        string filePath = prjItem.Properties.Item("FullPath").Value.ToString();
                        //System.Windows.Forms.MessageBox.Show(selItem.Name + filePath);
                        return filePath.TrimEnd("\\".ToArray());
                    }
                }
                return String.Empty;
            }
            catch
            {
                return String.Empty;
            }
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void MenuItemCallback(object sender, EventArgs e)
        {
            var selectedFolderPath = GetSourceFilePath();
            if (!String.IsNullOrEmpty(selectedFolderPath))
            {
                if (this.lastCommandText == CommandText_StopMonitoring)
                {
                    HandleStopMonitoring(selectedFolderPath);
                }
                else
                {
                    HandleStartMonitoring(selectedFolderPath);
                }
            }
        }

        private void HandleStopMonitoring(string selectedFolderPath)
        {
            if (this.observer == null)
            {
                return;
            }

            var matchingDirectory = GetWatchedPath(selectedFolderPath);
            if (matchingDirectory == null)
            {
                return;
            }

            this.watchedDirectories.Remove(matchingDirectory);
            this.observer.StopMonitoring(new[] { matchingDirectory });
        }

        private void HandleStartMonitoring(string selectedFolderPath)
        {
            var directoryInfo = new DirectoryInfo(selectedFolderPath);
            if (!directoryInfo.Exists)
            {
                ShowError("Directory does not exist.");
                return;
            }

            if (this.observer != null && GetWatchedPath(selectedFolderPath) != null)
            {
                ShowError("Directory is already being watched, or is a child directory of a directory that's being watched.");
                return;
            }

            if (this.observer == null)
            {
                var config = TypescriptImportSync.Configuration.Default;
                config.Logger = new VSConsoleLogger();
                this.observer = new TypescriptImportSync.TSObserver(config);
            }

            try
            {
                this.observer.Monitor(selectedFolderPath);
                this.watchedDirectories.Add(selectedFolderPath);
            }
            catch (Exception ex)
            {
                this.ShowError(ex.Message);
            }   
        }

        private void SetCommandText(OleMenuCommand command, string text)
        {
            command.Text = text;
            this.lastCommandText = text;
        }

        private string GetWatchedPath(string path)
        {
            return this.watchedDirectories.FirstOrDefault(p => path.Contains(p));
        }

        private void OnBeforeQueryStatus(object sender, EventArgs e)
        {
            var myCommand = sender as OleMenuCommand;
            if (myCommand != null)
            {
                if (this.observer != null)
                {
                    var selectedFolderPath = GetSourceFilePath();
                    if (!String.IsNullOrEmpty(selectedFolderPath))
                    {
                        var matchedObserver = GetWatchedPath(selectedFolderPath);
                        if (matchedObserver != null)
                        {
                            SetCommandText(myCommand, CommandText_StopMonitoring);
                            return;
                        }
                    }

                }
            }

            SetCommandText(myCommand, CommandText_StartMonitoring);
        }

        private void ShowError(string message, string title = "Angular Extensions")
        {
            VsShellUtilities.ShowMessageBox(
                this.ServiceProvider,
                message,
                title,
                OLEMSGICON.OLEMSGICON_CRITICAL,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }

        private void ShowInfo(string message, string title = "Angular Extensions")
        {
            VsShellUtilities.ShowMessageBox(
                this.ServiceProvider,
                message,
                title,
                OLEMSGICON.OLEMSGICON_INFO,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }

        public void Dispose()
        {
            if (this.observer != null)
            {
                this.observer.Dispose();
                this.observer = null;
                this.watchedDirectories = null;
            }
        }
    }
}
