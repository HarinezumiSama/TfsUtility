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
using TfsUtil.Properties;

namespace TfsUtil
{
    /// <summary>
    ///     Contains interaction logic for MainWindow.xaml.
    /// </summary>
    public partial class MainWindow : Window
    {
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

        #region Fields

        private readonly MainWindowModel m_model;

        #endregion

        #region Constructors

        public MainWindow()
        {
            InitializeComponent();

            m_model = new MainWindowModel();
            this.DataContext = m_model;

            this.RefreshTfsServers();
        }

        #endregion

        #region Private Methods

        #region Regular

        private void RefreshTfsServers()
        {
            this.ServerMenu.Items.Clear();

            var tfsCollections = RegisteredTfsConnections
                .GetProjectCollections()
                .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(item => item.Uri)
                .ToList();

            var isChecked = true;
            MenuItem firstItem = null;
            foreach (var tfsCollection in tfsCollections)
            {
                var serverItem = new MenuItem()
                {
                    Header = tfsCollection.Name,
                    ToolTip = tfsCollection.Uri.AbsoluteUri,
                    IsCheckable = true,
                    IsChecked = isChecked,
                    Tag = tfsCollection
                };
                serverItem.Click += this.ServerItem_Click;

                isChecked = false;
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
            if (menuItem == null)
            {
                m_model.TfsServerUri = null;
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
                m_model.TfsServerUri = null;
                return;
            }

            menuItem.IsChecked = true;
            m_model.TfsServerUri = tfsCollection.Uri;
        }

        private bool HandleException(Exception exception, bool writeToLog)
        {
            if (exception == null)
            {
                return false;
            }

            MessageBox.Show(
                this,
                string.Format(
                    "An error occurred:{0}"
                        + "[{1}] {2}",
                    Environment.NewLine,
                    exception.GetType().FullName,
                    exception.Message),
                this.Title,
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            if (writeToLog)
            {
                const string separatorFormat = "*****{0}";

                logBox.AppendText(
                    string.Format(
                        "{0}"
                            + separatorFormat
                            + "[ERROR]{0}"
                            + "[{1}] {2}{0}"
                            + separatorFormat
                            + "{0}",
                        Environment.NewLine,
                        exception.GetType().FullName,
                        exception.Message));
            }

            return true;
        }

        private void DoExecuteMergeSearch(object sender, ExecutedRoutedEventArgs e)
        {
            var mergeSearchWindow = new MergeSearchWindow(m_model.TfsServerUri) { Owner = this };
            mergeSearchWindow.ShowDialog();
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
                if (!HandleException(ex, true))
                {
                    throw;
                }
            }
        }

        private void CanExecuteMergeSearch(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = m_model.TfsServerUri != null;
        }

        private void ServerItem_Click(object sender, RoutedEventArgs e)
        {
            SelectServer(e.Source as MenuItem);
        }

        #endregion

        #endregion
    }
}