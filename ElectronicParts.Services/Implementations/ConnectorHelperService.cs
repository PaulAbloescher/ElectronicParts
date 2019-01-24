﻿using Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ElectronicParts.Services.Extensions;
using ElectronicParts.Services.Interfaces;

namespace ElectronicParts.Services.Implementations
{
    public class ConnectorHelperService : IConnectorHelperService
    {
        public IEnumerable<IDisplayableNode> ExistingNodes { get; set; }

        public bool IsSelfConnecting(IPin input, IPin output)
        {
            return this.ExistingNodes.Any(node => node.Inputs.Contains(input) && node.Outputs.Contains(output));
        }

        public double GetOffset(IPin input, IPin output, out int pinCount)
        {
            pinCount = 1;

            if (!this.IsSelfConnecting(input, output))
            {
                return 0;
            }

            var containingNode = ExistingNodes.First(node => node.Inputs.Contains(input));
            pinCount = containingNode.Inputs.Count;

            if (pinCount > containingNode.Outputs.Count)
            {
                if (containingNode.Inputs.IndexOf(input) < pinCount / 2)
                {
                    return -(pinCount + 1 - containingNode.Inputs.IndexOf(input) - 1) / 2.0;
                }

                else
                {
                    return containingNode.Inputs.IndexOf(input) / 2.0;
                }
            }
            else
            {
                if (containingNode.Outputs.IndexOf(output) < containingNode.Outputs.Count / 2)
                {
                    return -(containingNode.Outputs.Count + 1 - containingNode.Outputs.IndexOf(output) - 1) / 2.0;
                }

                else
                {
                    return containingNode.Outputs.IndexOf(output) / 2.0;
                }
            }
        }
    }
}
