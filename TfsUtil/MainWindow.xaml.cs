using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Framework.Client;
using Microsoft.TeamFoundation.Framework.Common;
using Microsoft.TeamFoundation.VersionControl.Client;
using TfsUtil.Commands;
using TfsUtil.Controls;
using TfsUtil.Properties;

namespace TfsUtil
{
    // TODO: [VM] Implement MDI-like (New Merge Search menu item creates a new `MDI` window)

    /// <summary>
    ///     Contains interaction logic for MainWindow.xaml.
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Fields

        private readonly MainWindowModel _model;

        #endregion

        #region Constructors

        public MainWindow()
        {
            InitializeComponent();

            this.Title = App.Current.ProductName;

            _model = new MainWindowModel();
            this.DataContext = _model;

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
                var serverItem = new MenuItem()
                {
                    Header = tfsCollection.Name,
                    ToolTip = tfsCollection.Uri.AbsoluteUri,
                    IsCheckable = false,  // Should not be checked/unchecked automatically
                    Tag = tfsCollection
                };
                serverItem.Click += this.ServerItem_Click;

                firstItem = firstItem ?? serverItem;

                this.ServerMenu.Items.Add(serverItem);
            }

            SelectServer(firstItem);

            if (!this.ServerMenu.HasItems)
            {
                this.ServerMenu.Items.Add(
                    new MenuItem()
                    {
                        Header = "No registered servers found.",
                        IsEnabled = false
                    });
            }
        }

        private void SelectServer(MenuItem menuItem)
        {
            if (HasCurrentContent())
            {
                var answer = this.ShowMessageBox(
                    "If you change the current server, the content will be closed. Continue?",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
                if (answer != MessageBoxResult.Yes)
                {
                    return;
                }

                ClearCurrentContent();
            }

            if (menuItem == null)
            {
                _model.TfsServerUri = null;
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

            var tfsCollection = menuItem.Tag as RegisteredProjectCollection;
            if (tfsCollection == null)
            {
                _model.TfsServerUri = null;
                return;
            }

            menuItem.IsChecked = true;
            _model.TfsServerUri = tfsCollection.Uri;
        }

        private bool HasCurrentContent()
        {
            return this.CurrentContent.Content != null;
        }

        private void SetCurrentContent(object content)
        {
            this.CurrentContent.Content = content;
        }

        private void ClearCurrentContent()
        {
            SetCurrentContent(null);
        }

        private void DoExecuteMergeSearch(object sender, ExecutedRoutedEventArgs e)
        {
            SetCurrentContent(new MergeSearchControl(_model.TfsServerUri));
        }

        #endregion

        #region Event Handlers

        private void ExecuteMergeSearch(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                DoExecuteMergeSearch(sender, e);
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
            e.CanExecute = _model.TfsServerUri != null && !(this.CurrentContent.Content is MergeSearchControl);
        }

        private void ServerItem_Click(object sender, RoutedEventArgs e)
        {
            SelectServer(e.Source as MenuItem);
        }

        private void CanCloseActiveContent(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = HasCurrentContent();
        }

        private void ExecuteCloseActiveContent(object sender, ExecutedRoutedEventArgs e)
        {
            ClearCurrentContent();
        }

        #endregion

        #endregion

        #region Nested Types

        #region MainWindowModel Class

        private sealed class MainWindowModel
        {
            #region Constructors

            internal MainWindowModel()
            {
                // Nothing to do
            }

            #endregion

            #region Public Properties

            public Uri TfsServerUri
            {
                get;
                set;
            }

            #endregion
        }

        #endregion

        #endregion
    }
}