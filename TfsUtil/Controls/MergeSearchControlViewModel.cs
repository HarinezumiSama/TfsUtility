using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows.Data;
using Microsoft.TeamFoundation.VersionControl.Client;
using TfsUtil.Wrappers;

namespace TfsUtil.Controls
{
    internal sealed class MergeSearchControlViewModel : INotifyPropertyChanged
    {
        #region Fields

        private string _sourceBranch;

        #endregion

        #region Constructors

        public MergeSearchControlViewModel()
        {
            _sourceBranch = string.Empty;

            this.SourceBranches = new List<ControlItem<ItemIdentifier>>();
            this.SourceBranchesView = CollectionViewSource.GetDefaultView(this.SourceBranches);
            this.SourceBranchesView.CurrentChanged += this.SourceBranchesView_CurrentChanged;

            this.TargetBranches = new List<ControlItem<ItemIdentifier>>();
            this.TargetBranchesView = CollectionViewSource.GetDefaultView(this.TargetBranches);
        }

        #endregion

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region Public Properties

        public TfsServerInfo TfsServer
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
                return _sourceBranch;
            }

            set
            {
                var actualValue = value ?? string.Empty;
                if (_sourceBranch == actualValue)
                {
                    return;
                }

                _sourceBranch = actualValue;
                RaisePropertyChanged(Helper.GetPropertyName((MergeSearchControlViewModel obj) => obj.SourceBranch));
            }
        }

        public string TargetBranch
        {
            get
            {
                var currentTargetBranchItem = (ControlItem<ItemIdentifier>)this.TargetBranchesView.CurrentItem;

                return currentTargetBranchItem == null
                    || currentTargetBranchItem.Item == null
                    || string.IsNullOrWhiteSpace(currentTargetBranchItem.Item.Item)
                    ? string.Empty
                    : currentTargetBranchItem.Item.Item;
            }
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

        #region Public Methods

        public TfsWrapper CreateTfsWrapper()
        {
            return new TfsWrapper(this.TfsServer);
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
    }
}