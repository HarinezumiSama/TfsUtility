using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using Microsoft.TeamFoundation.VersionControl.Client;
using TfsUtil.Wrappers;

namespace TfsUtil.Controls
{
    internal sealed class MergeSearchControlViewModel : INotifyPropertyChanged
    {
        #region Fields

        private readonly object _busySyncLock = new object();
        private readonly TaskScheduler _uiScheduler;
        private bool _isBusy;
        private string _sourceBranch;

        #endregion

        #region Constructors

        public MergeSearchControlViewModel()
        {
            _uiScheduler = TaskScheduler.FromCurrentSynchronizationContext();

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

        public bool IsBusy
        {
            [DebuggerStepThrough]
            get
            {
                lock (_busySyncLock)
                {
                    return _isBusy;
                }
            }
        }

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
                RaisePropertyChanged(obj => obj.SourceBranch);
            }
        }

        public string TargetBranch
        {
            get
            {
                var currentTargetBranchItem = (ControlItem<ItemIdentifier>)this.TargetBranchesView.CurrentItem;

                return currentTargetBranchItem == null
                    || currentTargetBranchItem.Value == null
                    || string.IsNullOrWhiteSpace(currentTargetBranchItem.Value.Item)
                    ? string.Empty
                    : currentTargetBranchItem.Value.Item;
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

        public bool SetIsBusy(bool value)
        {
            bool result;

            lock (_busySyncLock)
            {
                result = _isBusy;
                if (value == _isBusy)
                {
                    return result;
                }

                _isBusy = value;
            }

            RaisePropertyChanged(obj => obj.IsBusy);

            return result;
        }

        public TfsWrapper CreateTfsWrapper()
        {
            return new TfsWrapper(this.TfsServer);
        }

        public void RefreshSourceBranches()
        {
            SetIsBusy(true);

            var task = Task<ControlItem<ItemIdentifier>[]>.Factory.StartNew(this.GetSourceBranches);

            task.ContinueWith(
                this.OnRefreshSourceBranchesFinished,
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnRanToCompletion,
                _uiScheduler);
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

        private void RaisePropertyChanged<TProperty>(
            Expression<Func<MergeSearchControlViewModel, TProperty>> propertyGetterExpression)
        {
            #region Argument Check

            if (propertyGetterExpression == null)
            {
                throw new ArgumentNullException("propertyGetterExpression");
            }

            #endregion

            var propertyChanged = this.PropertyChanged;
            if (propertyChanged == null)
            {
                return;
            }

            var propertyName = Helper.GetPropertyName(propertyGetterExpression);
            propertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        private ControlItem<ItemIdentifier>[] GetSourceBranches()
        {
            BranchObject[] branchObjects;
            using (var tfsWrapper = CreateTfsWrapper())
            {
                branchObjects = tfsWrapper.VersionControlServer.QueryRootBranchObjects(RecursionType.Full);
            }

            var result = branchObjects
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

            return result;
        }

        private void OnRefreshSourceBranchesFinished(Task<ControlItem<ItemIdentifier>[]> task)
        {
            var sourceBranches = task.EnsureNotNull().Result ?? Enumerable.Empty<ControlItem<ItemIdentifier>>();
            this.SourceBranches.ReplaceContents(sourceBranches);

            this.SourceBranchesView.Refresh();
            this.SourceBranchesView.MoveCurrentToFirst();

            SetIsBusy(false);

            RefreshTargetBranches();
        }

        private void SourceBranchesView_CurrentChanged(object sender, EventArgs e)
        {
            RefreshTargetBranches();
        }

        #endregion
    }
}