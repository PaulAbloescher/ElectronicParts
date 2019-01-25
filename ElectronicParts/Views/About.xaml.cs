﻿// ***********************************************************************
// Assembly         : ElectronicParts
// Author           : Romane Jahn
// ***********************************************************************
// <copyright file="About.xaml.cs" company="FHWN">
//     Copyright ©  2019
// </copyright>
// <summary>Represents the About.xaml class of the ElectronicParts project</summary>
// ***********************************************************************
namespace ElectronicParts.Views
{
    using System.Reflection;
    using System.Windows;
    using MahApps.Metro.Controls;

    /// <summary>
    /// Interaction logic for the About view.
    /// </summary>
    public partial class About : MetroWindow
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="About"/> class.
        /// </summary>
        public About()
        {
            this.InitializeComponent();
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            this.VersionTextBlock.Text = "Version: " + string.Join(".", version.Major.ToString(), version.MajorRevision.ToString());
        }
    }
}
