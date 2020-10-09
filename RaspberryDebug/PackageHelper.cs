﻿//-----------------------------------------------------------------------------
// FILE:	    PackageHelper.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:   Open Source
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Microsoft.VisualStudio.Shell;

using Neon.Common;
using Newtonsoft.Json;

using Task = System.Threading.Tasks.Task;
using System.Diagnostics.Contracts;

namespace RaspberryDebug
{
    /// <summary>
    /// Package specific constants.
    /// </summary>
    internal static class PackageHelper
    {
        /// <summary>
        /// The path to the <b>%USERPROFILE%\.pi-debug</b> folder where the package
        /// will persist its settings and other files.
        /// </summary>
        public static readonly string SettingsFolder;

        /// <summary>
        /// The path to the folder holding the Raspberry SSH private keys.
        /// </summary>
        public static readonly string KeysFolder;

        /// <summary>
        /// The path to the JSON file defining the Raspberry Pi connections.
        /// </summary>
        public static readonly string ConnectionsPath;

        /// <summary>
        /// Directory on the Raspberry Pi where .NET Core SDKs will be installed along with the
        /// <b>vsdbg</b> remote debugger.
        /// </summary>
        public const string RemoteDotnetRootPath = "/lib/dotnet";

        /// <summary>
        /// Directory on the Raspberry Pi where the <b>vsdbg</b> remote debugger will be installed.
        /// </summary>
        public const string RemoteDebugRoot = RemoteDotnetRootPath + "/vsdbg";

        /// <summary>
        /// Returns information about the known .NET Core SDKs,
        /// </summary>
        public static SdkCatalog SdkCatalog { get; private set; }

        /// <summary>
        /// Static constructor.
        /// </summary>
        static PackageHelper()
        {
            // Initialize the settings path and folders.

            SettingsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".pi-debug");

            if (!Directory.Exists(SettingsFolder))
            {
                Directory.CreateDirectory(SettingsFolder);
            }

            KeysFolder = Path.Combine(SettingsFolder, "keys");

            if (!Directory.Exists(KeysFolder))
            {
                Directory.CreateDirectory(KeysFolder);
            }

            ConnectionsPath = Path.Combine(SettingsFolder, "connections.json");

            // Parse the embedded SDK catalog JSON.

            var assembly = Assembly.GetExecutingAssembly();

            using (var catalogStream = assembly.GetManifestResourceStream("RaspberryDebug.sdk-catalog.json"))
            {
                var catalogJson = Encoding.UTF8.GetString(catalogStream.ReadToEnd());

                SdkCatalog = NeonHelper.JsonDeserialize<SdkCatalog>(catalogJson);
            }
        }

        /// <summary>
        /// Reads the persisted connection settings.
        /// </summary>
        /// <returns>The connections.</returns>
        public static List<ConnectionInfo> ReadConnections()
        {
            Log.WriteLine("Reading connections");

            try
            {
                if (!File.Exists(ConnectionsPath))
                {
                    return new List<ConnectionInfo>();
                }

                var list = NeonHelper.JsonDeserialize<List<ConnectionInfo>>(File.ReadAllText(ConnectionsPath));

                return list ?? new List<ConnectionInfo>();
            }
            catch (Exception e)
            {
                Log.Exception(e);
                throw;
            }
        }

        /// <summary>
        /// Persists the connections passed.
        /// </summary>
        /// <param name="connections">The connections.</param>
        public static void WriteConnections(List<ConnectionInfo> connections)
        {
            Log.WriteLine("Writing connections");

            try
            {
                connections = connections ?? new List<ConnectionInfo>();

                File.WriteAllText(ConnectionsPath, NeonHelper.JsonSerialize(connections, Formatting.Indented));
            }
            catch (Exception e)
            {
                Log.Exception(e);
                throw;
            }
        }

        /// <summary>
        /// Runs an action on the UI thread.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <returns>The tracking <see cref="Task"/>.</returns>
        public static async Task RunOnUIThreadAsync(Action action)
        {
            Covenant.Requires<ArgumentNullException>(action != null, nameof(action));

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(alwaysYield: true);
            action();
        }

        /// <summary>
        /// Runs an action that returns a value on the UI thread.
        /// </summary>
        /// <typeparam name="T">The result type.</typeparam>
        /// <param name="action">The action.</param>
        /// <returns>The tracking <see cref="Task"/>.</returns>
        public static async Task<T> RunOnUIThreadAsync<T>(Func<T> action)
        {
            Covenant.Requires<ArgumentNullException>(action != null, nameof(action));

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(alwaysYield: true);
            return action();
        }
    }
}
