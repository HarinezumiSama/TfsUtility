using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace TfsUtil.Commands
{
    internal static class AppCommands
    {
        #region Fields

        public static readonly RoutedUICommand MergeSearch = new RoutedUICommand(
            "Merge search",
            "MergeSearch",
            typeof(AppCommands),
            new InputGestureCollection { new KeyGesture(Key.M, ModifierKeys.Control) });

        public static readonly RoutedUICommand CloseActiveContent = new RoutedUICommand(
            "Close Active Content",
            "CloseActiveContent",
            typeof(AppCommands),
            new InputGestureCollection { new KeyGesture(Key.W, ModifierKeys.Control) });

        #endregion
    }
}