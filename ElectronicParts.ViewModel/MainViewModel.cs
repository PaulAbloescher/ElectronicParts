﻿// ***********************************************************************
// Assembly         : ElectronicParts.Services
// Author           : Alex Fischer, Peter Helf, Roman Jahn, Kevin Janisch
// ***********************************************************************
// <copyright file="MainViewModel.cs" company="FHWN">
//     Copyright ©  2019
// </copyright>
// <summary>Represents the MainViewModel class of the ElectronicParts programm</summary>
// ***********************************************************************
namespace ElectronicParts.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Serialization;
    using System.Threading.Tasks;
    using System.Timers;
    using System.Windows;
    using System.Windows.Input;
    using ElectronicParts.Models;
    using ElectronicParts.Services.Implementations;
    using ElectronicParts.Services.Interfaces;
    using ElectronicParts.ViewModels.Commands;
    using ElectronicParts.ViewModels.Converter;
    using GuiLabs.Undo;
    using Microsoft.Extensions.Logging;
    using Shared;

    /// <summary>
    /// The main view model of the application.
    /// </summary>
    public class MainViewModel : BaseViewModel
    {
        /// <summary>
        /// Represents the Action manager which is used for redo/undo functionality.
        /// </summary>
        private readonly ActionManager actionManager;

        /// <summary>
        /// A service which extracts the names of assemblies.
        /// </summary>
        private readonly IAssemblyNameExtractorService assemblyNameExtractorService;

        /// <summary>
        /// A service used for extracting types out of assemblies.
        /// </summary>
        private readonly IAssemblyService assemblyService;

        /// <summary>
        /// A service which is used for configurations.
        /// </summary>
        private readonly IConfigurationService configurationService;

        /// <summary>
        /// Represents the Connector helper service.
        /// </summary>
        private readonly IConnectorHelperService connectorHelperService;

        /// <summary>
        /// A service used for the execution of nodes.
        /// </summary>
        private readonly IExecutionService executionService;

        /// <summary>
        /// A service used for comparing the types of two objects.
        /// </summary>
        private readonly IGenericTypeComparerService genericTypeComparerService;

        /// <summary>
        /// The logger of the <see cref="MainViewModel"/> class.
        /// </summary>
        private readonly ILogger<MainViewModel> logger;

        /// <summary>
        /// A service used to create copies of nodes and connectors.
        /// </summary>
        private readonly INodeCopyService nodeCopyService;

        /// <summary>
        /// A service used to serialize the current nodes and connections.
        /// </summary>
        private readonly INodeSerializerService nodeSerializerService;

        /// <summary>
        /// A service used to connect two pins.
        /// </summary>
        private readonly IPinConnectorService pinConnectorService;

        /// <summary>
        /// The pin creator service.
        /// </summary>
        private readonly IPinCreatorService pinCreatorService;

        /// <summary>
        /// Represents timer which starts re-snapping of the nodes 500 milliseconds after the latest grid size change.
        /// </summary>
        private readonly Timer reSnappingTimer;

        /// <summary>
        /// A timer which updates the <see cref="MilisecondsPerLoop"/> property.
        /// </summary>
        private readonly Timer updateMillisecondsPerLoopUpdateTimer;

        /// <summary>
        /// A collection of all nodes on the board.
        /// </summary>
        private ObservableCollection<NodeViewModel> availableNodes;

        /// <summary>
        /// The cleared connections.
        /// </summary>
        private Stack<IEnumerable<ConnectorViewModel>> clearedConnections;

        /// <summary>
        /// The cleared nodes.
        /// </summary>
        private Stack<IEnumerable<NodeViewModel>> clearedNodes;

        /// <summary>
        /// A collection of all connections on the board.
        /// </summary>
        private ObservableCollection<ConnectorViewModel> connections;

        /// <summary>
        /// The amount of executions per second. 
        /// </summary>
        private int framesPerSecond;

        /// <summary>
        /// The size of cells in the visible grid.
        /// </summary>
        private int gridSize;

        /// <summary>
        /// A value indicating whether grid snapping is enabled or not.
        /// </summary>
        private bool gridSnappingEnabled;

        /// <summary>
        /// The time it took to complete one loop.
        /// </summary>
        private long milisecondsPerLoop;

        /// <summary>
        /// A collection of all nodes on the board.
        /// </summary>
        private ObservableCollection<NodeViewModel> nodes;

        /// <summary>
        /// The copied node view models.
        /// </summary>
        private IEnumerable<NodeViewModel> nodesToCopy;

        /// <summary>
        /// The currently selected category.
        /// </summary>
        private string selectedCategory;

        /// <summary>
        /// Initializes a new instance of the <see cref="MainViewModel" /> class.
        /// </summary>
        /// <param name="executionService">A service used for the execution of nodes.</param>
        /// <param name="assemblyService">A service used for extracting types out of assemblies.</param>
        /// <param name="pinConnectorService">A service for the connection of pins.</param>
        /// <param name="nodeSerializerService">A service which serializes all given nodes.</param>
        /// <param name="logger">The logger for the main view model.</param>
        /// <param name="configurationService">A service including all configurations for the application.</param>
        /// <param name="assemblyNameExtractorService">A service which extracts the names of assemblies.</param>
        /// <param name="actionManager">The Action manager which is used for redo/undo functionality.</param>
        /// <param name="genericTypeComparerService">A service which checks if two classes implement the same generic types.</param>
        /// <param name="nodeCopyService">The node copy service.</param>
        /// <param name="pinCreatorService">The pin creator service.</param>
        /// <param name="connectorHelperService">The connector helper service.</param>
        /// <exception cref="ArgumentNullException">
        /// ActionManager
        /// or
        /// executionService
        /// or
        /// pinConnectorService
        /// or
        /// nodeSerializerService
        /// or
        /// assemblyService
        /// or
        /// logger
        /// or
        /// assemblyNameExtractorService
        /// or
        /// nodeCopyService
        /// or
        /// pinCreatorService
        /// or
        /// connectorHelperService
        /// or
        /// configurationService.
        /// </exception>
        public MainViewModel(
            IExecutionService executionService,
            IAssemblyService assemblyService,
            IPinConnectorService pinConnectorService,
            INodeSerializerService nodeSerializerService,
            ILogger<MainViewModel> logger,
            IConfigurationService configurationService,
            IAssemblyNameExtractorService assemblyNameExtractorService,
            ActionManager actionManager,
            IGenericTypeComparerService genericTypeComparerService,
            INodeCopyService nodeCopyService,
            IPinCreatorService pinCreatorService,
            IConnectorHelperService connectorHelperService)
        {
            this.clearedNodes = new Stack<IEnumerable<NodeViewModel>>();
            this.clearedConnections = new Stack<IEnumerable<ConnectorViewModel>>();
            this.actionManager = actionManager ?? throw new ArgumentNullException(nameof(actionManager));
            this.executionService = executionService ?? throw new ArgumentNullException(nameof(executionService));
            this.pinConnectorService = pinConnectorService ?? throw new ArgumentNullException(nameof(pinConnectorService));
            this.nodeSerializerService = nodeSerializerService ?? throw new ArgumentNullException(nameof(nodeSerializerService));
            this.assemblyService = assemblyService ?? throw new ArgumentNullException(nameof(assemblyService));
            this.AvailableNodes = new ObservableCollection<NodeViewModel>();
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.assemblyNameExtractorService = assemblyNameExtractorService ?? throw new ArgumentNullException(nameof(assemblyNameExtractorService));
            this.genericTypeComparerService = genericTypeComparerService;
            this.nodeCopyService = nodeCopyService ?? throw new ArgumentNullException(nameof(nodeCopyService));
            this.pinCreatorService = pinCreatorService ?? throw new ArgumentNullException(nameof(pinCreatorService));
            this.connectorHelperService = connectorHelperService ?? throw new ArgumentNullException(nameof(connectorHelperService));

            this.configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            this.updateMillisecondsPerLoopUpdateTimer = new Timer(2000);
            this.updateMillisecondsPerLoopUpdateTimer.Elapsed += this.UpdateMillisecondsPerLoopUpdateTimer_Elapsed;
            this.updateMillisecondsPerLoopUpdateTimer.Start();
            this.reSnappingTimer = new Timer(500);
            this.reSnappingTimer.Elapsed += this.ReSnappingTimer_Elapsed;
            this.reSnappingTimer.AutoReset = false;
            this.FramesPerSecond = 50;
            this.SelectedCategory = this.NodeCategories.First();

            this.GridSnappingEnabled = true;
            this.GridSize = 10;

            this.SaveCommand = new RelayCommand(arg =>
            {
                SnapShot snapShot = SnapShotConverter.Convert(this.Nodes, this.connections);

                try
                {
                    this.nodeSerializerService.Serialize(snapShot);
                }
                catch (SerializationException e)
                {
                    this.logger.LogError(e, $"An error occurred while serializing ({nameof(this.SaveCommand)}).");
                    Debug.WriteLine("Failed serialization");

                    var faultyAssembly = this.assemblyNameExtractorService.ExtractAssemblyNameFromErrorMessage(e);

                    var faultyComponent = string.Empty;

                    try
                    {
                        faultyComponent = e.Message.Split('"')[1].Split('`')[0];
                    }
                    catch
                    {
                        faultyComponent = "Unknown";
                    }

                    var result = MessageBox.Show($"At least one component in the assembly {faultyAssembly} is faulty and can not be saved.\nThe type that caused the problem: {faultyComponent}", "Saving Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                foreach (NodeViewModel nodeVM in this.Nodes)
                {
                    nodeVM.AddDelegate();
                }
            });

            this.LoadCommand = new RelayCommand(arg =>
            {
                this.actionManager?.Clear();
                this.clearedNodes?.Clear();
                this.clearedConnections?.Clear();
                SnapShot snapShot = default(SnapShot);

                try
                {
                    snapShot = nodeSerializerService.Deserialize();
                }
                catch (SerializationException e)
                {
                    this.logger.LogError(e, $"An error occurred while deserializing ({nameof(this.LoadCommand)}).");
                    Debug.WriteLine("Failed deserialization");

                    var missingAssembly = this.assemblyNameExtractorService.ExtractAssemblyNameFromErrorMessage(e);
                    var result = MessageBox.Show($"There are missing assemblies: {missingAssembly}\nDo you want to add new assemblies?", "Loading Failed", MessageBoxButton.YesNo, MessageBoxImage.Error);

                    if (result == MessageBoxResult.Yes)
                    {
                        this.AddAssembly?.Invoke();
                    }
                }

                this.ResetPreviewLine();

                if (snapShot is null)
                {
                    return;
                }

                List<NodeViewModel> nodes = new List<NodeViewModel>();
                List<ConnectorViewModel> connections = new List<ConnectorViewModel>();

                foreach (NodeSnapShot node in snapShot.Nodes)
                {
                    NodeViewModel nodeViewModel = new NodeViewModel(node.Node, this.DeleteNodeCommand, this.InputPinCommand, this.OutputPinCommand, this.executionService)
                    {
                        Left = node.Position.X,
                        Top = node.Position.Y
                    };

                    nodes.Add(nodeViewModel);
                }

                this.nodes.Clear();

                foreach (NodeViewModel node in nodes)
                {
                    this.nodes.Add(node);
                }

                foreach (ConnectionSnapShot connection in snapShot.Connections)
                {
                    PinViewModel outputPinViewModel = null;

                    foreach (NodeViewModel node in nodes)
                    {
                        if (node.Outputs.Any(outputPin => outputPin.Pin == connection.OutputPin.Pin))
                        {
                            outputPinViewModel = node.Outputs.First(outputPin => outputPin.Pin == connection.OutputPin.Pin);
                            break;
                        }
                    }

                    outputPinViewModel.Left = connection.OutputPin.Position.X;
                    outputPinViewModel.Top = connection.OutputPin.Position.Y;

                    PinViewModel inputPinViewModel = null;

                    foreach (NodeViewModel node in nodes)
                    {
                        if (node.Inputs.Any(inputPin => inputPin.Pin == connection.InputPin.Pin))
                        {
                            inputPinViewModel = node.Inputs.First(inputPin => inputPin.Pin == connection.InputPin.Pin);
                            break;
                        }
                    }

                    inputPinViewModel.Left = connection.InputPin.Position.X;
                    inputPinViewModel.Top = connection.InputPin.Position.Y;

                    ConnectorViewModel connectorViewModel = new ConnectorViewModel(connection.Connector, inputPinViewModel, outputPinViewModel, this.DeleteConnectionCommand, this.connectorHelperService);

                    connections.Add(connectorViewModel);
                }

                this.Connections.Clear();

                foreach (ConnectorViewModel connection in connections)
                {
                    this.Connections.Add(connection);
                    this.pinConnectorService.ManuallyAddConnectionToExistingConnections(connection.Connector);
                }

                this.RepositionNodes();
            });

            this.ExitCommand = new RelayCommand(arg => Environment.Exit(0));

            this.ExecutionStepCommand = new RelayCommand(
                async arg =>
            {
                var nodeList = this.Nodes.Select(nodeVM => nodeVM.Node);
                await this.executionService.ExecuteOnce(nodeList);
                foreach (var connection in this.connections)
                {
                    connection.Update();
                }

                this.FirePropertyChanged(nameof(CanAddNode));
            },
            arg => !this.executionService.IsEnabled);

            this.ExecutionStartLoopCommand = new RelayCommand(
                async arg =>
            {
                var nodeList = this.Nodes.Select(nodeVM => nodeVM.Node);
                await this.executionService.StartExecutionLoop(
                    nodeList,
                    () =>
                {
                    this.FirePropertyChanged(nameof(CanAddNode));
                    Task.Run(() =>
                    {
                        foreach (var connection in this.Connections)
                        {
                            connection.Update();
                        }
                    });
                });
            },
            arg => !this.executionService.IsEnabled);

            this.ExecutionStopLoopCommand = new RelayCommand(
                arg =>
            {
                this.executionService.StopExecutionLoop();
                this.FirePropertyChanged(nameof(CanAddNode));
            },
            arg => this.executionService.IsEnabled);

            this.ResetAllConnectionsCommand = new RelayCommand(
                async arg =>
            {
                await this.ResetAllConnections();
                this.FirePropertyChanged(nameof(CanAddNode));
            });

            this.ReloadAssembliesCommand = new RelayCommand(
                async arg =>
            {
                await this.ReloadAssemblies();
            });

            this.ExecutionStopLoopAndResetCommand = new RelayCommand(
                async arg =>
            {
                this.executionService.StopExecutionLoop();
                await this.ResetAllConnections();
                this.FirePropertyChanged(nameof(CanAddNode));
            },
            arg => this.executionService.IsEnabled);

            this.InputPinCommand = new RelayCommand(
                arg =>
            {
                this.InputPin = arg as PinViewModel;
                this.Connect(this.InputPin, this.OutputPin);
            },
            arg => !this.executionService.IsEnabled);

            this.OutputPinCommand = new RelayCommand(
                arg =>
            {
                this.OutputPin = arg as PinViewModel;
                this.Connect(this.InputPin, this.OutputPin);
            },
            arg => !this.executionService.IsEnabled);

            this.DeleteConnectionCommand = new RelayCommand(
                arg =>
            {
                var connectionVm = arg as ConnectorViewModel;

                if (connectionVm is null)
                {
                    return;
                }

                this.pinConnectorService.TryRemoveConnection(connectionVm.Connector);

                this.connections.Remove(connectionVm);
            },
            arg => !this.executionService.IsEnabled);

            this.DeleteNodeCommand = new RelayCommand(
                arg =>
            {
                var nodeVm = arg as NodeViewModel;

                if (nodeVm is null)
                {
                    return;
                }

                this.InputPin = null;
                this.OutputPin = null;

                var connectionsMarkedForDeletion = this.Connections.Where(connection => nodeVm.Inputs.Contains(connection.Input) || nodeVm.Outputs.Contains(connection.Output)).ToList();
                var nodeToRemove = nodeVm;

                this.actionManager.Execute(new CallMethodAction(
                    () =>
                    {
                        connectionsMarkedForDeletion.ForEach(c =>
                        {
                            this.pinConnectorService.TryRemoveConnection(c.Connector);
                            this.connections.Remove(c);
                        });
                        this.Nodes.Remove(nodeVm);
                    },
                    () =>
                    {
                        connectionsMarkedForDeletion.ForEach(c =>
                        {
                            this.pinConnectorService.TryConnectPins(c.Input.Pin, c.Output.Pin, out Connector newConnection, true);
                            this.pinConnectorService.ManuallyAddConnectionToExistingConnections(c.Connector);
                            this.Connections.Add(c);
                        });
                        this.Nodes.Add(nodeVm);
                    }));
            },
            arg => !this.executionService.IsEnabled);

            this.UndoCommand = new RelayCommand(
                arg =>
            {
                this.actionManager.Undo();
            },
            arg => this.actionManager.CanUndo && !this.executionService.IsEnabled);

            this.RedoCommand = new RelayCommand(
                arg =>
            {
                this.actionManager.Redo();
            },
            arg => this.actionManager.CanRedo && !this.executionService.IsEnabled);

            this.ClearAllNodesCommand = new RelayCommand(
                arg =>
            {
                if (this.Nodes.Count > 0)
                {
                    this.actionManager.Execute(new CallMethodAction(
                        async () => await this.ClearCanvas(),
                        async () => await this.UndoClearCanvas()));
                }
            },
            arg => !this.executionService.IsEnabled && this.Nodes.Count > 0);

            this.AddNodeCommand = new RelayCommand(
                arg =>
            {
                this.AddNode(arg as IDisplayableNode);
            },
            arg => !this.executionService.IsEnabled);

            this.UpdateBoardSize = new RelayCommand(
                arg =>
            {
                this.RepositionNodes();

                this.FirePropertyChanged(nameof(BoardHeight));
                this.FirePropertyChanged(nameof(BoardWidth));
            });

            this.CopyCommand = new RelayCommand(
                arg =>
            {
                var connectors = this.GetFullSelectedConnectors(this.SelectedConntectors, this.SelectedNodes.SelectMany(n => n.Inputs.Concat(n.Outputs)));
                this.CopyItems(this.SelectedNodes, connectors);
            });

            this.PasteCommand = new RelayCommand(
                async arg =>
            {
                await this.nodeCopyService.CopyTaskAwaiter();
                var copiedNodes = this.nodeCopyService.CopiedNodes.ToList();
                var copiedConnectors = this.nodeCopyService.CopiedConnectors.ToList();
                var mousePosition = this.GetMousePosition();
                this.actionManager.Execute(new CallMethodAction(
                    () => { this.AddItems(copiedNodes, copiedConnectors, mousePosition); },
                    () =>
                {
                    foreach (var node in copiedNodes)
                    {
                        this.Nodes.Remove(this.Nodes.FirstOrDefault(nodeVM => nodeVM.Node == node));
                        foreach (var conn in copiedConnectors)
                        {
                            this.Connections.Remove(this.Connections.FirstOrDefault(connVM => connVM.Connector == conn));
                            this.pinConnectorService.TryRemoveConnection(conn);
                        }
                    }
                }));
                this.nodeCopyService.TryBeginCopyTask();

                this.RepositionNodes();
            },
            arg => this.nodeCopyService.IsInitialized && this.nodeCopyService.CopiedNodes?.Count() > 0 && !this.executionService.IsEnabled);

            this.DeleteCommand = new RelayCommand(
                arg =>
                {
                    var selectedConns = this.SelectedConntectors.ToList();
                    var selectedNodeVms = this.SelectedNodes.ToList();
                    var connsToDelete = selectedConns.Concat(this.GetConnectorViewModels(selectedNodeVms, this.Connections)).Distinct().ToList();
                    this.SelectedConntectors.Clear();
                    this.SelectedNodes.Clear();

                    this.actionManager.Execute(new CallMethodAction(
                        () =>
                        {
                            // remove all connectors from UI
                            foreach (var connectorVm in connsToDelete)
                            {
                                this.RemoveConnection(connectorVm, connectorVm.Connector);
                            }

                            foreach (var nodeVm in selectedNodeVms)
                            {
                                this.Nodes.Remove(nodeVm);
                            }
                        },
                    () =>
                    {
                        foreach (var nodeVM in selectedNodeVms)
                        {
                            this.Nodes.Add(nodeVM);
                        }

                        foreach (var connVM in connsToDelete)
                        {
                            this.pinConnectorService.TryConnectPins(connVM.Input.Pin, connVM.Output.Pin, out Connector newConn, true);
                            this.pinConnectorService.ManuallyAddConnectionToExistingConnections(connVM.Connector);
                            this.Connections.Add(connVM);
                        }
                    }));
                },
            arg => !this.executionService.IsEnabled);

            this.CutCommand = new RelayCommand(
                arg =>
            {
                var selectedConns = this.SelectedConntectors.ToList();
                var selectedNodeVms = this.SelectedNodes.ToList();
                var connsToDelete = selectedConns.Concat(this.GetConnectorViewModels(selectedNodeVms, this.Connections)).Distinct().ToList();
                this.SelectedConntectors.Clear();
                this.SelectedNodes.Clear();

                this.actionManager.Execute(new CallMethodAction(
                    () =>
                {
                    // remove all connectors from UI
                    foreach (var connectorVm in connsToDelete)
                    {
                        this.RemoveConnection(connectorVm, connectorVm.Connector);
                    }

                    // connections to be cut
                    var connectors = this.GetFullSelectedConnectors(selectedConns, selectedNodeVms.SelectMany(n => n.Inputs.Concat(n.Outputs)));
                    this.CopyItems(selectedNodeVms, connectors);

                    foreach (var nodeVm in selectedNodeVms)
                    {
                        this.Nodes.Remove(nodeVm);
                    }
                },
                () =>
                {
                    foreach (var nodeVM in selectedNodeVms)
                    {
                        this.Nodes.Add(nodeVM);
                    }

                    foreach (var connVM in connsToDelete)
                    {
                        this.pinConnectorService.TryConnectPins(connVM.Input.Pin, connVM.Output.Pin, out Connector newConn, true);
                        this.pinConnectorService.ManuallyAddConnectionToExistingConnections(connVM.Connector);
                        this.Connections.Add(connVM);
                    }
                }));
            },
            arg => !this.executionService.IsEnabled);

            this.AddInputPinsCommand = new RelayCommand(
                arg =>
            {
                var information = arg as Tuple<Type, int, NodeViewModel>;
                var pins = this.pinCreatorService.CreatePins(information.Item1, information.Item2);
                information.Item3.AddInputPins(pins);

                foreach (var inPin in information.Item3.Inputs)
                {
                    foreach (var connectionVM in this.Connections.Where(cVM => cVM.Input == inPin))
                    {
                        connectionVM.RecalculateOffsets();
                    }
                }

                foreach (var outPin in information.Item3.Outputs)
                {
                    foreach (var connectionVM in this.Connections.Where(cVM => cVM.Output == outPin))
                    {
                        connectionVM.RecalculateOffsets();
                    }
                }
            },
            arg => !this.executionService.IsEnabled);

            this.AddOutputPinsCommand = new RelayCommand(
                arg =>
            {
                var information = arg as Tuple<Type, int, NodeViewModel>;
                var pins = this.pinCreatorService.CreatePins(information.Item1, information.Item2);
                information.Item3.AddOutputPins(pins);

                foreach (var inPin in information.Item3.Inputs)
                {
                    foreach (var connectionVM in this.Connections.Where(cVM => cVM.Input == inPin))
                    {
                        connectionVM.RecalculateOffsets();
                    }
                }

                foreach (var outPin in information.Item3.Outputs)
                {
                    foreach (var connectionVM in this.Connections.Where(cVM => cVM.Output == outPin))
                    {
                        connectionVM.RecalculateOffsets();
                    }
                }
            },
            arg => !this.executionService.IsEnabled);

            this.HowToCommand = new RelayCommand(
                arg =>
            {
                var path = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                try
                {
                    Process.Start(System.IO.Path.Combine(path, @"Resources\ComponentsHandbook.pdf"));
                }
                catch (Exception e)
                {
                    this.logger.LogError(e.Message);
                }
            });

            this.UserManualCommand = new RelayCommand(
                arg =>
                {
                    var path = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                    try
                    {
                        Process.Start(System.IO.Path.Combine(path, @"Resources\UserManual.pdf"));
                    }
                    catch (Exception e)
                    {
                        this.logger.LogError(e.Message);
                    }
                });

            this.DocumentationCommand = new RelayCommand(
                arg =>
                {
                    var path = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                    try
                    {
                        Process.Start(System.IO.Path.Combine(path, @"Resources\Documentation.pdf"));
                    }
                    catch (Exception e)
                    {
                        this.logger.LogError(e.Message);
                    }
                });

            this.DocumentationOnlineCommand = new RelayCommand(
                arg =>
                {
                    var path = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                    try
                    {
                        Process.Start("https://bleialf.synology.me/site/api/index.html");
                    }
                    catch (Exception e)
                    {
                        this.logger.LogError(e.Message);
                    }
                });

            this.Nodes = new ObservableCollection<NodeViewModel>();
            this.PreviewLines = new ObservableCollection<PreviewLineViewModel>() { new PreviewLineViewModel() };
            this.AvailableNodes = new ObservableCollection<NodeViewModel>();
            this.Connections = new ObservableCollection<ConnectorViewModel>();
            this.SelectedNodes = new List<NodeViewModel>();
            this.SelectedConntectors = new List<ConnectorViewModel>();
            var reloadingTask = this.ReloadAssemblies();
            this.connectorHelperService.ExistingNodes = this.Nodes.Select(nodeVM => nodeVM.Node);
            this.connectorHelperService.ExistingConnections = this.Connections.Select(connVM => connVM.Connector);
            this.connectorHelperService.GetHeightMapping = (pin) =>
            {
                var connectionVM = this.Connections.FirstOrDefault(connVM => connVM.Input.Pin == pin);

                if (!(connectionVM is null))
                {
                    return connectionVM.Input.Top;
                }
                else
                {
                    connectionVM = this.Connections.FirstOrDefault(connVM => connVM.Output.Pin == pin);
                    if (connectionVM is null)
                    {
                        return 0;
                    }

                    return connectionVM.Output.Top;
                }
            };
            this.logger.LogInformation("Ctor MainVM done");
        }

        /// <summary>
        /// Gets or sets a action which adds assemblies to the application.
        /// </summary>
        /// <value>A action which adds assemblies to the application.</value>
        public Action AddAssembly { get; set; }

        /// <summary>
        /// Gets the add input pins command.
        /// </summary>
        /// <value>The add input pins command.</value>
        public ICommand AddInputPinsCommand { get; }

        /// <summary>
        /// Gets a command which adds a node to the board.
        /// </summary>
        /// <value>A command which adds a node to the board.</value>
        public ICommand AddNodeCommand { get; }

        /// <summary>
        /// Gets the add output pins command.
        /// </summary>
        /// <value>The add output pins command.</value>
        public ICommand AddOutputPinsCommand { get; }

        /// <summary>
        /// Gets or sets a collection of all nodes which can be used on the board.
        /// </summary>
        /// <value>All nodes which have loaded correctly.</value>
        public ObservableCollection<NodeViewModel> AvailableNodes
        {
            get
            {
                return this.availableNodes.Where(
                    nodeVM =>
                   (Enum.GetName(typeof(NodeType), nodeVM.Type) == this.SelectedCategory || this.SelectedCategory == this.NodeCategories.First()))
                   .ToObservableCollection();
            }

            set
            {
                this.Set(ref this.availableNodes, value);
            }
        }

        /// <summary>
        /// Gets the current board height.
        /// </summary>
        /// <value>The height of the board.</value>
        public int BoardHeight { get => this.configurationService.Configuration.BoardHeight; }

        /// <summary>
        /// Gets the current board width.
        /// </summary>
        /// <value>The width of the board.</value>
        public int BoardWidth { get => this.configurationService.Configuration.BoardWidth; }

        /// <summary>
        /// Gets a value indicating whether a node can be added right now.
        /// </summary>
        /// <value>A value indicating whether a node can be added right now.</value>
        public bool CanAddNode { get => !this.executionService.IsEnabled; }

        /// <summary>
        /// Gets a command which clears all nodes from the board.
        /// </summary>
        /// <value>A command which clears all nodes from the board.</value>
        public ICommand ClearAllNodesCommand { get; }

        /// <summary>
        /// Gets or sets a collection of all current connections.
        /// </summary>
        /// <value>A collection of all current connections.</value>
        public ObservableCollection<ConnectorViewModel> Connections
        {
            get => this.connections;

            set
            {
                this.Set(ref this.connections, value);
            }
        }

        /// <summary>
        /// Gets the copy command.
        /// </summary>
        /// <value>The copy command.</value>
        public ICommand CopyCommand { get; }

        /// <summary>
        /// Gets the cut command.
        /// </summary>
        /// <value>The cut command.</value>
        public ICommand CutCommand { get; }

        /// <summary>
        /// Gets the delete command.
        /// </summary>
        /// <value>The delete command.</value>
        public ICommand DeleteCommand { get; }

        /// <summary>
        /// Gets a command which removes a connection between two pins.
        /// </summary>
        /// <value>A command which removes a node from the board.</value>
        public ICommand DeleteConnectionCommand { get; }

        /// <summary>
        /// Gets a command which removes a node from the board.
        /// </summary>
        /// <value>A command which removes a node from the board.</value>
        public ICommand DeleteNodeCommand { get; }

        /// <summary>
        /// Gets a command which starts the execution loop.
        /// </summary>
        /// <value>A command which starts the execution loop.</value>
        public ICommand ExecutionStartLoopCommand { get; }

        /// <summary>
        /// Gets a command which invokes the Execution method of every node once.
        /// </summary>
        /// <value>A command which invokes the Execution method of every node once.</value>
        public ICommand ExecutionStepCommand { get; }

        /// <summary>
        /// Gets a command which stops the execution loop and resets all node values to their default.
        /// </summary>
        /// <value>A command which stops the execution loop and resets all node values to their default.</value>
        public ICommand ExecutionStopLoopAndResetCommand { get; }

        /// <summary>
        /// Gets a command which stops the execution loop.
        /// </summary>
        /// <value>A command which stops the execution loop.</value>
        public ICommand ExecutionStopLoopCommand { get; }

        /// <summary>
        /// Gets a command which exits the application.
        /// </summary>
        /// <value>A command which exits the application.</value>
        public ICommand ExitCommand { get; }

        /// <summary>
        /// Gets or sets the amount of executions per second. 
        /// </summary>
        /// <value>The amount of executions per second. </value>
        public int FramesPerSecond
        {
            get => this.framesPerSecond;
            set
            {
                if (value > 0 && value <= 100)
                {
                    this.Set(ref this.framesPerSecond, value);
                    this.executionService.FramesPerSecond = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the get mouse position function.
        /// </summary>
        /// <value>The get mouse position.</value>
        public Func<Point> GetMousePosition { get; set; }

        /// <summary>
        /// Gets or sets the cell size of the visible grid.
        /// </summary>
        /// <value>The cell size of the visible grid.</value>
        public int GridSize
        {
            get => this.gridSize;
            set
            {
                if (value >= 5 && value <= 30)
                {
                    this.Set(ref this.gridSize, value);
                    this.reSnappingTimer.Stop();
                    this.reSnappingTimer.Start();
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether grid snapping is enabled or not.
        /// </summary>
        /// <value>A value indicating whether grid snapping is enabled or not.</value>
        public bool GridSnappingEnabled
        {
            get => this.gridSnappingEnabled;
            set
            {
                if (value != this.gridSnappingEnabled)
                {
                    this.gridSnappingEnabled = value;
                    var snappingTask = this.SnapAllNodesToGrid();
                }
            }
        }

        /// <summary>
        /// Gets or sets the offset of the horizontal scroll bar.
        /// </summary>
        /// <value>The offset of the horizontal scroll bar.</value>
        public int HorizontalScrollerOffset { get; set; }

        /// <summary>
        /// Gets the command to open the HowTo PDF.
        /// </summary>
        /// <value>The command to open the HowTo PDF.</value>
        public ICommand HowToCommand { get; }

        /// <summary>
        /// Gets the command to open the user manual PDF.
        /// </summary>
        /// <value>The command to open the user manual PDF.</value>
        public ICommand UserManualCommand { get; }

        /// <summary>
        /// Gets the documentation command.
        /// </summary>
        /// <value>The documentation command.</value>
        public ICommand DocumentationCommand { get; }

        /// <summary>
        /// Gets the documentation online command.
        /// </summary>
        /// <value>The documentation online command.</value>
        public ICommand DocumentationOnlineCommand { get; }

        /// <summary>
        /// Gets or sets the currently selected input pin.
        /// </summary>
        /// <value>The currently selected input pin.</value>
        public PinViewModel InputPin { get; set; }

        /// <summary>
        /// Gets a command used in the <see cref="InputPin"/> property.
        /// </summary>
        /// <value>A command used in the <see cref="InputPin"/> property.</value>
        public ICommand InputPinCommand { get; }

        /// <summary>
        /// Gets a command which loads a previously saved board from a file.
        /// </summary>
        /// <value>A command which loads a previously saved board from a file.</value>
        public ICommand LoadCommand { get; }

        /// <summary>
        /// Gets or sets the time it took to complete one loop.
        /// </summary>
        /// <value>The time it took to complete one loop.</value>
        public long MilisecondsPerLoop
        {
            get => this.milisecondsPerLoop;
            set
            {
                this.Set(ref this.milisecondsPerLoop, value);
            }
        }

        /// <summary>
        /// Gets all possible categories.
        /// </summary>
        /// <value>All possible categories.</value>
        public IEnumerable<string> NodeCategories
        {
            get
            {
                return Enum.GetNames(typeof(Shared.NodeType)).Prepend("All");
            }
        }

        /// <summary>
        /// Gets or sets a collection of all current nodes.
        /// </summary>
        /// <value>A collection of all current nodes.</value>
        public ObservableCollection<NodeViewModel> Nodes
        {
            get => this.nodes;

            set
            {
                this.Set(ref this.nodes, value);
            }
        }

        /// <summary>
        /// Gets or sets the currently selected output pin.
        /// </summary>
        /// <value>The currently selected output pin.</value>
        public PinViewModel OutputPin { get; set; }

        /// <summary>
        /// Gets a command used in the <see cref="OutputPin"/> property.
        /// </summary>
        /// <value>A command used in the <see cref="OutputPin"/> property.</value>
        public ICommand OutputPinCommand { get; }

        /// <summary>
        /// Gets the paste command.
        /// </summary>
        /// <value>The paste command.</value>
        public ICommand PasteCommand { get; }

        /// <summary>
        /// Gets a collection of lines showing the preview of a connection. 
        /// </summary>
        /// <value>A collection of lines showing the preview of a connection. </value>
        public ObservableCollection<PreviewLineViewModel> PreviewLines { get; private set; }

        /// <summary>
        /// Gets a command which redoes the last action.
        /// </summary>
        /// <value>A command which redoes the last action.</value>
        public ICommand RedoCommand { get; }

        /// <summary>
        /// Gets a command which reloads all assemblies from the assembly folder.
        /// </summary>
        /// <value>A command which reloads all assemblies from the assembly folder.</value>
        public ICommand ReloadAssembliesCommand { get; }

        /// <summary>
        /// Gets a command which resets the values of all connection.
        /// </summary>
        /// <value>A command which resets the values of all connection.</value>
        public ICommand ResetAllConnectionsCommand { get; }

        /// <summary>
        /// Gets a command which saves the current nodes and connections to a file.
        /// </summary>
        /// <value>A command which saves the current nodes and connections to a file.</value>
        public ICommand SaveCommand { get; }

        /// <summary>
        /// Gets or sets the currently selected category.
        /// </summary>
        /// <value>The currently selected category.</value>
        public string SelectedCategory
        {
            get => this.selectedCategory;
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    this.Set(ref this.selectedCategory, value);
                    this.FirePropertyChanged(nameof(this.AvailableNodes));
                }
            }
        }

        /// <summary>
        /// Gets or sets the selected connectors.
        /// </summary>
        /// <value>The selected connectors.</value>
        public ICollection<ConnectorViewModel> SelectedConntectors { get; set; }

        /// <summary>
        /// Gets or sets the currently selected node.
        /// </summary>
        /// <value>The currently selected node.</value>
        public NodeViewModel SelectedNode { get; set; }

        /// <summary>
        /// Gets or sets the information of the currently selected node.
        /// </summary>
        /// <value>The information of the currently selected node.</value>
        public NodeViewModel SelectedNodeInformation { get; set; }

        /// <summary>
        /// Gets or sets the selected nodes.
        /// </summary>
        /// <value>The selected nodes.</value>
        public ICollection<NodeViewModel> SelectedNodes { get; set; }

        /// <summary>
        /// Gets a command which undoes the last action.
        /// </summary>
        /// <value>A command which undoes the last action.</value>
        public ICommand UndoCommand { get; }

        /// <summary>
        /// Gets a command which updates the board size.
        /// </summary>
        /// <value>A command which updates the board size.</value>
        public ICommand UpdateBoardSize { get; }

        /// <summary>
        /// Gets or sets the offset of the vertical scroll bar.
        /// </summary>
        /// <value>The offset of the vertical scroll bar.</value>
        public int VerticalScrollerOffset { get; set; }

        /// <summary>
        /// Gets the connector view models connected to the <paramref name="nodeVms"/>.
        /// </summary>
        /// <param name="nodeVms">The node VMS.</param>
        /// <param name="connectorVms">The connector VMS.</param>
        /// <returns>An Enumerable containing all connector view models per node.</returns>
        public IEnumerable<ConnectorViewModel> GetConnectorViewModels(IEnumerable<NodeViewModel> nodeVms, IEnumerable<ConnectorViewModel> connectorVms)
        {
            var pinVms = nodeVms.SelectMany(n => n.Inputs.Concat(n.Outputs));
            return connectorVms.Where(c => pinVms.Contains(c.Input) || pinVms.Contains(c.Output));
        }

        /// <summary>
        /// Reloads all assemblies from the assembly folder.
        /// </summary>
        /// <returns>A await able task.</returns>
        public async Task ReloadAssemblies()
        {
            await this.assemblyService.LoadAssemblies();
            this.availableNodes.Clear();
            foreach (var assembly in this.assemblyService.AvailableNodes.Select(node => new NodeViewModel(node, this.DeleteNodeCommand, this.InputPinCommand, this.OutputPinCommand, this.executionService)))
            {
                this.availableNodes.Add(assembly);
            }

            this.FirePropertyChanged(nameof(this.AvailableNodes));
        }

        /// <summary>
        /// Resets the preview line from the selected pin to the mouse.
        /// </summary>
        public void ResetPreviewLine()
        {
            if (this.PreviewLines[0].Visible)
            {
                this.PreviewLines[0].Visible = false;
                this.OutputPin = null;
                this.InputPin = null;
                this.ResetPossibleConnections();
            }
        }

        /// <summary>
        /// Adds <see cref="IDisplayableNode"/> and <see cref="Connector"/> instances to the view model.
        /// </summary>
        /// <param name="nodes">The nodes being added.</param>
        /// <param name="connectors">The connectors being added.</param>
        private void AddItems(IEnumerable<IDisplayableNode> nodes, IEnumerable<Connector> connectors)
        {
            if (this.GetMousePosition is null)
            {
                return;
            }

            this.AddItems(nodes, connectors, this.GetMousePosition());
        }

        /// <summary>
        /// Adds <see cref="IDisplayableNode"/> and <see cref="Connector"/> instances to the view model.
        /// </summary>
        /// <param name="nodes">The nodes being added.</param>
        /// <param name="connectors">The connectors being added.</param>
        /// <param name="mousePosition">The current mouse position.</param>
        private void AddItems(IEnumerable<IDisplayableNode> nodes, IEnumerable<Connector> connectors, Point mousePosition)
        {
            var mostLeft = this.nodesToCopy.FirstOrDefault(n => n.Left == this.nodesToCopy.Min(nd => nd.Left));
            var minLeft = mostLeft?.Left ?? 0;
            var minTop = mostLeft?.Top ?? 0;

            var nodeVms = nodes.Select(n => new NodeViewModel(n, this.DeleteNodeCommand, this.InputPinCommand, this.OutputPinCommand, this.executionService)).ToArray();
            for (int i = 0; i < nodeVms.Length; i++)
            {
                var nodeVm = nodeVms[i];
                var originalVm = this.nodesToCopy.ElementAt(i);

                nodeVm.Left = originalVm.Left - minLeft + (int)mousePosition.X;
                nodeVm.Top += originalVm.Top - minTop + (int)mousePosition.Y;
                this.Nodes.Add(nodeVm);
            }

            var connectorVms = connectors.Select(c => new ConnectorViewModel(c, this.GetPinViewModel(nodeVms, c.InputPin), this.GetPinViewModel(nodeVms, c.OutputPin), this.DeleteConnectionCommand, this.connectorHelperService));

            foreach (var connectorVm in connectorVms)
            {
                this.pinConnectorService.RedoConnection(connectorVm.Connector);
                this.Connections.Add(connectorVm);
            }
        }

        /// <summary>
        /// Adds a node to the board.
        /// </summary>
        /// <param name="node">The node which will be added.</param>
        private void AddNode(IDisplayableNode node)
        {
            if (node is null)
            {
                return;
            }

            var copy = Activator.CreateInstance(node?.GetType()) as IDisplayableNode;
            var vm = new NodeViewModel(copy, this.DeleteNodeCommand, this.InputPinCommand, this.OutputPinCommand, this.executionService)
            {
                Top = this.VerticalScrollerOffset + 20,
                Left = this.HorizontalScrollerOffset + 20
            };
            this.actionManager.Execute(new CallMethodAction(() => this.Nodes.Add(vm), () => this.Nodes.Remove(vm)));
            vm.SnapToNewGrid(this.GridSize, false);
        }

        /// <summary>
        /// Checks all possible connection and marks them.
        /// </summary>
        /// <param name="pinList">The list of pins which are checked for possible connections.</param>
        /// <param name="selectedPin">The pin which was chosen for connection.</param>
        /// <param name="checkExistingConnections">A value indicating if it should be checked if the pin already has other connections.</param>
        private void CheckPossibleConnections(IEnumerable<PinViewModel> pinList, IPin selectedPin, bool checkExistingConnections)
        {
            if (!(pinList is null) && !(pinList.GetEnumerator() is null))
            {
                var xx = pinList.ToList();

                foreach (var pin in pinList)
                {
                    if (checkExistingConnections)
                    {
                        if (this.pinConnectorService.IsConnectable(pin.Pin, selectedPin))
                        {
                            pin.CanBeConnected = true;
                            continue;
                        }
                    }
                    else
                    {
                        if (this.genericTypeComparerService.IsSameGenericType(pin.Pin, selectedPin) && !this.pinConnectorService.HasConnection(selectedPin))
                        {
                            pin.CanBeConnected = true;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Deletes on all nodes on the board.
        /// </summary>
        /// <returns>A await able task.</returns>
        private async Task ClearCanvas()
        {
            await Task.Run(() =>
            {
                this.clearedConnections.Push(this.Connections.ToList());
                this.clearedNodes.Push(this.Nodes.ToList());

                foreach (var connectionVM in this.Connections)
                {
                    this.pinConnectorService.TryRemoveConnection(connectionVM.Connector);
                }

                Application.Current.Dispatcher.Invoke(() =>
                {
                    this.Connections.Clear();
                    this.Nodes.Clear();
                });
            });
        }

        /// <summary>
        /// Connects two pins.
        /// </summary>
        /// <param name="inputPin">The input pin of the connection.</param>
        /// <param name="outputPin">The output pin of the connection.</param>
        private void Connect(PinViewModel inputPin, PinViewModel outputPin)
        {
            if (!(inputPin is null) && !(outputPin is null) && this.pinConnectorService.IsConnectable(inputPin.Pin, outputPin.Pin))
            {
                Connector createdConnector = null;
                ConnectorViewModel createdConnectorVM = null;
                var inPin = inputPin;
                var outPin = outputPin;
                Action creationAction = () =>
                {
                    if (createdConnector is null && createdConnectorVM is null)
                    {
                        this.MakeConnection(inPin, outPin, out createdConnectorVM, out createdConnector);
                        createdConnectorVM.RecalculateOffsets();
                    }
                    else
                    {
                        this.pinConnectorService.TryConnectPins(createdConnectorVM.Input.Pin, createdConnectorVM.Output.Pin, out Connector newConn, true);
                        this.pinConnectorService.ManuallyAddConnectionToExistingConnections(createdConnector);
                        this.Connections.Add(createdConnectorVM);
                        createdConnectorVM.RecalculateOffsets();
                    }
                };
                Action deleteAction = () => this.RemoveConnection(createdConnectorVM, createdConnector);

                this.actionManager.Execute(new CallMethodAction(creationAction, deleteAction));

                this.PreviewLines[0].Visible = false;
                this.ResetPossibleConnections();
                return;
            }

            if (!(this.InputPin is null) && !(this.OutputPin is null))
            {
                return;
            }

            if (!(this.InputPin is null))
            {
                this.ResetPossibleConnections();
                if (this.pinConnectorService.HasConnection(this.InputPin.Pin) || !(this.OutputPin is null))
                {
                    this.ResetPreviewLine();
                }
                else
                {
                    this.CheckPossibleConnections(this.nodes.Where(node => !(node.Outputs is null)).SelectMany(node => node.Outputs), this.InputPin.Pin, false);
                    PreviewLineViewModel previewLineViewModel = this.PreviewLines[0];
                    previewLineViewModel.PointOneX = this.InputPin.Left;
                    previewLineViewModel.PointOneY = this.InputPin.Top;
                }
            }

            if (!(this.OutputPin is null))
            {
                this.CheckPossibleConnections(this.nodes.Where(node => !(node.Inputs is null)).SelectMany(node => node.Inputs), this.OutputPin.Pin, true);
                PreviewLineViewModel previewLineViewModel = this.PreviewLines[0];
                previewLineViewModel.PointOneX = this.OutputPin.Left;
                previewLineViewModel.PointOneY = this.OutputPin.Top;
            }
        }

        /// <summary>
        /// Creates a copy of <see cref="NodeViewModel"/> and <see cref="ConnectorViewModel"/> instances.
        /// </summary>
        /// <param name="nodes">The nodes being copied.</param>
        /// <param name="connectors">The connectors being copied.</param>
        private void CopyItems(IEnumerable<NodeViewModel> nodes, IEnumerable<ConnectorViewModel> connectors)
        {
            this.nodesToCopy = nodes.ToList();
            this.nodeCopyService.InitializeCopyProcess(nodes.Select(vm => vm.Node), connectors.Select(vm => vm.Connector));
        }

        /// <summary>
        /// Gets all connectors which are fully included in the selected area.
        /// </summary>
        /// <param name="connectors">All current connections.</param>
        /// <param name="pins">The pins of the selected area.</param>
        /// <returns>All connectors which are fully included in the selected area.</returns>
        private IEnumerable<ConnectorViewModel> GetFullSelectedConnectors(IEnumerable<ConnectorViewModel> connectors, IEnumerable<PinViewModel> pins)
        {
            return connectors.Where(c => pins.Contains(c.Input) && pins.Contains(c.Output));
        }

        /// <summary>
        /// Gets the pin view model of a pin.
        /// </summary>
        /// <param name="nodes">The IEnumerable in which the pin view models are stored.</param>
        /// <param name="pin">The pin being searched for.</param>
        /// <returns>Either the view model of the pin being searched for or null.</returns>
        private PinViewModel GetPinViewModel(IEnumerable<NodeViewModel> nodes, IPin pin)
        {
            return nodes.SelectMany(n => n.Inputs.Concat(n.Outputs)).FirstOrDefault(p => p.Pin.Equals(pin));
        }

        /// <summary>
        /// Creates a connection between two pins.
        /// </summary>
        /// <param name="input">The input pin of the connection.</param>
        /// <param name="output">The output pin of the connection.</param>
        /// <param name="connectionVM">The created view model of the connection.</param>
        /// <param name="connection">The created connection.</param>
        private void MakeConnection(PinViewModel input, PinViewModel output, out ConnectorViewModel connectionVM, out Connector connection)
        {
            connectionVM = null;
            connection = null;

            Connector newConnection = null;

            if (this.pinConnectorService.TryConnectPins(input.Pin, output.Pin, out newConnection, false))
            {
                connection = newConnection;
                connectionVM = new ConnectorViewModel(newConnection, input, output, this.DeleteConnectionCommand, this.connectorHelperService);
                this.Connections.Add(connectionVM);
                try
                {
                    foreach (var conn in this.Connections.Where(connVM => connVM.Input == input || connVM.Output == output))
                    {
                        conn.UpdateLine();
                    }
                }
                catch
                {

                }
            }

            this.InputPin = null;
            this.OutputPin = null;
            return;
        }

        /// <summary>
        /// Removes the connection between two pins.
        /// </summary>
        /// <param name="connectionVM">The view model of the connection.</param>
        /// <param name="connection">The instance of the connection.</param>
        private void RemoveConnection(ConnectorViewModel connectionVM, Connector connection)
        {
            if (!(connectionVM is null) && !(connection is null))
            {
                if (this.pinConnectorService.TryRemoveConnection(connection))
                {
                    this.Connections.Remove(connectionVM);
                }
            }
        }

        /// <summary>
        /// Repositions nodes when the board size changed.
        /// </summary>
        private void RepositionNodes()
        {
            foreach (var node in this.Nodes)
            {
                if (node.Left + node.Width + 20 >= this.BoardWidth)
                {
                    node.Left = this.BoardWidth - node.Width - 20;
                }

                if (node.Top + (20 * node.MaxPins) >= this.BoardHeight)
                {
                    node.Top = this.BoardHeight - (20 * node.MaxPins);
                }
            }
        }

        /// <summary>
        /// Resets the values of all connection.
        /// </summary>
        /// <returns>A await able task.</returns>
        private async Task ResetAllConnections()
        {
            await Task.Run(() =>
            {
                foreach (var connectionVM in this.Connections)
                {
                    connectionVM.Connector.ResetValue();
                }

                foreach (var connection in this.Connections)
                {
                    connection.Update();
                }

                foreach (var nodeVm in this.Nodes)
                {
                    nodeVm.Update();
                }
            });
        }

        /// <summary>
        /// Resets the marked possible connections.
        /// </summary>
        private void ResetPossibleConnections()
        {
            foreach (var pinList in this.nodes.Select(node => node.Outputs))
            {
                if (pinList == null)
                {
                    continue;
                }

                foreach (var pin in pinList)
                {
                    pin.CanBeConnected = false;
                }
            }

            foreach (var pinList in this.nodes.Select(node => node.Inputs))
            {
                if (pinList == null)
                {
                    continue;
                }

                foreach (var pin in pinList)
                {
                    pin.CanBeConnected = false;
                }
            }
        }

        /// <summary>
        /// Is invoked when the nodes should be snapped to the grid cells.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event arguments.</param>
        private async void ReSnappingTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            await this.SnapAllNodesToGrid();
        }

        /// <summary>
        /// Snaps the nodes to the grid cells.
        /// </summary>
        /// <returns>A await able task.</returns>
        private async Task SnapAllNodesToGrid()
        {
            await Task.Run(() =>
            {
                if (!(this.Nodes is null) && this.GridSnappingEnabled)
                {
                    foreach (var nodeVm in this.Nodes)
                    {
                        try
                        {
                            nodeVm.SnapToNewGrid(this.GridSize);
                        }
                        catch (Exception e)
                        {
                            this.logger.LogError(e, $"Unexpected error in {nameof(this.SnapAllNodesToGrid)}");
                        }
                    }
                }
            });
        }

        /// <summary>
        /// Snaps the nodes to the grid cells.
        /// </summary>
        /// <param name="floor">A value indicating whether the node snaps to the ceiling or floor of the cell.</param>
        /// <returns>A await able task.</returns>
        private async Task SnapAllNodesToGrid(bool floor)
        {
            await Task.Run(() =>
            {
                if (!(this.Nodes is null) && this.GridSnappingEnabled)
                {
                    foreach (var nodeVm in this.Nodes)
                    {
                        try
                        {
                            nodeVm.SnapToNewGrid(this.GridSize, floor);
                        }
                        catch (Exception e)
                        {
                            this.logger.LogError(e, $"Unexpected error in {nameof(this.SnapAllNodesToGrid)}");
                        }
                    }
                }
            });
        }

        /// <summary>
        /// Undoes the <see cref="ClearCanvas"/> method.
        /// </summary>
        /// <returns>A await able task.</returns>
        private async Task UndoClearCanvas()
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                foreach (var nodeVM in this.clearedNodes.Pop())
                {
                    this.Nodes.Add(nodeVM);
                }
            });

            var connectionsList = new List<ConnectorViewModel>();

            await Task.Run(() =>
            {
                foreach (var connector in this.clearedConnections.Pop())
                {
                    this.pinConnectorService.TryConnectPins(connector.Input.Pin, connector.Output.Pin, out Connector newConnection, true);

                    this.pinConnectorService.ManuallyAddConnectionToExistingConnections(connector.Connector);

                    connectionsList.Add(connector);
                }
            });

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                foreach (var connectorVM in connectionsList)
                {
                    this.Connections.Add(connectorVM);
                }
            });
        }

        /// <summary>
        /// Updates the amount of time it took to complete a loop.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void UpdateMillisecondsPerLoopUpdateTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (this.executionService.IsEnabled)
            {
                this.MilisecondsPerLoop = this.executionService.MillisecondsPerLoop;
            }
        }
    }
}
