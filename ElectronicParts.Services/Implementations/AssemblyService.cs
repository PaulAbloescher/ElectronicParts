﻿// ***********************************************************************
// Author           : Kevin Janisch
// ***********************************************************************
// <copyright file="AssemblyService.cs" company="FHWN">
//     Copyright (c) . All rights reserved.
// </copyright>

// <summary>Represents the AssemblyService class of the ElectronicParts Programm</summary>
// ***********************************************************************
namespace ElectronicParts.Services.Assemblies
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using Shared;

    /// <summary>
    /// Represents the <see cref="AssemblyService"/> class of the ElectronicParts application.
    /// Implements the <see cref="ElectronicParts.Services.Assemblies.IAssemblyService" />
    /// </summary>
    /// <seealso cref="ElectronicParts.Services.Assemblies.IAssemblyService" />
    public class AssemblyService : IAssemblyService
    {
        /// <summary>
        /// Represents the Assembly path.
        /// </summary>
        private readonly string assemblyPath;

        /// <summary>
        /// Initializes a new instance of the <see cref="AssemblyService"/> class.
        /// </summary>
        public AssemblyService()
        {
            // Generating the assembly path in the same folder as the exe.
            // C:\Programme\ElectronicParts\electronicParts.exe
            // C:\Programme\ElectronicParts\assemblies\????.dll
            this.assemblyPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "assemblies");
            Directory.CreateDirectory(this.assemblyPath);
        }

        /// <summary>
        /// Gets the available nodes.
        /// </summary>
        /// <value>The List of available lists.</value>
        public ImmutableList<IDisplayableNode> AvailableNodes { get; private set; }

        /// <summary>
        /// Loads the assemblies in the assembly paths.
        /// </summary>
        /// <returns>A Task for awaiting this operation.</returns>
        public async Task LoadAssemblies()
        {
            await Task.Run(() =>
            {
                // Creating temporary nodelist.
                List<IDisplayableNode> loadedNodes = new List<IDisplayableNode>();
                IEnumerable<FileInfo> files;
                try
                {
                    // Finding all files in the assembly directory with .dll extension.
                    files = Directory.GetFiles(this.assemblyPath).Select(path => new FileInfo(path))
                    .Where(file => file.Extension == ".dll");
                }
                catch (Exception e)
                {
                    // TODO proper exception-handeling
                    Debug.WriteLine($"{e.Message}");
                    return;
                }

                // Iterating over every dll-file and finding dlls with types that implement IDisplayableNode.
                foreach (var file in files)
                {
                    try
                    {
                        // Loading Assembly.
                        var assembly = Assembly.LoadFrom(file.FullName);

                        // Getting all Types that implement IDisplayableNode interface.
                        var types = assembly.GetTypes();
                        var availableNodes = types
                            .Where(type => type.GetInterfaces()
                            .Contains(typeof(IDisplayableNode)));

                        // Iterating over every type and adding an instance to the AvailableNodeslist.
                        foreach (var node in availableNodes)
                        {
                            loadedNodes.Add(Activator.CreateInstance(node) as IDisplayableNode);
                        }
                    }
                    catch (Exception e)
                    {
                        // TODO proper exception-handeling
                        Debug.WriteLine($"{e.Message}");
                    }
                }

                // writing loadedNodes into availableNodes immutableList.
                this.AvailableNodes = loadedNodes.ToImmutableList();
            });
        }
    }
}