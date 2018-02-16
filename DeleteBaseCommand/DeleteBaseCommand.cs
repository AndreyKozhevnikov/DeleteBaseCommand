//------------------------------------------------------------------------------
// <copyright file="DeleteBaseCommand.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.ComponentModel.Design;
using System.Globalization;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using EnvDTE;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace DeleteBaseCommand {
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class DeleteBaseCommand {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0100;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("b37b5a9e-2fa4-48c5-912f-161e379a610f");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly Package package;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteBaseCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        private DeleteBaseCommand(Package package) {
            if(package == null) {
                throw new ArgumentNullException("package");
            }

            this.package = package;

            OleMenuCommandService commandService = this.ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if(commandService != null) {
                var menuCommandID = new CommandID(CommandSet, CommandId);
                var menuItem = new MenuCommand(this.MenuItemCallback, menuCommandID);
                commandService.AddCommand(menuItem);
            }
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static DeleteBaseCommand Instance {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private IServiceProvider ServiceProvider {
            get {
                return this.package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static void Initialize(Package package) {
            Instance = new DeleteBaseCommand(package);
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        /// 
 

        private void MenuItemCallback(object sender, EventArgs e) {
            DTE dte = (DTE)ServiceProvider.GetService(typeof(DTE));
            var slnName = dte.Solution.FullName;
            var solutionFolderName = Path.GetDirectoryName(slnName);
            List<string> configFiles = new List<string>();
            configFiles.AddRange(Directory.GetFiles(solutionFolderName, "app.config", SearchOption.AllDirectories));
            configFiles.AddRange(Directory.GetFiles(solutionFolderName, "web.config", SearchOption.AllDirectories));
            var dbNamePattern = @"<add name=""ConnectionString"" connectionString=""Integrated Security=SSPI;Pooling=false;Data Source=\(localdb\)\\mssqllocaldb;Initial Catalog=(?<dbname>.*)""";
            foreach(var confFile in configFiles) {
                using(var sw = new StreamReader(confFile)) {
                    var configText = sw.ReadToEnd();
                    var dbNameRegex = new Regex(dbNamePattern);
                    Match dbNameMatch = dbNameRegex.Match(configText);
                    if(dbNameMatch.Success) {
                        var dbName = dbNameMatch.Groups["dbname"].Value;
                        DeleteDb(dbName);
                        VsShellUtilities.ShowMessageBox(this.ServiceProvider,
               string.Format("db {0} was deleted", dbName),
                      "Deleted",
               OLEMSGICON.OLEMSGICON_INFO,
               OLEMSGBUTTON.OLEMSGBUTTON_OK,
               OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                    }
                    return;
                }
            }

            VsShellUtilities.ShowMessageBox(this.ServiceProvider,
          
          "No database was found",
                   "Not found",
                   OLEMSGICON.OLEMSGICON_INFO,
                   OLEMSGBUTTON.OLEMSGBUTTON_OK,
                   OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }

        public void DeleteDb(string dbName) {
            DeleteBaseCommandPackage options = package as DeleteBaseCommandPackage;
            var deleteProcessPath = options.DeleteProgramFilePath;
            System.Diagnostics.Process proc = new System.Diagnostics.Process();
            proc.StartInfo.FileName = deleteProcessPath;
            proc.StartInfo.Arguments = "-" + dbName;
            proc.Start();
        }
    }
}
