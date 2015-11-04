using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.TeamFoundation.Client;
using TfsUtil.Controls;
using TfsUtil.Wrappers;

namespace TfsUtil
{
    /// <summary>
    ///     Contains interaction logic for <c>MainWindow.xaml</c>.
    /// </summary>
    public partial class MainWindow
    {
        #region Constructors

        public MainWindow()
        {
            InitializeComponent();

            if (DesignerProperties.GetIsInDesignMode(this))
            {
                return;
            }

            this.Title = App.Current.ProductName;
            this.RefreshTfsServers();
        }

        #endregion

        #region Private Methods

        #region Regular

        private bool HandleException(Exception exception)
        {
            if (exception == null)
            {
                return false;
            }

            this.ShowMessageBox(
                string.Format(
                    "An error occurred:{0}"
                        + "[{1}] {2}",
                    Environment.NewLine,
                    exception.GetType().FullName,
                    exception.Message),
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            return true;
        }

        private void RefreshTfsServers()
        {
            //// TODO: [VM] Allow user to enter custom server URI

            this.ServerMenu.Items.Clear();

            var tfsCollections = RegisteredTfsConnections
                .GetProjectCollections()
                .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(item => item.Uri)
                .ToList();

            MenuItem firstItem = null;
            foreach (var tfsCollection in tfsCollections)
            {
                var serverItem = new MenuItem
                {
                    Header = tfsCollection.Name,
                    ToolTip = tfsCollection.Uri.AbsoluteUri,
                    IsCheckable = false,  // Should not be checked/unchecked automatically
                    Tag = new TfsServerInfo(tfsCollection)
                };

                serverItem.Click += this.ServerItem_Click;

                firstItem = firstItem ?? serverItem;

                this.ServerMenu.Items.Add(serverItem);
            }

            SelectServer(firstItem);

            if (!this.ServerMenu.HasItems)
            {
                this.ServerMenu.Items.Add(
                    new MenuItem
                    {
                        Header = "No registered servers found.",
                        IsEnabled = false
                    });
            }
        }

        private void SelectServer(MenuItem menuItem)
        {
            if (menuItem == null)
            {
                this.ViewModel.SelectedServer = null;
                return;
            }

            var oldSelectedServerItem = this
                .ServerMenu
                .Items
                .OfType<MenuItem>()
                .SingleOrDefault(item => item.IsChecked);

            if (oldSelectedServerItem != null)
            {
                oldSelectedServerItem.IsChecked = false;
            }

            var serverInfo = menuItem.Tag as TfsServerInfo;
            if (serverInfo == null)
            {
                this.ViewModel.SelectedServer = null;
                return;
            }

            menuItem.IsChecked = true;
            this.ViewModel.SelectedServer = serverInfo;
        }

        private bool HasAnyContentTab()
        {
            return this.ContentTabs.SelectedItem is TabItem;
        }

        private void AddContentTab(TabContentControl tabContentControl)
        {
            if (this.ViewModel.SelectedServer == null)
            {
                throw new InvalidOperationException("The TFS server is not selected.");
            }

            tabContentControl.EnsureNotNull();

            var dataContext = new ContentTabViewModel
            {
                HeaderText = string.Format(
                    CultureInfo.InvariantCulture,
                    @"{0} @ {1}",
                    tabContentControl.Header,
                    this.ViewModel.SelectedServer.Name),
                TfsServerUri = this.ViewModel.SelectedServer.Uri
            };

            var contentTab = new TabItem
            {
                Content = tabContentControl,
                DataContext = dataContext
            };

            this.ContentTabs.Items.Add(contentTab);
            this.ContentTabs.SelectedItem = contentTab;
        }

        private void RemoveContentTab(TabItem contentTab)
        {
            if (contentTab == null)
            {
                return;
            }

            this.ContentTabs.Items.Remove(contentTab);
        }

        private void DoExecuteMergeSearch()
        {
            AddContentTab(new MergeSearchControl(this.ViewModel.SelectedServer));
        }

        #endregion

        #region Event Handlers

        private void ExecuteMergeSearch(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                DoExecuteMergeSearch();
            }
            catch (Exception ex)
            {
                if (!HandleException(ex))
                {
                    throw;
                }
            }
        }

        private void CanExecuteMergeSearch(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = this.ViewModel.SelectedServer != null;
        }

        private void ServerItem_Click(object sender, RoutedEventArgs e)
        {
            SelectServer(e.Source as MenuItem);
        }

        private void CanCloseActiveContent(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = HasAnyContentTab();
        }

        private void ExecuteCloseActiveContent(object sender, ExecutedRoutedEventArgs e)
        {
            var contentTab = e.Parameter as TabItem ?? this.ContentTabs.SelectedItem as TabItem;
            RemoveContentTab(contentTab);
        }

        #endregion

        #endregion
    }
}