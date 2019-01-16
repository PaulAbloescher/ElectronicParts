﻿// ***********************************************************************
// Assembly         : ElectronicParts.Services
// Author           : Kevin Janisch
// ***********************************************************************
// <copyright file="ExecutionService.cs" company="FHWN">
//     Copyright ©  2019
// </copyright>

// <summary>Represents the ExecutionService class of the ElectronicParts.Services programm</summary>
// ***********************************************************************
namespace ElectronicParts.Services.Implementations
{
    using ElectronicParts.Services.Interfaces;
    using Shared;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents the <see cref="ExecutionService"/> class of the ElectronicParts.Services application.
    /// Implements the <see cref="ElectronicParts.Services.Implementations.IExecutionService" />
    /// </summary>
    /// <seealso cref="ElectronicParts.Services.Implementations.IExecutionService" />
    public class ExecutionService : IExecutionService
    {
        /// <summary>
        /// Gets a value indicating whether this instance is enabled.
        /// </summary>
        /// <value>true if this instance is enabled; otherwise, false.</value>
        public bool IsEnabled { get; private set; }

        /// <summary>
        /// Starts the execution loop.
        /// </summary>
        /// <param name="nodes">The nodes to simulate.</param>
        /// <returns>Task.</returns>
        public async Task StartExecutionLoop(IEnumerable<INode> nodes)
        {
            if (!this.IsEnabled)
            {
                this.IsEnabled = true;

                while (this.IsEnabled)
                {
                    try
                    {
                        await this.ExecuteOnce(nodes);
                    }
                    catch (Exception e)
                    {
                        // TODO Proper Exception handeling
                        Debug.WriteLine(e.Message);
                    }
                }
            }
        }

        /// <summary>
        /// Stops the execution loop.
        /// </summary>
        public void StopExecutionLoop()
        {
            this.IsEnabled = false;
        }

        /// <summary>
        /// Executes one step.
        /// </summary>
        /// <param name="nodes">The nodes.</param>
        /// <returns>Task.</returns>
        public async Task ExecuteOnce(IEnumerable<INode> nodes)
        {
            await Task.Run(() =>
            {
                Parallel.ForEach(nodes, node =>
                {
                    node.Execute();
                });
            });
        }
    }
}
