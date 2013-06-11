using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

namespace TfsUtil.Commands
{
    internal static class AppCommands
    {
        #region Fields

        public static readonly RoutedUICommand NewMergeSearch = new RoutedUICommand(
            "New Merge search",
            "NewMergeSearch",
            typeof(AppCommands),
            new InputGestureCollection { new KeyGesture(Key.M, ModifierKeys.Control) });

        public static readonly RoutedUICommand CloseActiveContent = new RoutedUICommand(
            "Close",
            "CloseActiveContent",
            typeof(AppCommands),
            new InputGestureCollection { new KeyGesture(Key.W, ModifierKeys.Control) });

        #endregion
    }
}