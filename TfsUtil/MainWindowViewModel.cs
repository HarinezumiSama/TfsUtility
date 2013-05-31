using System;
using System.Linq;
using TfsUtil.Wrappers;

namespace TfsUtil
{
    public sealed class MainWindowViewModel
    {
        #region Public Properties

        public TfsServerInfo SelectedServer
        {
            get;
            set;
        }

        #endregion
    }
}