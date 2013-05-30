using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace TfsUtil
{
    public sealed class ContentTabViewModel
    {
        #region Public Properties

        public string HeaderText
        {
            get;
            set;
        }

        public Uri TfsServerUri
        {
            get;
            set;
        }

        #endregion
    }
}