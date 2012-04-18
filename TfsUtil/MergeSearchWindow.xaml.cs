﻿using System;
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

        private sealed class MergeSearchWindowModel
        {
            #region Constructors

            public MergeSearchWindowModel()
            {
                this.SourceBranches = new List<ControlItem<ItemIdentifier>>();
                this.SourceBranchesView = CollectionViewSource.GetDefaultView(this.SourceBranches);
                this.SourceBranchesView.CurrentChanged += this.SourceBranchesView_CurrentChanged;

                this.TargetBranches = new List<ControlItem<ItemIdentifier>>();
                this.TargetBranchesView = CollectionViewSource.GetDefaultView(this.TargetBranches);
            }

            #endregion

            #region Private Methods

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
                get;
                set;
            }

            #endregion

            #region Public Methods

            public void RefreshSourceBranches()
            {
                BranchObject[] branchObjects;
                using (var tfsWrapper = new TfsWrapper(this.TfsServerUri))
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
                    using (var tfsWrapper = new TfsWrapper(this.TfsServerUri))
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

        private void SearchMergeCandidates()
        {
            this.MergeCandidatesListView.Items.Clear();

            var sourceBranch = m_model.SourceBranch;
            if (string.IsNullOrWhiteSpace(sourceBranch))
            {
                return;
            }

            var targetBranchItem = (ControlItem<ItemIdentifier>)this.TargetBranchComboBox.SelectedItem;
            if (targetBranchItem == null
                || targetBranchItem.Item == null
                || string.IsNullOrEmpty(targetBranchItem.Item.Item))
            {
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
                    using (var tfsWrapper = new TfsWrapper(m_model.TfsServerUri))
                    {
                        return tfsWrapper
                            .VersionControlServer
                            .GetMergeCandidates(sourceBranch, targetBranch, RecursionType.Full);
                    }
                });

            if (pr.Cancelled)
            {
                this.MergeCandidatesListView.Items.Add("The operation is cancelled.");
                return;
            }
            if (pr.Exception != null)
            {
                this.MergeCandidatesListView.Items.Add(
                    string.Format(
                        "Error occurred: [{0}] {1}",
                        pr.Exception.GetType().FullName,
                        pr.Exception.Message));
                return;
            }

            var mergeCandidates = (MergeCandidate[])pr.Result;
            var filteredCandidates = mergeCandidates;

            if (!string.IsNullOrEmpty(userName))
            {
                filteredCandidates = filteredCandidates
                    .Where(item => string.Equals(item.Changeset.Owner, userName, StringComparison.OrdinalIgnoreCase))
                    .ToArray();
            }

            if (!filteredCandidates.Any())
            {
                this.MergeCandidatesListView.Items.Add("Nothing is found.");
                return;
            }

            foreach (var candidate in filteredCandidates)
            {
                this.MergeCandidatesListView.Items.Add(candidate.ToString());
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
            m_model.RefreshTargetBranches();
        }

        #endregion

        #endregion
    }
}