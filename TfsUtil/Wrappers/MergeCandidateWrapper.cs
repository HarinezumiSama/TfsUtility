using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace TfsUtil.Wrappers
{
    /// <summary>
    ///     Represents a wrapper for <see cref="MergeCandidate"/> class.
    /// </summary>
    public sealed class MergeCandidateWrapper
    {
        #region Constructors and Destructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="MergeCandidateWrapper"/> class.
        /// </summary>
        public MergeCandidateWrapper(MergeCandidate mergeCandidate)
        {
            #region Argument Check

            if (mergeCandidate == null)
            {
                throw new ArgumentNullException("mergeCandidate");
            }

            if (mergeCandidate.Changeset == null)
            {
                throw new ArgumentException(@"The associated changeset cannot be null.", "mergeCandidate");
            }

            #endregion

            var changeset = mergeCandidate.Changeset;

            this.AsString = mergeCandidate.ToString();
            this.CreationDate = changeset.CreationDate;
            this.ChangesetId = changeset.ChangesetId;
            this.Owner = changeset.Owner ?? string.Empty;
            this.Comment = changeset.Comment ?? string.Empty;

            this.WorkItems = changeset.WorkItems.Select(item => new WorkItemWrapper(item)).ToList().AsReadOnly();
            this.WorkItemIdsAsString = string.Join(
                ", ",
                this.WorkItems.OrderBy(item => item.Id).Select(item => item.Id.ToString("D")));
        }

        #endregion

        #region Public Properties

        public string AsString
        {
            get;
            private set;
        }

        public DateTime CreationDate
        {
            get;
            private set;
        }

        public int ChangesetId
        {
            get;
            private set;
        }

        public string Owner
        {
            get;
            private set;
        }

        public string Comment
        {
            get;
            private set;
        }

        public IList<WorkItemWrapper> WorkItems
        {
            get;
            private set;
        }

        public string WorkItemIdsAsString
        {
            get;
            private set;
        }

        #endregion

        #region Public Methods

        public override string ToString()
        {
            return string.Format("{0}. {{{1}}}", GetType().Name, this.AsString);
        }

        #endregion
    }
}