//------------------------------------------------------------------------------
// <copyright file="TypescriptImportCommand.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio;

namespace TypescriptImportSync.VSExtension
{
    internal sealed class VSConsoleLogger : ILogger
    {
        private bool activatedOnce = false;

        public void Log(string message)
        {
            WriteToOutput(message);
        }

        private void WriteToOutput(string message)
        {
            var outWindow = Package.GetGlobalService(typeof(SVsOutputWindow)) as IVsOutputWindow;
            if (outWindow == null)
            {
                return;
            }

            var generalPaneGuid = VSConstants.GUID_OutWindowGeneralPane;    
            outWindow.GetPane(ref generalPaneGuid, out IVsOutputWindowPane generalPane);

            if (generalPane == null)
            {
                generalPaneGuid = VSConstants.GUID_OutWindowDebugPane;
                outWindow.GetPane(ref generalPaneGuid, out generalPane);
            }

            if (generalPane != null)
            {
                generalPane.OutputString(message + Environment.NewLine);

                if (!activatedOnce)
                {
                    activatedOnce = true;
                    generalPane.Activate();
                }            
            }
        }
    }
}
