﻿// ***********************************************************************
// Assembly         : ElectronicParts.Services
// Author           : Kevin Janisch
// ***********************************************************************
// <copyright file="NodeCopyService.cs" company="FHWN">
//     Copyright ©  2019
// </copyright>
// <summary>Represents the NodeCopyService class of the ElectronicParts.Services project</summary>
// ***********************************************************************
namespace ElectronicParts.Services.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using ElectronicParts.Models;
    using ElectronicParts.Services.Extensions;
    using ElectronicParts.Services.Interfaces;
    using Shared;

    /// <summary>
    /// Represents the <see cref="NodeCopyService"/> class of the ElectronicParts.Services application.
    /// Implements the <see cref="ElectronicParts.Services.Interfaces.INodeCopyService" />
    /// </summary>
    /// <seealso cref="ElectronicParts.Services.Interfaces.INodeCopyService" />
    public class NodeCopyService : INodeCopyService
    {
        /// <summary>
        /// Represents the pin connector service.
        /// </summary>
        private readonly IPinConnectorService connectorService;
        private readonly IPinCreatorService pinCreatorService;

        /// <summary>
        /// Represents the connectors which are to be copied.
        /// </summary>
        private IEnumerable<Connector> connectorsToCopy;

        /// <summary>
        /// Represents the nodes which are to be copied.
        /// </summary>
        private IEnumerable<IDisplayableNode> nodesToCopy;

        /// <summary>
        /// Represents the connectors which were copied last.
        /// </summary>
        private ICollection<Connector> copiedConnectors;

        /// <summary>
        /// Represents the nodes which were copied last.
        /// </summary>
        private ICollection<IDisplayableNode> copiedNodes;

        /// <summary>
        /// Represents the currently running copy task.
        /// </summary>
        private Task copyTask;

        /// <summary>
        /// Initializes a new instance of the <see cref="NodeCopyService"/> class.
        /// </summary>
        /// <param name="connectorService">The connector service.</param>
        /// <exception cref="ArgumentNullException">Gets throws if the injected <see cref="PinConnectorService"/> is null.</exception>
        public NodeCopyService(IPinConnectorService connectorService, IPinCreatorService pinCreatorService)
        {
            this.copiedConnectors = new List<Connector>();
            this.copiedNodes = new List<IDisplayableNode>();
            this.connectorService = connectorService ?? throw new ArgumentNullException(nameof(connectorService));
            this.pinCreatorService = pinCreatorService ?? throw new ArgumentNullException(nameof(pinCreatorService));
            this.nodesToCopy = Enumerable.Empty<IDisplayableNode>();
            this.connectorsToCopy = Enumerable.Empty<Connector>();
        }
        
        /// <summary>
        /// Gets the copied connectors.
        /// </summary>
        /// <value>The copied connectors.</value>
        public ICollection<Connector> CopiedConnectors
        {
            get
            {
                if (!this.copyTask?.IsCompleted == true)
                {
                    return new List<Connector>();
                }

                return this.copiedConnectors;
            }
        }

        /// <summary>
        /// Gets the copied nodes.
        /// </summary>
        /// <value>The copied nodes.</value>
        public ICollection<IDisplayableNode> CopiedNodes
        {
            get
            {
                if (!this.copyTask?.IsCompleted == true)
                {
                    return new List<IDisplayableNode>();
                }

                return this.copiedNodes;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the service has been initialized or not.
        /// </summary>
        /// <value>A value indicating whether the service has been initialized or not.</value>
        public bool IsInitialized
        {
            get; private set;
        }

        /// <summary>
        /// This asynchronous method returns a Task which can be used to await the currently running copy process.
        /// </summary>
        /// <returns>Returns a task used for waiting for the copy process to finish without exposing the actual task.</returns>
        public async Task CopyTaskAwaiter()
        {
            if (!this.copyTask.IsCompleted && !(this.copyTask is null))
            {
                await this.copyTask;
            }
        }

        /// <summary>
        /// Initializes the copy process. Call this method when the user requested the copy process for example by pressing STRG-C
        /// This method will store the nodes and connector and start creating a copy of the elements.
        /// </summary>
        /// <param name="nodes">The nodes.</param>
        /// <param name="connectors">The connectors.</param>
        public void InitializeCopyProcess(IEnumerable<IDisplayableNode> nodes, IEnumerable<Connector> connectors)
        {
            this.CopiedNodes.Clear();
            this.CopiedConnectors.Clear();
            this.nodesToCopy = Enumerable.Empty<IDisplayableNode>();
            this.connectorsToCopy = Enumerable.Empty<Connector>();
            this.nodesToCopy = nodes?.ToList() ?? throw new ArgumentNullException(nameof(nodes));
            this.connectorsToCopy = connectors?.ToList() ?? throw new ArgumentNullException(nameof(connectors));
            this.copyTask = this.MakeCopyAsync();
            this.IsInitialized = true;
        }

        /// <summary>
        /// This Method tries to start a new CopyProcess.
        /// </summary>
        /// <returns>true if there is no copyProcess running at the moment and a new one has been successfully created, false otherwise.</returns>
        public bool TryBeginCopyTask()
        {
            if (this.copyTask?.IsCompleted == true)
            {
                this.copyTask = this.MakeCopyAsync();
                return true;
            }

            return false;
        }

        /// <summary>
        /// This is a private task which contains the logic to create a full deep copy of all nodes supplied during initialization phase.
        /// This task will also make all the necessary connection via the <see cref="PinConnectorService"/>.
        /// </summary>
        /// <returns>An Task used to wait for the copy process to finish.</returns>
        private async Task MakeCopyAsync()
        {
            await Task.Run(() =>
            {
                List<IDisplayableNode> copiedNodes = new List<IDisplayableNode>();
                List<Connector> copiedConnectors = new List<Connector>();
                var inputPinsSource = nodesToCopy.SelectMany(node => node.Inputs);
                var outputPinsSource = nodesToCopy.SelectMany(node => node.Outputs);
                var inputPinsDest = copiedNodes.SelectMany(node => node.Inputs);
                var outputPinsDest = copiedNodes.SelectMany(node => node.Outputs);

                for (int i = 0; i < this.nodesToCopy.Count(); i++)
                {
                    var node = nodesToCopy.ElementAt(i);
                    var newNode = Activator.CreateInstance(node?.GetType()) as IDisplayableNode;                    

                    for (int j = newNode.Inputs.Count; j < nodesToCopy.ElementAt(i).Inputs.Count; j++)
                    {
                        var newPin = this.pinCreatorService.CreatePin(node.Inputs.ElementAt(j).Value.Current.GetType());
                        newNode.Inputs.Add(newPin);
                    }

                    copiedNodes.Add(newNode);

                    this.copiedNodes = copiedNodes;
                }

                foreach (var connS in this.connectorsToCopy)
                {
                    var inputSourceIndex = inputPinsSource.IndexOf(connS.InputPin);
                    var outputSourceIndex = outputPinsSource.IndexOf(connS.OutputPin);

                    this.connectorService.TryConnectPins(inputPinsDest.ElementAt(inputSourceIndex), outputPinsDest.ElementAt(outputSourceIndex), out Connector newConn, false);
                    copiedConnectors.Add(newConn);
                }

                this.copiedConnectors = copiedConnectors;
            });
        }
    }
}
