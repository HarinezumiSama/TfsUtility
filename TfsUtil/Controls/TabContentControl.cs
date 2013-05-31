using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Controls;

namespace TfsUtil.Controls
{
    public abstract class TabContentControl : UserControl
    {
        #region Public Properties

        public abstract string Header
        {
            get;
        }

        #endregion
    }
}