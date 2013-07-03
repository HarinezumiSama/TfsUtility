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

            this.MergeCandidates = new List<MergeCandidateWrapper>();
            this.MergeCandidatesView = CollectionViewSource.GetDefaultView(this.MergeCandidates);
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

        public ICollectionView MergeCandidatesView
        {
            get;
            private set;
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

        internal List<MergeCandidateWrapper> MergeCandidates
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

            //// TODO [vmaklai] Handle exceptions in RefreshSourceBranches task

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

        public void SearchMergeCandidates(string sourceBranch, string targetBranch, string userName)
        {
            SetIsBusy(true);

            var task = Task<MergeCandidateWrapper[]>.Factory.StartNew(
                () => GetMergeCandidates(sourceBranch, targetBranch, userName));

            //// TODO [vmaklai] Handle exceptions in SearchMergeCandidates task

            ////task.ContinueWith(
            ////    t =>
            ////    {
            ////        if (t.Exception == null)
            ////        {
            ////            return;
            ////        }

            ////        var exception = t.Exception.InnerExceptions.FirstOrDefault()
            ////            ?? t.Exception.InnerException
            ////            ?? t.Exception;
            ////        SetMergeCandidatesListViewBackText(
            ////            string.Format(
            ////                "Error occurred: [{0}] {1}",
            ////                exception.GetType().FullName,
            ////                exception.Message));
            ////    },
            ////    CancellationToken.None,
            ////    TaskContinuationOptions.OnlyOnFaulted,
            ////    uiScheduler);

            task.ContinueWith(
                this.OnSearchMergeCandidatesFinished,
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnRanToCompletion,
                _uiScheduler);
        }

        public MergeCandidateWrapper[] GetSelectedMergeCandidates()
        {
            return this.MergeCandidates.Where(item => item.IsSelected).ToArray();
        }

        public MergeCandidateWrapper GetSoleSelectedMergeCandidate()
        {
            var selectedMergeCandidates = GetSelectedMergeCandidates();
            return selectedMergeCandidates.Length == 1 ? selectedMergeCandidates.Single() : null;
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

        private MergeCandidateWrapper[] GetMergeCandidates(string sourceBranch, string targetBranch, string userName)
        {
            using (var tfsWrapper = CreateTfsWrapper())
            {
                var mergeCandidates = tfsWrapper
                    .VersionControlServer
                    .GetMergeCandidates(sourceBranch, targetBranch, RecursionType.Full)
                    .Select(item => new MergeCandidateWrapper(item))
                    .ToArray();

                var filteredCandidates = (IEnumerable<MergeCandidateWrapper>)mergeCandidates;

                if (!string.IsNullOrEmpty(userName))
                {
                    filteredCandidates = filteredCandidates
                        .Where(item => string.Equals(item.Owner, userName, StringComparison.OrdinalIgnoreCase))
                        .ToArray();
                }

                ////if (!filteredCandidates.Any())
                ////{
                ////    SetMergeCandidatesListViewBackText("Nothing is found matching the criteria.");
                ////    return;
                ////}

                filteredCandidates = filteredCandidates
                    .OrderBy(item => item.CreationDate)
                    .ThenBy(item => item.ChangesetId);

                return filteredCandidates.ToArray();
            }
        }

        private void OnSearchMergeCandidatesFinished(Task<MergeCandidateWrapper[]> task)
        {
            var mergeCandidates = task.EnsureNotNull().Result ?? Enumerable.Empty<MergeCandidateWrapper>();
            this.MergeCandidates.ReplaceContents(mergeCandidates);

            this.MergeCandidatesView.Refresh();
            this.MergeCandidatesView.MoveCurrentToFirst();

            SetIsBusy(false);
        }

        private void SourceBranchesView_CurrentChanged(object sender, EventArgs e)
        {
            RefreshTargetBranches();
        }

        #endregion
    }
}