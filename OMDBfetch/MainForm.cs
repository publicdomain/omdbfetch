﻿// <copyright file="MainForm.cs" company="PublicDomain.is">
//     CC0 1.0 Universal (CC0 1.0) - Public Domain Dedication
//     https://creativecommons.org/publicdomain/zero/1.0/legalcode
// </copyright>

namespace OMDBfetch
{
    // Directives
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Data;
    using System.Diagnostics;
    using System.Drawing;
    using System.IO;
    using System.Net;
    using System.Linq;
    using System.Reflection;
    using System.Windows.Forms;
    using System.Xml.Serialization;
    using Microsoft.VisualBasic;
    using PublicDomain;
    using Newtonsoft.Json;
    using OMDbApiNet;
    using OMDbApiNet.Model;
    using System.Text;

    /// <summary>
    /// Description of MainForm.
    /// </summary>
    public partial class MainForm : Form
    {
        /// <summary>
        /// The directory.
        /// </summary>
        private string directory;

        /// <summary>
        /// The fetched count.
        /// </summary>
        private int fetchedCount = 0;

        /// <summary>
        /// Gets or sets the associated icon.
        /// </summary>
        /// <value>The associated icon.</value>
        private Icon associatedIcon = null;

        /// <summary>
        /// The settings data.
        /// </summary>
        private SettingsData settingsData = null;

        /// <summary>
        /// The data table.
        /// </summary>
        private DataTable dataTable = new DataTable();

        /// <summary>
        /// The settings data path.
        /// </summary>
        private string settingsDataPath = $"{Application.ProductName}-SettingsData.txt";

        /// <summary>
        /// The error log path.
        /// </summary>
        private string errorLogPath = "OMDBfetch-log.txt";

        /// <summary>
        /// The image file path.
        /// </summary>
        private string imageFilePath = String.Empty;

        /// <summary>
        /// The server API calls.
        /// </summary>
        private int serverApiCalls = 0;

        /// <summary>
        /// The local API calls.
        /// </summary>
        private int localApiCalls = 0;

        /// <summary>
        /// The async omdb client.
        /// </summary>
        AsyncOmdbClient omdb = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:OMDBfetch.MainForm"/> class.
        /// </summary>
        public MainForm()
        {
            // The InitializeComponent() call is required for Windows Forms designer support.
            this.InitializeComponent();

            /* Set icons */

            // Set associated icon from exe file
            this.associatedIcon = Icon.ExtractAssociatedIcon(typeof(MainForm).GetTypeInfo().Assembly.Location);

            // Set PublicDomain.is tool strip menu item image
            this.freeReleasesPublicDomainIsToolStripMenuItem.Image = this.associatedIcon.ToBitmap();

            // SSL fix
            System.Net.ServicePointManager.Expect100Continue = true;
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;
            System.Net.ServicePointManager.ServerCertificateValidationCallback = delegate { return true; }; // DEBUG, Linux

            /* Data table */

            DataColumn column1 = new DataColumn();
            DataColumn column2 = new DataColumn();
            DataColumn column3 = new DataColumn();
            DataColumn column4 = new DataColumn();
            column1.DataType = System.Type.GetType("System.String");
            column2.DataType = System.Type.GetType("System.String");
            column3.DataType = System.Type.GetType("System.String");
            column3.DataType = System.Type.GetType("System.String");
            column1.ColumnName = "ID";
            column2.ColumnName = "Title";
            column3.ColumnName = "Description";
            column4.ColumnName = "Image";
            dataTable.Columns.Add(column1);
            dataTable.Columns.Add(column2);
            dataTable.Columns.Add(column3);
            dataTable.Columns.Add(column4);

            /* Settings data */

            // Saved settings flag
            bool prevSettingsData = false;

            // Check for settings file
            if (!File.Exists(this.settingsDataPath))
            {
                // Create new settings file
                this.SaveSettingsFile(this.settingsDataPath, new SettingsData());

                // Center form
                this.CenterToScreen();
            }
            else
            {
                // Set flag
                prevSettingsData = true;
            }

            // Load settings from disk
            this.settingsData = this.LoadSettingsFile(this.settingsDataPath);

            // Check for previous settings
            if (prevSettingsData)
            {
                // Set values
                this.directoryTextBox.Text = this.settingsData.Directory;
                this.Location = this.settingsData.Location;
                this.Size = this.settingsData.Size;
            }

            // Check radio button
            switch (this.settingsData.SortRadioButton)
            {
                case "rawRadioButton":
                    this.rawRadioButton.Checked = true;

                    break;

                case "idRadioButton":
                    this.idRadioButton.Checked = true;

                    break;

                case "titleRadioButton":
                    this.titleRadioButton.Checked = true;

                    break;

                case "descriptionRadioButton":
                    this.descriptionRadioButton.Checked = true;

                    break;
            }

            // Desc
            this.descCheckBox.Checked = this.settingsData.Desc;
        }

        /// <summary>
        /// Sorteds the data table to list box.
        /// </summary>
        private void SortedDataTableToListBox()
        {
            // Check for datatable rows
            if (this.dataTable.Rows.Count == 0)
            {
                // Halt flow
                return;
            }

            // Prevent drawing
            this.searchListBox.BeginUpdate();

            // Clear list box
            this.searchListBox.Items.Clear();

            // Set sorted datatable
            DataTable sortedDataTable = this.dataTable.Clone();

            sortedDataTable.Columns["ID"].DataType = System.Type.GetType("System.String");
            sortedDataTable.Columns["Title"].DataType = System.Type.GetType("System.String");
            sortedDataTable.Columns["Description"].DataType = System.Type.GetType("System.String");

            // Check for desc
            if (this.descCheckBox.Checked)
            {
                for (int i = this.dataTable.Rows.Count - 1; i >= 0; i--)
                {
                    sortedDataTable.ImportRow(this.dataTable.Rows[i]);
                }
            }
            else
            {
                foreach (DataRow dataRow in this.dataTable.Rows)
                {
                    sortedDataTable.ImportRow(dataRow);
                }
            }

            sortedDataTable.AcceptChanges();

            // Set data view
            DataView dataView = sortedDataTable.DefaultView;

            // Sort data table
            if (this.settingsData.SortRadioButton != "rawRadioButton")
            {
                // Sort by radio button
                switch (this.settingsData.SortRadioButton)
                {
                    case "idRadioButton":
                        dataView.Sort = $"ID{(this.descCheckBox.Checked ? " DESC" : string.Empty)}";

                        break;

                    case "descriptionRadioButton":
                        dataView.Sort = $"Description{(this.descCheckBox.Checked ? " DESC" : string.Empty)}";

                        break;

                    case "titleRadioButton":
                        dataView.Sort = $"Title{(this.descCheckBox.Checked ? " DESC" : string.Empty)}";

                        break;
                }
            }

            sortedDataTable = dataView.ToTable();

            // Data table to list box
            for (int i = 0; i < sortedDataTable.Rows.Count; i++)
            {
                // Add item
                this.searchListBox.Items.Add($"{(this.settingsData.HideIdsInList ? string.Empty : $"{ sortedDataTable.Rows[i]["ID"]} ")}{sortedDataTable.Rows[i]["Title"]} {sortedDataTable.Rows[i]["Description"]}");
            }

            // Resume drawing
            this.searchListBox.EndUpdate();
        }

        /// <summary>
        /// Handles the fetch button click.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private async void OnFetchButtonClick(object sender, EventArgs e)
        {
            // Something to work with
            if (this.searchTextBox.Text.Length == 0)
            {
                MessageBox.Show("Please enter a search term.", "Search", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                this.searchTextBox.Focus();

                return;
            }

            // Valid directory
            if (!Directory.Exists(this.directoryTextBox.Text))
            {
                MessageBox.Show("Target save directory must exist.", "Directory", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                this.directoryTextBox.Focus();

                return;
            }

            // Empty api key
            if (this.settingsData.ApiKey.Length == 0)
            {
                MessageBox.Show($"Please set API key.{Environment.NewLine}(Tools/Settings/API key)", "Set www.omdbapi.com key", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                return;
            }

            // Set async omdb client once
            if (this.omdb == null)
            {
                // New client
                this.omdb = new AsyncOmdbClient(this.settingsData.ApiKey);
            }

            // Disable
            this.searchTextBox.Enabled = false;
            this.fetchButton.Enabled = false;
            this.searchListBox.Enabled = false;

            // Set variables
            this.directory = this.directoryTextBox.Text;

            // Reset
            this.infoRichTextBox.Clear();
            this.imagePictureBox.Image = null;
            this.searchListBox.Items.Clear();
            this.linksRichTextBox.Clear();

            // Advise user
            this.resultToolStripStatusLabel.Text = $"Searching for: \"{this.searchTextBox.Text}\"...";

            try
            {
                // Clear data table
                this.dataTable.Rows.Clear();

                /* Search pages */

                for (int i = 0; i < this.settingsData.SearchPages; i++)
                {
                    // Set page number
                    int pageNumber = (i + 1);

                    SearchList searchList = await omdb.GetSearchListAsync(this.searchTextBox.Text, pageNumber);

                    /* Extract fields */

                    foreach (var result in searchList.SearchResults)
                    {
                        DataRow row = this.dataTable.NewRow();

                        /* Set values */

                        row["ID"] = result.ImdbId;
                        row["Title"] = result.Title;
                        row["Description"] = $"{result.Year} ({result.Type})";
                        row["Image"] = result.Poster;

                        // Add to data table
                        this.dataTable.Rows.Add(row);
                    }

                    // Populate list box
                    this.SortedDataTableToListBox();

                    // Advise user
                    this.resultToolStripStatusLabel.Text = $"Fetched search page #{pageNumber}...";

                    // Update api calls count
                    this.UpdateApiCalls();

                    // Check for pagination end
                    if (searchList.SearchResults.Count() < 10)
                    {
                        // Exit loop
                        break;
                    }
                }

                // Advise user
                this.resultToolStripStatusLabel.Text = "Please click a result to process";

                // Update fetched count
                this.fetchedCount++;
                this.fetchedCountToolStripStatusLabel.Text = this.fetchedCount.ToString();
            }
            catch (Exception ex)
            {
                // Log error
                this.LogError($"Search exception message:{Environment.NewLine}{ex.Message}{Environment.NewLine}{ex.StackTrace}", "Exception while searching. Please retry.");
            }

            // Enable
            this.searchTextBox.Enabled = true;
            this.fetchButton.Enabled = true;
            this.searchListBox.Enabled = true;
        }

        /// <summary>
        /// Handles the search list box selected index changed.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private async void OnSearchListBoxSelectedIndexChanged(object sender, EventArgs e)
        {
            // Ensure there's a selected item
            if (this.searchListBox.SelectedIndex == -1)
            {
                return;
            }

            /* Download info */

            // Disable
            this.searchListBox.Enabled = false;

            // Resets
            this.imagePictureBox.Image = null;
            this.infoRichTextBox.Clear();
            this.linksRichTextBox.Clear();

            try
            {
                // Get ID and title dictionary
                var selectedItemIdAndTitle = GetSelectedItemIdAndTitle();

                // Set vars
                var id = selectedItemIdAndTitle["ID"];
                var title = selectedItemIdAndTitle["Title"];

                // Set data row
                DataRow[] dataRowArray = this.dataTable.Select($"ID = '{id}'");

                // Update status
                this.resultToolStripStatusLabel.Text = $"Downloading info for: \"{title.Substring(0, Math.Min(title.Length, 25))}\"...";

                // Set item by id, with full plot
                Item item = await omdb.GetItemByIdAsync(id, this.fullPlotToolStripMenuItem.Checked);

                // TODO Check for error [Legacy. Check if needed]
                if (item.Title.Length == 0)
                {
                    // Halt flow
                    throw new Exception($"Error when searching for \"{searchTextBox.Text}\"");
                }

                StringBuilder infoStringBuilder = new StringBuilder();

                Type myType = item.GetType();
                IList<PropertyInfo> props = new List<PropertyInfo>(myType.GetProperties());

                foreach (PropertyInfo prop in props)
                {
                    try
                    {
                        object propValue = prop.GetValue(item, null);

                        if (propValue.ToString().Length > 0)
                        {
                            // Populate info string builder
                            switch (prop.Name)
                            {
                                // Skips
                                case "Response":
                                    break;

                                // Ratings
                                case "Ratings":

                                    infoStringBuilder.AppendLine($"{prop.Name}");

                                    foreach (var rating in item.Ratings)
                                    {
                                        infoStringBuilder.AppendLine($"{rating.Source}: {rating.Value}");
                                    }

                                    infoStringBuilder.AppendLine();

                                    break;

                                // Everything else
                                default:

                                    infoStringBuilder.AppendLine($"{prop.Name}: {propValue}");
                                    infoStringBuilder.AppendLine();
                                    break;
                            }


                        }
                    }
                    catch (Exception ex)
                    {
                        // Let it fall through
                    }
                }

                // Populate 
                this.infoRichTextBox.Text = infoStringBuilder.ToString();

                // Set links rich text box
                this.linksRichTextBox.Text = $"IMDB:{Environment.NewLine}https://www.imdb.com/title/{item.ImdbId}/{Environment.NewLine}{Environment.NewLine}Poster:{Environment.NewLine}{item.Poster}";

                // Focus links tab page
                this.logTabControl.SelectedTab = this.linksTabPage;

                /* Image */

                // Update status
                this.resultToolStripStatusLabel.Text = $"Downloading image for: \"{title.Substring(0, Math.Min(title.Length, 25))}\"...";

                //  Set image uri
                var imageUri = new Uri(dataRowArray[0]["Image"].ToString());

                // Set image file path
                this.imageFilePath = Path.Combine(this.directory, this.GetValidFilePathName($"{title}{Path.GetExtension(dataRowArray[0]["Image"].ToString())}"));

                // Fetch image
                using (WebClient webClient = new WebClient())
                {
                    // No proxy
                    webClient.Proxy = null;

                    // Download image to file
                    await webClient.DownloadFileTaskAsync(imageUri, this.imageFilePath);
                }

                // Try to load
                if (File.Exists(this.imageFilePath))
                {
                    // Load picture
                    this.imagePictureBox.Image = Image.FromFile(this.imageFilePath);

                    // Advise user
                    this.resultToolStripStatusLabel.Text = $"Info and image downloaded.";
                }
                else
                {
                    // Throw an exception
                    throw new Exception($"Error when downloading image for: \"{title.Substring(0, Math.Min(title.Length, 25))}\"...");
                }
            }
            catch (Exception ex)
            {
                // Log
                this.LogError($"Info + image exception message:{Environment.NewLine}{ex.Message}{Environment.NewLine}{ex.Message}", $"Exception while fetching info + image. Please retry.");
            }

            // Update api calls count
            this.UpdateApiCalls();

            // Enable
            this.searchListBox.Enabled = true;
        }

        /// <summary>
        /// Gets the selected item identifier and title.
        /// </summary>
        /// <returns>The selected item identifier and title.</returns>
        private Dictionary<string, string> GetSelectedItemIdAndTitle()
        {
            Dictionary<string, string> returnDictionary = new Dictionary<string, string>();

            // Set selected item 
            var searchListBoxSelectedItem = this.searchListBox.SelectedItem.ToString();

            // Check if there's an ID
            if (this.settingsData.HideIdsInList)
            {
                // Select
                var rows = from row in this.dataTable.AsEnumerable()
                           where $"{row.Field<string>("Title")} {row.Field<string>("Description")}" == searchListBoxSelectedItem
                           select row.Field<string>("ID");

                // Set into return dictionary
                returnDictionary.Add("ID", rows.First());
                returnDictionary.Add("Title", searchListBoxSelectedItem);
            }
            else
            {
                // Split list item
                var item = searchListBoxSelectedItem.Split(new char[] { ' ' }, 2);

                returnDictionary.Add("ID", item[0]);
                returnDictionary.Add("Title", item[1]);
            }

            return returnDictionary;
        }

        /// <summary>
        /// Updates the API calls.
        /// </summary>
        private void UpdateApiCalls()
        {
            // Raise
            this.localApiCalls++;

            // TODO Update properly
            this.apiCallsCountToolStripStatusLabel.Text = this.localApiCalls.ToString();
        }

        /// <summary>
        /// Handles the browse button click.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnBrowseButtonClick(object sender, EventArgs e)
        {
            this.folderBrowserDialog.SelectedPath = string.Empty;

            if (this.folderBrowserDialog.ShowDialog() == DialogResult.OK && this.folderBrowserDialog.SelectedPath.Length > 0)
            {
                this.directoryTextBox.Text = this.folderBrowserDialog.SelectedPath;
            }
        }

        /// <summary>
        /// Handles the radio button checked changed.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnRadioButtonCheckedChanged(object sender, EventArgs e)
        {
            // Save to settings data
            this.settingsData.SortRadioButton = ((RadioButton)sender).Name;

            // Refresh list box
            this.SortedDataTableToListBox();
        }

        /// <summary>
        /// Handles the desc check box checked changed.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnDescCheckBoxCheckedChanged(object sender, EventArgs e)
        {
            // Save to settings data
            this.settingsData.Desc = this.descCheckBox.Checked;

            // Refresh list box
            this.SortedDataTableToListBox();
        }

        /// <summary>
        /// Handles the APIK ey tool strip menu item click.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnAPIKeyToolStripMenuItemClick(object sender, EventArgs e)
        {
            // Get raw value
            string userValue = Interaction.InputBox("API key from omdbapi.com", "Edit key", this.settingsData.ApiKey);

            // Check length
            if (userValue.Length == 0)
            {
                // Halt flow
                return;
            }

            try
            {
                // TODO Validate API key

                // Set API key
                this.settingsData.ApiKey = userValue;
            }
            catch (Exception ex)
            {
                // Advise user
                MessageBox.Show($"API key format error:{Environment.NewLine}{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Handles the search pages tool strip menu item click.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnSearchPagesToolStripMenuItemClick(object sender, EventArgs e)
        {
            // Get raw value
            string userValue = Interaction.InputBox("Search results pages (10 items per page)", "Results pagination", this.settingsData.SearchPages.ToString());

            // Check length
            if (userValue.Length == 0)
            {
                // Halt flow
                return;
            }

            try
            {
                // Parse
                if (int.TryParse(userValue, out int pages))
                {
                    // Correct range
                    if (pages == 0)
                    {
                        pages = 1;
                    }
                    else if (pages > 100)
                    {
                        pages = 100;
                    }

                    // Set API key
                    this.settingsData.SearchPages = pages;
                }
            }
            catch (Exception ex)
            {
                // Advise user
                MessageBox.Show($"Search pages edit error:{Environment.NewLine}{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Handles the options tool strip menu item drop down item clicked.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnOptionsToolStripMenuItemDropDownItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            // Set tool strip menu item
            ToolStripMenuItem toolStripMenuItem = (ToolStripMenuItem)e.ClickedItem;

            // Toggle checked
            toolStripMenuItem.Checked = !toolStripMenuItem.Checked;

            // Set topmost by check box
            this.TopMost = this.alwaysOnTopToolStripMenuItem.Checked;

            /* Settings data */

            // ALways on top
            this.settingsData.AlwaysOnTop = this.alwaysOnTopToolStripMenuItem.Checked;

            // Hide IDs in list
            this.settingsData.HideIdsInList = this.hideIDsInListToolStripMenuItem.Checked;

            // Full plot
            this.settingsData.FullPlot = this.fullPlotToolStripMenuItem.Checked;

            // Check if it's a Hide IDs toggle
            if (toolStripMenuItem.Name == this.hideIDsInListToolStripMenuItem.Name)
            {
                // Update list box
                this.SortedDataTableToListBox();
            }
        }

        /// <summary>
        /// Handles the new tool strip menu item click.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnNewToolStripMenuItemClick(object sender, EventArgs e)
        {
            this.searchTextBox.Clear();

            this.searchListBox.Items.Clear();

            this.fetchedCount = 0;
            this.fetchedCountToolStripStatusLabel.Text = "0";

            this.infoRichTextBox.Clear();

            this.imagePictureBox.Image = null;

            this.searchTextBox.Focus();

            this.resultToolStripStatusLabel.Text = "Waiting for a search term to fetch...";
        }

        /// <summary>
        /// Handles the free releases public domain is tool strip menu item click.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnFreeReleasesPublicDomainIsToolStripMenuItemClick(object sender, EventArgs e)
        {
            // Open our website
            Process.Start("https://publicdomain.is");
        }

        /// <summary>
        /// Handles the original thread donation codercom tool strip menu item click.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnOriginalThreadDonationCodercomToolStripMenuItemClick(object sender, EventArgs e)
        {
            // Open orignal thread
            Process.Start("https://www.donationcoder.com/forum/index.php?topic=52602.0");
        }

        /// <summary>
        /// Handles the source code githubcom tool strip menu item click.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnSourceCodeGithubcomToolStripMenuItemClick(object sender, EventArgs e)
        {
            // Open GitHub repository
            Process.Start("https://github.com/publicdomain/omdbfetch");
        }

        /// <summary>
        /// Ons the get API Key tool strip menu item click.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnGetAPIKeyToolStripMenuItemClick(object sender, EventArgs e)
        {
            // Open API Key page
            Process.Start("http://www.omdbapi.com/apikey.aspx");
        }

        /// <summary>
        /// Handles the about tool strip menu item click.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnAboutToolStripMenuItemClick(object sender, EventArgs e)
        {
            // Set license text
            var licenseText = $"CC0 1.0 Universal (CC0 1.0) - Public Domain Dedication{Environment.NewLine}" +
                $"https://creativecommons.org/publicdomain/zero/1.0/legalcode{Environment.NewLine}{Environment.NewLine}" +
                $"Libraries and icons have separate licenses.{Environment.NewLine}{Environment.NewLine}" +
                $"OMDb-API.NET library by Torben Wagner - MIT License{Environment.NewLine}" +
                $"https://github.com/torben-w/OMDb-API.NET{Environment.NewLine}{Environment.NewLine}" +
                $"Newtonsoft.Json library by James Newton-King - MIT License{Environment.NewLine}" +
                $"https://github.com/JamesNK/Newtonsoft.Json{Environment.NewLine}{Environment.NewLine}" +
                $"Clapboard icon by mariananbu - Pixabay License{Environment.NewLine}" +
                $"https://pixabay.com/vectors/clapboard-clapper-movie-film-4489924/{Environment.NewLine}{Environment.NewLine}" +
                $"Patreon icon used according to published brand guidelines{Environment.NewLine}" +
                $"https://www.patreon.com/brand{Environment.NewLine}{Environment.NewLine}" +
                $"GitHub mark icon used according to published logos and usage guidelines{Environment.NewLine}" +
                $"https://github.com/logos{Environment.NewLine}{Environment.NewLine}" +
                $"DonationCoder icon used with permission{Environment.NewLine}" +
                $"https://www.donationcoder.com/forum/index.php?topic=48718{Environment.NewLine}{Environment.NewLine}" +
                $"PublicDomain icon is based on the following source images:{Environment.NewLine}{Environment.NewLine}" +
                $"Bitcoin by GDJ - Pixabay License{Environment.NewLine}" +
                $"https://pixabay.com/vectors/bitcoin-digital-currency-4130319/{Environment.NewLine}{Environment.NewLine}" +
                $"Letter P by ArtsyBee - Pixabay License{Environment.NewLine}" +
                $"https://pixabay.com/illustrations/p-glamour-gold-lights-2790632/{Environment.NewLine}{Environment.NewLine}" +
                $"Letter D by ArtsyBee - Pixabay License{Environment.NewLine}" +
                $"https://pixabay.com/illustrations/d-glamour-gold-lights-2790573/{Environment.NewLine}{Environment.NewLine}";

            // Prepend sponsors
            licenseText = $"RELEASE SPONSORS:{Environment.NewLine}{Environment.NewLine}* Jesse Reichler{Environment.NewLine}{Environment.NewLine}=========={Environment.NewLine}{Environment.NewLine}" + licenseText;

            // Set title
            string programTitle = typeof(MainForm).GetTypeInfo().Assembly.GetCustomAttribute<AssemblyTitleAttribute>().Title;

            // Set version for generating semantic version
            Version version = typeof(MainForm).GetTypeInfo().Assembly.GetName().Version;

            // Set about form
            var aboutForm = new AboutForm(
                $"About {programTitle}",
                $"{programTitle} {version.Major}.{version.Minor}.{version.Build}",
                $"Made for: Mouser{Environment.NewLine}DonationCoder.com{Environment.NewLine}Day #249, Week #36 @ September 06, 2022",
                licenseText,
                this.Icon.ToBitmap())
            {
                // Set about form icon
                Icon = this.associatedIcon,

                // Set always on top
                TopMost = this.TopMost
            };

            // Show about form
            aboutForm.ShowDialog();
        }

        /// <summary>
        /// Handles the directory text box drag drop.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnDirectoryTextBoxDragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var possibleDirectory = (string[])e.Data.GetData(DataFormats.FileDrop);

                if (Directory.Exists(possibleDirectory[0]))
                {
                    this.directoryTextBox.Text = possibleDirectory[0];
                }
            }
        }

        /// <summary>
        /// Handles the directory text box drag enter.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnDirectoryTextBoxDragEnter(object sender, DragEventArgs e)
        {
            e.Effect = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        }

        /// <summary>
        /// Handles the image picture box mouse move.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnImagePictureBoxMouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                var files = new string[] { this.imageFilePath };

                this.imagePictureBox.DoDragDrop(new DataObject(DataFormats.FileDrop, files), DragDropEffects.Copy | DragDropEffects.Move);
            }
        }

        /// <summary>
        /// Gets the name of the valid file path.
        /// </summary>
        /// <returns>The valid file path name.</returns>
        /// <param name="rawName">Raw name.</param>
        private string GetValidFilePathName(string rawName)
        {
            var invalidCharList = new List<char>();

            invalidCharList.AddRange(Path.GetInvalidFileNameChars());

            invalidCharList.AddRange(Path.GetInvalidPathChars());

            foreach (var c in invalidCharList)
            {
                rawName = rawName.Replace(c.ToString(), string.Empty);
            }

            // Return processed raw name variable
            return rawName;
        }

        /// <summary>
        /// Handles the main form load.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnMainFormLoad(object sender, EventArgs e)
        {
            // Set topmost
            this.TopMost = this.settingsData.AlwaysOnTop;

            // Focus search text box
            this.searchTextBox.Focus();

            // GUI
            this.alwaysOnTopToolStripMenuItem.Checked = this.settingsData.AlwaysOnTop;
            this.hideIDsInListToolStripMenuItem.Checked = this.settingsData.HideIdsInList;
            this.fullPlotToolStripMenuItem.Checked = this.settingsData.FullPlot;
        }

        /// <summary>
        /// Handles the main form form closing.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnMainFormFormClosing(object sender, FormClosingEventArgs e)
        {
            /* Setiings data values */

            // Set values
            this.settingsData.Directory = this.directoryTextBox.Text;
            this.settingsData.Location = this.Location;
            this.settingsData.Size = this.Size;

            // Save settings data to disk
            this.SaveSettingsFile(this.settingsDataPath, this.settingsData);
        }

        /// <summary>
        /// Loads the settings file.
        /// </summary>
        /// <returns>The settings file.</returns>
        /// <param name="settingsFilePath">Settings file path.</param>
        private SettingsData LoadSettingsFile(string settingsFilePath)
        {
            // Use file stream
            using (FileStream fileStream = File.OpenRead(settingsFilePath))
            {
                // Set xml serialzer
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(SettingsData));

                // Return populated settings data
                return xmlSerializer.Deserialize(fileStream) as SettingsData;
            }
        }

        /// <summary>
        /// Saves the settings file.
        /// </summary>
        /// <param name="settingsFilePath">Settings file path.</param>
        /// <param name="settingsDataParam">Settings data parameter.</param>
        private void SaveSettingsFile(string settingsFilePath, SettingsData settingsDataParam)
        {
            try
            {
                // Use stream writer
                using (StreamWriter streamWriter = new StreamWriter(settingsFilePath, false))
                {
                    // Set xml serialzer
                    XmlSerializer xmlSerializer = new XmlSerializer(typeof(SettingsData));

                    // Serialize settings data
                    xmlSerializer.Serialize(streamWriter, settingsDataParam);
                }
            }
            catch (Exception exception)
            {
                // Advise user
                MessageBox.Show($"Error saving settings file.{Environment.NewLine}{Environment.NewLine}Message:{Environment.NewLine}{exception.Message}", "File error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Logs the error.
        /// </summary>
        /// <param name="errorMessage">Error message.</param>
        /// <param name="statusMessage">Status message.</param>
        private void LogError(string errorMessage, string statusMessage)
        {
            try
            {
                // Focus error tab page
                this.logTabControl.SelectedTab = this.errorlogTabPage;

                // Set in text box 
                this.errorLogTextBox.Text = errorMessage;

                // Log to file
                File.AppendAllText(this.errorLogPath, $"{Environment.NewLine}{Environment.NewLine}{DateTime.Now}{Environment.NewLine}{errorMessage}");

                // Advise user
                this.resultToolStripStatusLabel.Text = statusMessage;
            }
            catch (Exception ex)
            {
                // Advise user
                this.resultToolStripStatusLabel.Text = "Error when logging.";
            }
        }

        /// <summary>
        /// Handles the links rich text box link clicked.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnLinksRichTextBoxLinkClicked(object sender, LinkClickedEventArgs e)
        {
            // Uri
            var uri = new Uri(e.LinkText);

            // Validate url 
            if (uri.IsWellFormedOriginalString())
            {
                // Launch with default browser
                Process.Start(uri.ToString());
            }
        }

        /// <summary>
        /// Handles the image picture box double click.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnImagePictureBoxDoubleClick(object sender, EventArgs e)
        {
            // Check for image file
            if (this.imageFilePath.Length > 0 && File.Exists(this.imageFilePath))
            {
                // Open image
                Process.Start(this.imageFilePath);
            }
        }

        /// <summary>
        /// Handles the browse image directory tool strip menu item click.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnBrowseImageDirectoryToolStripMenuItemClick(object sender, EventArgs e)
        {
            // Check for a vaild directory
            if (this.directoryTextBox.Text.Length > 0 && Directory.Exists(this.directoryTextBox.Text))
            {
                // Open image directory
                Process.Start(this.directoryTextBox.Text);
            }
        }

        /// <summary>
        /// Handles the exit tool strip menu item click.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnExitToolStripMenuItemClick(object sender, EventArgs e)
        {
            // Close program        
            this.Close();
        }
    }
}
