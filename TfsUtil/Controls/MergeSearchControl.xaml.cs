using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.TeamFoundation.VersionControl.Client;
using TfsUtil.Wrappers;

namespace TfsUtil.Controls
{
    /// <summary>
    ///     Interaction logic for &quot;MergeSearchControl.xaml&quot;.
    /// </summary>
    public partial class MergeSearchControl
    {
        #region Constructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="MergeSearchControl"/> class.
        /// </summary>
        public MergeSearchControl(TfsServerInfo serverInfo)
            : this()
        {
            #region Argument Check

            if (serverInfo == null)
            {
                throw new ArgumentNullException("serverInfo");
            }

            #endregion

            this.ViewModel.TfsServer = serverInfo;
        }

        /// <summary>
        ///     Prevents a default instance of the <see cref="MergeSearchControl"/> class from being created.
        /// </summary>
        private MergeSearchControl()
        {
            InitializeComponent();
        }

        #endregion

        #region Public Properties

        public override string Header
        {
            [DebuggerNonUserCode]
            get
            {
                return "Merge Search";
            }
        }

        #endregion

        #region Private Methods

        #region Regular

        private static bool SelectPopupItem(ComboBox comboBox, ListBoxItem lbi)
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

        private void FillCurrentUserName()
        {
            var identity = WindowsIdentity.GetCurrent();
            this.UserNameTextBox.Text = identity == null ? string.Empty : identity.Name;
        }

        private void ClearUserName()
        {
            this.UserNameTextBox.Clear();
        }

        private void Initialize()
        {
            this.ViewModel.RefreshSourceBranches();
        }

        private void SetMergeCandidatesListViewBackText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                this.MergeCandidatesListView.Background = null;
                return;
            }

            this.MergeCandidatesListView.Background = new VisualBrush(
                new TextBlock
                {
                    Text = text,
                    ToolTip = text,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    TextAlignment = TextAlignment.Center,
                    Margin = new Thickness(12d),
                    TextWrapping = TextWrapping.Wrap
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

            var window = this.GetWindow();
            this.SourceBranchPopup.IsOpen = window != null
                && window.IsActive
                && this.SourceBranchComboBox.IsKeyboardFocusWithin
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

            var sourceBranchText = this.ViewModel.SourceBranch;
            if (string.IsNullOrWhiteSpace(sourceBranchText))
            {
                return;
            }

            IEnumerable<string> suggestions;
            using (var tfsWrapper = this.ViewModel.CreateTfsWrapper())
            {
                suggestions = tfsWrapper.GetSuggestions(sourceBranchText);
            }

            foreach (var suggestion in suggestions)
            {
                var lbi = new ListBoxItem
                {
                    Content = suggestion,
                    Tag = suggestion,
                    ToolTip = suggestion
                };
                lbi.PreviewMouseLeftButtonDown += this.SourceBranchPopupListBox_PreviewMouseLeftButtonDown;
                this.SourceBranchPopupListBox.Items.Add(lbi);
            }
        }

        private void SearchMergeCandidates()
        {
            this.MergeCandidatesListView.ItemsSource = null;
            this.MergeCandidatesListView.Items.Clear();
            this.MergeDirectionTextBox.Clear();
            SetMergeCandidatesListViewBackText(null);

            var sourceBranch = this.ViewModel.SourceBranch;
            if (string.IsNullOrWhiteSpace(sourceBranch))
            {
                SetMergeCandidatesListViewBackText("The source branch is not selected properly.");
                return;
            }

            var targetBranch = this.ViewModel.TargetBranch;
            if (string.IsNullOrWhiteSpace(targetBranch))
            {
                SetMergeCandidatesListViewBackText("The target branch is not selected properly.");
                return;
            }

            var userName = (this.UserNameTextBox.Text ?? string.Empty).Trim();

            var mergeDirection = new StringBuilder();
            mergeDirection.AppendFormat("'{0}' => '{1}'", sourceBranch, targetBranch);
            if (!string.IsNullOrEmpty(userName))
            {
                mergeDirection.AppendFormat(" by '{0}'", userName);
            }

            this.MergeDirectionTextBox.Text = mergeDirection.ToString();

            var window = this.GetWindow();
            var progressResult = ProgressWindow.Execute(
                window,
                window.EnsureNotNull().Title,
                "Searching for changesets to merge...",
                pw =>
                {
                    using (var tfsWrapper = this.ViewModel.CreateTfsWrapper())
                    {
                        return tfsWrapper
                            .VersionControlServer
                            .GetMergeCandidates(sourceBranch, targetBranch, RecursionType.Full)
                            .Select(item => new MergeCandidateWrapper(item))
                            .ToArray();
                    }
                });

            if (progressResult.Cancelled)
            {
                SetMergeCandidatesListViewBackText("The operation is cancelled.");
                return;
            }

            if (progressResult.Exception != null)
            {
                SetMergeCandidatesListViewBackText(
                    string.Format(
                        "Error occurred: [{0}] {1}",
                        progressResult.Exception.GetType().FullName,
                        progressResult.Exception.Message));
                return;
            }

            var mergeCandidates = (MergeCandidateWrapper[])progressResult.Result;
            var filteredCandidates = (IEnumerable<MergeCandidateWrapper>)mergeCandidates;

            if (!string.IsNullOrEmpty(userName))
            {
                filteredCandidates = filteredCandidates
                    .Where(item => string.Equals(item.Owner, userName, StringComparison.OrdinalIgnoreCase))
                    .ToArray();
            }

            if (!filteredCandidates.Any())
            {
                SetMergeCandidatesListViewBackText("Nothing is found matching the criteria.");
                return;
            }

            filteredCandidates = filteredCandidates
                .OrderBy(item => item.CreationDate)
                .ThenBy(item => item.ChangesetId);

            foreach (var candidate in filteredCandidates)
            {
                var item = ControlItem.Create(candidate, candidate.ToString());
                this.MergeCandidatesListView.Items.Add(item);
            }
        }

        private MergeCandidateWrapper GetSoleSelectedMergeCandidate()
        {
            if (this.MergeCandidatesListView.SelectedItems.Count != 1)
            {
                return null;
            }

            return ((ControlItem<MergeCandidateWrapper>)this.MergeCandidatesListView.SelectedItems[0]).Value;
        }

        private void CopySoleSelectedMergeCandidateDataToClipboard(Func<MergeCandidateWrapper, string> selector)
        {
            #region Argument Check

            if (selector == null)
            {
                throw new ArgumentNullException("selector");
            }

            #endregion

            var mergeCandidate = GetSoleSelectedMergeCandidate();
            if (mergeCandidate == null)
            {
                return;
            }

            var data = selector(mergeCandidate);
            Clipboard.SetText(data);
        }

        #endregion

        #region Event Handlers

        private void Control_Loaded(object sender, RoutedEventArgs e)
        {
            //// TODO [vmcl] Figure out why it's called multiple times (even when switching between tabs)

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

        private void ClearUserNameButton_Click(object sender, RoutedEventArgs e)
        {
            ClearUserName();
        }

        private void SourceBranchComboBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateSourceBranchPopupList();
            UpdateSourceBranchPopupState();

            this.ViewModel.RefreshTargetBranches();
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

        private void SourceBranchStackPanel_PreviewKeyDown(object sender, KeyEventArgs e)
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

                const int NoItemSelectedIndex = -1;

                int? newSelectedIndex = null;
                if (this.SourceBranchPopupListBox.Items.Count > 0)
                {
                    if (e.Key == Key.Up)
                    {
                        e.Handled = true;
                        newSelectedIndex = this.SourceBranchPopupListBox.SelectedIndex - 1;
                        if (newSelectedIndex < NoItemSelectedIndex)
                        {
                            newSelectedIndex = this.SourceBranchPopupListBox.Items.Count - 1;
                        }
                    }
                    else if (e.Key == Key.Down)
                    {
                        e.Handled = true;
                        newSelectedIndex = this.SourceBranchPopupListBox.SelectedIndex + 1;
                        if (newSelectedIndex >= this.SourceBranchPopupListBox.Items.Count)
                        {
                            newSelectedIndex = NoItemSelectedIndex;
                        }
                    }
                }

                if (newSelectedIndex.HasValue
                    && newSelectedIndex.Value >= NoItemSelectedIndex
                    && newSelectedIndex.Value < this.SourceBranchPopupListBox.Items.Count)
                {
                    this.SourceBranchPopupListBox.SelectedIndex = newSelectedIndex.Value;
                    this.SourceBranchPopupListBox.ScrollIntoView(this.SourceBranchPopupListBox.SelectedItem);
                }
            }
        }

        ////private void Window_Deactivated(object sender, EventArgs e)
        ////{
        ////    UpdateSourceBranchPopupState();
        ////}

        private void MergeCandidatesListView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var contextMenu = this.MergeCandidatesListView.ContextMenu;
            if (contextMenu == null)
            {
                e.Handled = true;
                return;
            }

            var isSoleItemSelected = this.MergeCandidatesListView.SelectedItems.Count == 1;

            var mergeCandidate = new Lazy<MergeCandidateWrapper>(GetSoleSelectedMergeCandidate);

            this.CopyChangesetNumberMenuItem.IsEnabled = isSoleItemSelected;
            this.CopyCommentMenuItem.IsEnabled = isSoleItemSelected;
            this.CopyWorkItemIdsMenuItem.IsEnabled = isSoleItemSelected
                && mergeCandidate.Value != null
                && !string.IsNullOrEmpty(mergeCandidate.Value.WorkItemIdsAsString);
        }

        private void CopyChangesetNumberMenuItem_Click(object sender, RoutedEventArgs e)
        {
            CopySoleSelectedMergeCandidateDataToClipboard(
                mc => mc.ChangesetId.ToString("D", CultureInfo.InvariantCulture));
        }

        private void CopyCommentMenuItem_Click(object sender, RoutedEventArgs e)
        {
            CopySoleSelectedMergeCandidateDataToClipboard(mc => mc.Comment);
        }

        private void CopyWorkItemIdsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            CopySoleSelectedMergeCandidateDataToClipboard(mc => mc.WorkItemIdsAsString);
        }

        private void CopyTargetBranch_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(this.ViewModel.TargetBranch);
        }

        private void SetAsSourceBranch_Click(object sender, RoutedEventArgs e)
        {
            this.ViewModel.SourceBranch = this.ViewModel.TargetBranch;
        }

        private void TargetBranchComboBox_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var isTargetBranchValid = !string.IsNullOrWhiteSpace(this.ViewModel.TargetBranch);

            this.CopyTargetBranch.IsEnabled = isTargetBranchValid;
            this.SetAsSourceBranch.IsEnabled = isTargetBranchValid;
        }

        #endregion

        #endregion
    }
}