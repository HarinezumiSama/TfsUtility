using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.TeamFoundation.WorkItemTracking.Client;

namespace TfsUtil
{
    /// <summary>
    ///     Represents a wrapper for <see cref="WorkItem"/> class.
    /// </summary>
    public sealed class WorkItemWrapper
    {
        #region Constructors and Destructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="WorkItemWrapper"/> class.
        /// </summary>
        public WorkItemWrapper(WorkItem workItem)
        {
            #region Argument Check

            if (workItem == null)
            {
                throw new ArgumentNullException("workItem");
            }

            #endregion

            this.AsString = workItem.ToString();
            this.Id = workItem.Id;
        }

        #endregion

        #region Public Properties

        public string AsString
        {
            get;
            private set;
        }

        public int Id
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