﻿// <copyright file="SettingsData.cs" company="PublicDomain.is">
//     CC0 1.0 Universal (CC0 1.0) - Public Domain Dedication
//     https://creativecommons.org/publicdomain/zero/1.0/legalcode
// </copyright>
// <auto-generated />

namespace PublicDomain
{
    // Directives
    using System.Collections.Generic;
    using System.Drawing;

    /// <summary>
    /// Urlister settings.
    /// </summary>
    public class SettingsData
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="T:PublicDomain.SettingsData"/> class.
        /// </summary>
        public SettingsData()
        {
            // Parameterless constructor
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="T:PublicDomain.SettingsData"/> always on top.
        /// </summary>
        /// <value><c>true</c> if always on top; otherwise, <c>false</c>.</value>
        public bool AlwaysOnTop { get; set; } = false;

        /// <summary>
        /// Gets or sets the directory.
        /// </summary>
        /// <value>The directory.</value>
        public string Directory { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the location.
        /// </summary>
        /// <value>The location.</value>
        public Point Location { get; set; } = new Point(0, 0);

        /// <summary>
        /// Gets or sets the size.
        /// </summary>
        /// <value>The size.</value>
        public Size Size { get; set; } = new Size(0, 0);

        /// <summary>
        /// Gets or sets the sort radio button.
        /// </summary>
        /// <value>The sort radio button.</value>
        public string SortRadioButton { get; set; } = "rawRadioButton";

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="T:PublicDomain.SettingsData"/> is desc.
        /// </summary>
        /// <value><c>true</c> if desc; otherwise, <c>false</c>.</value>
        public bool Desc { get; set; } = false;

        /// <summary>
        /// Gets or sets the API key.
        /// </summary>
        /// <value>The API key.</value>
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the search results.
        /// </summary>
        /// <value>The search results.</value>
        public int SearchResults { get; set; } = 10;

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="T:PublicDomain.SettingsData"/> hide identifiers in list.
        /// </summary>
        /// <value><c>true</c> if hide identifiers in list; otherwise, <c>false</c>.</value>
        public bool HideIdsInList { get; set; } = true;
    }
}