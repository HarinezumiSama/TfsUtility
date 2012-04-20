using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
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
using System.Windows.Shapes;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace TfsUtil
{
    /// <summary>
    /// Interaction logic for MergeSearchWindow.xaml
    /// </summary>
    public partial class MergeSearchWindow : Window
    {
        #region Nested Types

        #region MergeSearchWindowModel Class

        private sealed class MergeSearchWindowModel : INotifyPropertyChanged
        {
            #region Fields

            private string m_sourceBranch;

            #endregion

            #region Constructors

            public MergeSearchWindowModel()
            {
                m_sourceBranch = string.Empty;

                this.SourceBranches = new List<ControlItem<ItemIdentifier>>();
                this.SourceBranchesView = CollectionViewSource.GetDefaultView(this.SourceBranches);
                this.SourceBranchesView.CurrentChanged += this.SourceBranchesView_CurrentChanged;

                this.TargetBranches = new List<ControlItem<ItemIdentifier>>();
                this.TargetBranchesView = CollectionViewSource.GetDefaultView(this.TargetBranches);
            }

            #endregion

            #region Private Methods

            private void RaisePropertyChanged(string propertyName)
            {
                if (string.IsNullOrEmpty(propertyName))
                {
                    return;
                }

                var propertyChanged = this.PropertyChanged;
                if (propertyChanged == null)
                {
                    return;
                }

                propertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }

            private void SourceBranchesView_CurrentChanged(object sender, EventArgs e)
            {
                RefreshTargetBranches();
            }

            #endregion

            #region Internal Properties

            internal List<ControlItem<ItemIdentifier>> SourceBranches
            {
                get;
                private set;
            }

            internal List<ControlItem<ItemIdentifier>> TargetBranches
            {
                get;
                private set;
            }

            #endregion

            #region Public Properties

            public Uri TfsServerUri
            {
                get;
                set;
            }

            public ICollectionView SourceBranchesView
            {
                get;
                private set;
            }

            public ICollectionView TargetBranchesView
            {
                get;
                private set;
            }

            public string SourceBranch
            {
                [DebuggerStepThrough]
                get
                {
                    return m_sourceBranch;
                }
                set
                {
                    var actualValue = value ?? string.Empty;
                    if (m_sourceBranch != actualValue)
                    {
                        m_sourceBranch = actualValue;
                        RaisePropertyChanged(Helper.GetPropertyName((MergeSearchWindowModel obj) => obj.SourceBranch));
                    }
                }
            }

            #endregion

            #region Public Methods

            public TfsWrapper CreateTfsWrapper()
            {
                return new TfsWrapper(this.TfsServerUri);
            }

            public void RefreshSourceBranches()
            {
                BranchObject[] branchObjects;
                using (var tfsWrapper = CreateTfsWrapper())
                {
                    branchObjects = tfsWrapper.VersionControlServer.QueryRootBranchObjects(RecursionType.Full);
                }

                var branchItems = branchObjects
                    .SelectMany(
                        item => item
                            .ChildBranches
                            .Concat(item.RelatedBranches)
                            .Concat(item.Properties.RootItem.AsCollection()))
                    .Select(item => item.Item)
                    .Distinct()
                    .OrderBy(item => item)
                    .Select(item => ControlItem.Create(new ItemIdentifier(item), item))
                    .ToArray();

                this.SourceBranches.ReplaceContents(branchItems);

                this.SourceBranchesView.Refresh();
                this.SourceBranchesView.MoveCurrentToFirst();

                RefreshTargetBranches();
            }

            public void RefreshTargetBranches()
            {
                this.TargetBranches.Clear();

                var sourceBranch = this.SourceBranch;

                var mergeRelationships = Enumerable.Empty<ItemIdentifier>();
                if (!string.IsNullOrWhiteSpace(sourceBranch))
                {
                    using (var tfsWrapper = CreateTfsWrapper())
                    {
                        var vcs = tfsWrapper.VersionControlServer;
                        if (vcs.ServerItemExistsSafe(sourceBranch, ItemType.Any))
                        {
                            mergeRelationships = vcs.QueryMergeRelationships(sourceBranch);
                        }
                    }
                }

                var targetBranches = mergeRelationships
                    .Select(item => ControlItem.Create(item, item.Item))
                    .ToArray();

                this.TargetBranches.ReplaceContents(targetBranches);

                this.TargetBranchesView.Refresh();
                this.TargetBranchesView.MoveCurrentToFirst();
            }

            #endregion

            #region INotifyPropertyChanged Members

            public event PropertyChangedEventHandler PropertyChanged;

            #endregion
        }

        #endregion

        #endregion

        #region Fields

        private readonly MergeSearchWindowModel m_model;

        #endregion

        #region Constructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="MergeSearchWindow"/> class.
        /// </summary>
        private MergeSearchWindow()
        {
            InitializeComponent();

            m_model = new MergeSearchWindowModel();
            this.DataContext = m_model;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="MergeSearchWindow"/> class.
        /// </summary>
        public MergeSearchWindow(Uri tfsServerUri)
            : this()
        {
            #region Argument Check

            if (tfsServerUri == null)
            {
                throw new ArgumentNullException("tfsServerUri");
            }

            #endregion

            m_model.TfsServerUri = tfsServerUri;
        }

        #endregion

        #region Private Methods

        #region Regular

        private void FillCurrentUserName()
        {
            var identity = WindowsIdentity.GetCurrent();
            this.UserNameTextBox.Text = identity == null ? string.Empty : identity.Name;
        }

        private void Initialize()
        {
            m_model.RefreshSourceBranches();
            FillCurrentUserName();
        }

        private void SetMergeCandidatesListViewBackText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                this.MergeCandidatesListView.Background = null;
                return;
            }

            this.MergeCandidatesListView.Background = new VisualBrush(
                new TextBlock()
                {
                    Text = text,
                    ToolTip = text,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextAlignment = TextAlignment.Center,
                    Margin = new Thickness(12d),
                    TextWrapping = TextWrapping.Wrap,
                    Width = this.MergeCandidatesListView.ActualWidth,
                    Height = this.MergeCandidatesListView.ActualHeight
                })
            {
                AlignmentX = AlignmentX.Center,
                AlignmentY = AlignmentY.Center,
                Stretch = Stretch.None,
                TileMode = TileMode.None,
                AutoLayoutContent = false
            };
        }

        private void UpdateSourceBranchPopupState()
        {
            this.SourceBranchPopup.Width = this.SourceBranchComboBox.ActualWidth;

            this.SourceBranchPopup.IsOpen = this.SourceBranchComboBox.IsKeyboardFocusWithin
                && !this.SourceBranchComboBox.IsDropDownOpen
                && this.SourceBranchPopupListBox.HasItems;
        }

        private void CloseSourceBranchPopup()
        {
            this.SourceBranchPopup.IsOpen = false;
        }

        private void UpdateSourceBranchPopupList()
        {
            this.SourceBranchPopupListBox.Items.Clear();

            var sourceBranchText = m_model.SourceBranch;
            if (string.IsNullOrWhiteSpace(sourceBranchText))
            {
                return;
            }

            IEnumerable<string> suggestions;
            using (var tfsWrapper = m_model.CreateTfsWrapper())
            {
                suggestions = tfsWrapper.GetSuggestions(sourceBranchText);
            }

            foreach (var suggestion in suggestions)
            {
                var lbi = new ListBoxItem()
                {
                    Content = suggestion,
                    Tag = suggestion,
                    ToolTip = suggestion
                };
                lbi.PreviewMouseLeftButtonDown += this.SourceBranchPopupListBox_PreviewMouseLeftButtonDown;
                this.SourceBranchPopupListBox.Items.Add(lbi);
            }
        }

        private bool SelectPopupItem(ComboBox comboBox, ListBoxItem lbi)
        {
            if (lbi == null)
            {
                return false;
            }

            var path = lbi.Tag as string;
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            comboBox.Text = path;
            comboBox.MoveCaretToEnd();

            return true;
        }

        private void SearchMergeCandidates()
        {
            this.MergeCandidatesListView.ItemsSource = null;
            this.MergeCandidatesListView.Items.Clear();
            SetMergeCandidatesListViewBackText(null);

            var sourceBranch = m_model.SourceBranch;
            if (string.IsNullOrWhiteSpace(sourceBranch))
            {
                SetMergeCandidatesListViewBackText("The source branch is not selected properly.");
                return;
            }

            var targetBranchItem = (ControlItem<ItemIdentifier>)this.TargetBranchComboBox.SelectedItem;
            if (targetBranchItem == null
                || targetBranchItem.Item == null
                || string.IsNullOrEmpty(targetBranchItem.Item.Item))
            {
                SetMergeCandidatesListViewBackText("The target branch is not selected properly.");
                return;
            }

            var targetBranch = targetBranchItem.Item.Item;
            var userName = (this.UserNameTextBox.Text ?? string.Empty).Trim();

            var pr = ProgressWindow.Execute(
                this,
                this.Title,
                "Searching for changesets to merge...",
                pw =>
                {
                    using (var tfsWrapper = m_model.CreateTfsWrapper())
                    {
                        return tfsWrapper
                            .VersionControlServer
                            .GetMergeCandidates(sourceBranch, targetBranch, RecursionType.Full);
                    }
                });

            if (pr.Cancelled)
            {
                SetMergeCandidatesListViewBackText("The operation is cancelled.");
                return;
            }
            if (pr.Exception != null)
            {
                SetMergeCandidatesListViewBackText(
                    string.Format(
                        "Error occurred: [{0}] {1}",
                        pr.Exception.GetType().FullName,
                        pr.Exception.Message));
                return;
            }

            var mergeCandidates = (MergeCandidate[])pr.Result;
            var filteredCandidates = (IEnumerable<MergeCandidate>)mergeCandidates;

            if (!string.IsNullOrEmpty(userName))
            {
                filteredCandidates = filteredCandidates
                    .Where(item => string.Equals(item.Changeset.Owner, userName, StringComparison.OrdinalIgnoreCase))
                    .ToArray();
            }

            if (!filteredCandidates.Any())
            {
                SetMergeCandidatesListViewBackText("Nothing is found matching the criteria.");
                return;
            }

            filteredCandidates = filteredCandidates
                .OrderBy(item => item.Changeset.CreationDate)
                .ThenBy(item => item.Changeset.ChangesetId);

            foreach (var candidate in filteredCandidates)
            {
                var item = ControlItem.Create(candidate, candidate.ToString());
                this.MergeCandidatesListView.Items.Add(item);
            }
        }

        #endregion

        #region Event Handlers

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.Title = string.Format("{0} — {1}", this.Title, App.Current.ProductName);
            App.DoEvents();

            Initialize();
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            SearchMergeCandidates();
        }

        private void FillCurrentUserNameButton_Click(object sender, RoutedEventArgs e)
        {
            FillCurrentUserName();
        }

        private void SourceBranchComboBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateSourceBranchPopupList();
            UpdateSourceBranchPopupState();

            m_model.RefreshTargetBranches();
        }

        private void MergeCandidatesListView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var vb = this.MergeCandidatesListView.Background as VisualBrush;
            if (vb == null)
            {
                return;
            }

            var fe = vb.Visual as FrameworkElement;
            if (fe == null)
            {
                return;
            }

            fe.Width = this.MergeCandidatesListView.ActualWidth;
            fe.Height = this.MergeCandidatesListView.ActualHeight;
        }

        private void SourceBranchComboBox_DropDownOpened(object sender, EventArgs e)
        {
            UpdateSourceBranchPopupState();
        }

        private void SourceBranchComboBox_DropDownClosed(object sender, EventArgs e)
        {
            UpdateSourceBranchPopupState();
        }

        private void SourceBranchComboBox_GotFocus(object sender, RoutedEventArgs e)
        {
            UpdateSourceBranchPopupState();
        }

        private void SourceBranchComboBox_LostFocus(object sender, RoutedEventArgs e)
        {
            UpdateSourceBranchPopupState();
        }

        private void SourceBranchPopupListBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = SelectPopupItem(this.SourceBranchComboBox, e.Source as ListBoxItem);
        }

        private void StackPanel_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                e.Handled = true;
                CloseSourceBranchPopup();
                return;
            }

            if (this.SourceBranchPopup.IsOpen)
            {
                if (e.Key == Key.Enter)
                {
                    e.Handled = SelectPopupItem(
                        this.SourceBranchComboBox,
                        this.SourceBranchPopupListBox.SelectedItem as ListBoxItem);
                    return;
                }

                int? newSelectedIndex = null;
                if (e.Key == Key.Up)
                {
                    e.Handled = true;
                    newSelectedIndex = this.SourceBranchPopupListBox.SelectedIndex - 1;
                }
                else if (e.Key == Key.Down)
                {
                    e.Handled = true;
                    newSelectedIndex = this.SourceBranchPopupListBox.SelectedIndex + 1;
                }

                if (newSelectedIndex.HasValue
                    && newSelectedIndex.Value >= 0
                    && newSelectedIndex.Value < this.SourceBranchPopupListBox.Items.Count)
                {
                    this.SourceBranchPopupListBox.SelectedIndex = newSelectedIndex.Value;
                }
            }
        }

        #endregion

        #endregion
    }
}