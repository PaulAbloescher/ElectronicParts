﻿using ElectronicParts.Services.Interfaces;
using ElectronicParts.ViewModels.Commands;
using Shared;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Windows.Input;

namespace ElectronicParts.ViewModels
{
    public class NodeViewModel : BaseViewModel
    {
        private int top;

        private int left;

        private int width;

        private readonly IExecutionService executionService;

        public NodeViewModel(IDisplayableNode node, ICommand deleteCommand, ICommand inputPinCommand, ICommand OutputPinCommand, IExecutionService executionService)
        {
            this.Node = node ?? throw new ArgumentNullException(nameof(node));
            this.DeleteCommand = deleteCommand ?? throw new ArgumentNullException(nameof(deleteCommand));
            this.executionService = executionService ?? throw new ArgumentNullException(nameof(executionService));
            this.Inputs = node.Inputs?.Select(n => new PinViewModel(n, inputPinCommand, this.executionService)).ToObservableCollection();
            this.Outputs = node.Outputs?.Select(n => new PinViewModel(n, OutputPinCommand, this.executionService)).ToObservableCollection();
            this.Top = 18;
            this.Left = 20;
            this.Width = 50;

            this.IncreaseWidthCommand = new RelayCommand(arg =>
            {
                this.Width += 20;
                this.UpdatePosition();
                this.FirePropertyChanged(string.Empty);
            });

            this.DecreaseWidthCommand = new RelayCommand(arg =>
            {
                this.Width -= 20;
                this.UpdatePosition();
                this.FirePropertyChanged(string.Empty);
            });

            this.ActivateCommand = new RelayCommand(arg =>
            {
                this.Node.Activate();
                if (!(this.Inputs is null))
                {
                    foreach (var input in this.Inputs)
                    {
                        input.Update();
                    }
                }

                if (!(this.Outputs is null))
                {
                    foreach (var output in this.Outputs)
                    {
                        output.Update();
                    }
                }
            });

            this.Node.PictureChanged += NodePictureChanged;
        }

        public void RemoveDelegate()
        {
            this.Node.PictureChanged -= NodePictureChanged;
        }

        public void AddDeleage()
        {
            this.Node.PictureChanged -= NodePictureChanged;
            this.Node.PictureChanged += NodePictureChanged;
        }

        private void NodePictureChanged(object sender, EventArgs e)
        {
            this.FirePropertyChanged(nameof(Picture));
        }

        public int Width
        {
            get => this.width;


            private set
            {
                if (value < 50)
                {
                    this.width = 50;
                    return;
                }

                if (value > 200)
                {
                    this.width = 200;
                    return;
                }

                this.width = value;
            }
        }

        public int Top
        {
            get => this.top;

            set
            {
                if (value < 0)
                {
                    Set(ref this.top, 0);
                }
                else
                {
                    Set(ref this.top, value);
                }
                this.UpdateTop(this.Inputs?.Select((p, i) => Tuple.Create(p, i)), this.Top);
                this.UpdateTop(this.Outputs?.Select((p, i) => Tuple.Create(p, i)), this.Top);
            }
        }

        public int Left
        {
            get => this.left;

            set
            {
                if (value < 0)
                {
                    Set(ref this.left, 0);
                }
                else
                {
                    Set(ref this.left, value);
                }
                this.UpdateLeft(this.Inputs, this.left + 16);
                if (this.Inputs is null || this.Inputs.Count == 0)
                {
                    this.UpdateLeft(this.Outputs, this.Left + this.Width + 12);
                }
                else
                {
                    this.UpdateLeft(this.Outputs, this.Left + this.Width + 36);
                }
            }
        }


        /// <summary>
        /// Snaps to grid.
        /// </summary>
        /// <param name="gridSize">Size of the grid.</param>
        /// <param name="floor">If set to true will floor value else will ceil value.</param>
        public void SnapToNewGrid(int gridSize, bool floor)
        {
            int leftOffset = this.Inputs?.Count > 0 ? 10 : 0;

            if (floor)
            {
                if ((this.Left + leftOffset) % gridSize != 0)
                {
                    this.Left = Math.Max(this.Left.FloorTo(gridSize) - leftOffset, 0);
                }
                if (this.Top % gridSize != 0)
                {
                    this.Top = Math.Max(this.Top.FloorTo(gridSize), 0);
                }
            }
            else
            {
                if ((this.Left + leftOffset) % gridSize != 0)
                {
                    this.Left = Math.Max(this.Left.CeilingTo(gridSize) - leftOffset, 0);
                }
                if (this.Top % gridSize != 0)
                {
                    this.Top = Math.Max(this.Top.CeilingTo(gridSize), 0);
                }
            }
        }

        /// <summary>
        /// Snaps to grid.
        /// Will round to the next possible value.
        /// </summary>
        /// <param name="gridSize">Size of the grid.</param>
        public void SnapToNewGrid(int gridSize)
        {
            int leftOffset;
            leftOffset = this.Inputs.Count > 0 ? 10 : 0;


            if ((this.Left + leftOffset) % gridSize != 0)
            {
                this.Left = Math.Max(this.Left.RoundTo(gridSize) - leftOffset, 0);
            }
            if (this.Top % gridSize != 0)
            {
                this.Top = Math.Max(this.Top.RoundTo(gridSize), 0);
            }
        }

        public ObservableCollection<PinViewModel> Inputs { get; }

        public ObservableCollection<PinViewModel> Outputs { get; }

        public Bitmap Picture { get => this.Node.Picture; }

        public string Label { get => this.Node.Label; }

        public string Description { get => this.Node.Description; }

        public NodeType Type { get => this.Node.Type; }

        public IDisplayableNode Node { get; }

        public ICommand DeleteCommand { get; }

        public ICommand ActivateCommand { get; }

        public ICommand IncreaseWidthCommand { get; }

        public ICommand DecreaseWidthCommand { get; }

        public void Update()
        {
            this.FirePropertyChanged(nameof(Picture));
        }

        public int MaxPins
        {
            get
            {
                return (this.Inputs?.Count >= this.Outputs?.Count ? this.Inputs?.Count : this.Outputs?.Count) ?? 0;
            }
        }

        private void UpdateLeft(IEnumerable<PinViewModel> pins, int value)
        {
            if (pins is null)
            {
                return;
            }

            foreach (var pin in pins)
            {
                pin.Left = value;
            }
        }

        private void UpdateTop(IEnumerable<Tuple<PinViewModel, int>> pins, int value)
        {
            if (pins is null)
            {
                return;
            }

            foreach (var pin in pins)
            {
                pin.Item1.Top = (pin.Item2 * 19) + value + 16;
            }
        }

        public void UpdatePosition()
        {
            if (this.Inputs is null || this.Inputs.Count == 0)
            {
                this.UpdateLeft(this.Outputs, this.Left + this.Width + 13);
            }
            else
            {
                this.UpdateLeft(this.Outputs, this.Left + this.Width + 23);
            }
        }
    }
}
