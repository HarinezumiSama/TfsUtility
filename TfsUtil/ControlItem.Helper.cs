using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TfsUtil
{
    public static class ControlItem
    {
        #region Public Methods

        public static ControlItem<T> Create<T>(T item, string text)
        {
            return new ControlItem<T>(item, text);
        }

        public static ControlItem<T> Create<T>(T item)
        {
            return new ControlItem<T>(item);
        }

        #endregion
    }
}