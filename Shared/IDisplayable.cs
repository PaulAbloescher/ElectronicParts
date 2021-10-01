﻿// ***********************************************************************
// Assembly         : Shared
// Author           : 
// ***********************************************************************
// <copyright file="IDisplayable.cs" company="FHWN">
//     Copyright ©  2019
// </copyright>
// <summary>Represents the IDisplayable interface.</summary>
// ***********************************************************************

using System;
using System.Drawing;

namespace Shared
{
    /// <summary>
    ///     An interface used to enable displaying of an image.
    /// </summary>
    public interface IDisplayable
    {
        /// <summary>
        ///     Gets the current picture as bitmap.
        /// </summary>
        /// <value>The current picture.</value>
        Bitmap Picture { get; }

        /// <summary>
        ///     Is fired every time the <see cref="Picture" /> changes.
        /// </summary>
        event EventHandler PictureChanged;
    }
}