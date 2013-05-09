using System;
using System.Collections.Generic;
using System.Linq;

namespace TfsUtil
{
    public sealed class ProgressResult
    {
        #region Constructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="ProgressResult"/> class.
        /// </summary>
        internal ProgressResult(object result, bool cancelled, Exception exception)
        {
            this.Result = result;
            this.Cancelled = cancelled;
            this.Exception = exception;
        }

        #endregion

        #region Public Properties

        public object Result
        {
            get;
            private set;
        }

        public bool Cancelled
        {
            get;
            private set;
        }

        public Exception Exception
        {
            get;
            private set;
        }

        #endregion
    }
}