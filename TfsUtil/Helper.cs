using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace TfsUtil
{
    internal static class Helper
    {
        #region Constants

        private const bool DefaultInheritAttributeParameter = true;

        /// <summary>
        ///     The invalid expression message format.
        /// </summary>
        private const string InvalidExpressionMessageFormat =
            "Invalid expression (must be a getter of a property of the type '{0}'): {{ {1} }}.";

        /// <summary>
        ///     The invalid expression message auto format.
        /// </summary>
        private const string InvalidExpressionMessageAutoFormat =
            "Invalid expression (must be a getter of a property of some type): {{ {0} }}.";

        #endregion

        #region Public Methods

        [DebuggerNonUserCode]
        public static T EnsureNotNull<T>(this T value)
            where T : class
        {
            #region Argument Check

            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            #endregion

            return value;
        }

        public static PropertyInfo GetPropertyInfo<TObject, TProperty>(
            Expression<Func<TObject, TProperty>> propertyGetterExpression)
        {
            #region Argument Check

            if (propertyGetterExpression == null)
            {
                throw new ArgumentNullException("propertyGetterExpression");
            }

            #endregion

            var objectType = typeof(TObject);

            var memberExpression = propertyGetterExpression.Body as MemberExpression;
            if ((memberExpression == null) || (memberExpression.NodeType != ExpressionType.MemberAccess))
            {
                throw new ArgumentException(
                    string.Format(InvalidExpressionMessageFormat, objectType.FullName, propertyGetterExpression),
                    "propertyGetterExpression");
            }

            var result = memberExpression.Member as PropertyInfo;
            if (result == null)
            {
                throw new ArgumentException(
                    string.Format(InvalidExpressionMessageFormat, objectType.FullName, propertyGetterExpression),
                    "propertyGetterExpression");
            }

            if ((result.DeclaringType == null) || !result.DeclaringType.IsAssignableFrom(objectType))
            {
                throw new ArgumentException(
                    string.Format(InvalidExpressionMessageFormat, objectType.FullName, propertyGetterExpression),
                    "propertyGetterExpression");
            }

            if (memberExpression.Expression == null)
            {
                var accessor = result.GetGetMethod(true) ?? result.GetSetMethod(true);
                if ((accessor == null) || !accessor.IsStatic || (result.ReflectedType != objectType))
                {
                    throw new ArgumentException(
                        string.Format(InvalidExpressionMessageFormat, objectType.FullName, propertyGetterExpression),
                        "propertyGetterExpression");
                }
            }
            else
            {
                var parameterExpression = memberExpression.Expression as ParameterExpression;
                if ((parameterExpression == null) || (parameterExpression.NodeType != ExpressionType.Parameter) ||
                    (parameterExpression.Type != typeof(TObject)))
                {
                    throw new ArgumentException(
                        string.Format(InvalidExpressionMessageFormat, objectType.FullName, propertyGetterExpression),
                        "propertyGetterExpression");
                }
            }

            return result;
        }

        public static string GetPropertyName<TObject, TProperty>(
            Expression<Func<TObject, TProperty>> propertyGetterExpression)
        {
            var propertyInfo = GetPropertyInfo(propertyGetterExpression);
            return propertyInfo.Name;
        }

        public static string GetQualifiedPropertyName<TObject, TProperty>(
            Expression<Func<TObject, TProperty>> propertyGetterExpression)
        {
            var propertyInfo = GetPropertyInfo(propertyGetterExpression);
            return typeof(TObject).Name + Type.Delimiter + propertyInfo.Name;
        }

        public static PropertyInfo GetPropertyInfo<TProperty>(Expression<Func<TProperty>> propertyGetterExpression)
        {
            #region Argument Check

            if (propertyGetterExpression == null)
            {
                throw new ArgumentNullException("propertyGetterExpression");
            }

            #endregion

            var memberExpression = propertyGetterExpression.Body as MemberExpression;
            if ((memberExpression == null) || (memberExpression.NodeType != ExpressionType.MemberAccess))
            {
                throw new ArgumentException(
                    string.Format(InvalidExpressionMessageAutoFormat, propertyGetterExpression),
                    "propertyGetterExpression");
            }

            var result = memberExpression.Member as PropertyInfo;
            if (result == null)
            {
                throw new ArgumentException(
                    string.Format(InvalidExpressionMessageAutoFormat, propertyGetterExpression),
                    "propertyGetterExpression");
            }

            if (result.DeclaringType == null)
            {
                throw new ArgumentException(
                    string.Format(InvalidExpressionMessageAutoFormat, propertyGetterExpression),
                    "propertyGetterExpression");
            }

            if (memberExpression.Expression == null)
            {
                var accessor = result.GetGetMethod(true) ?? result.GetSetMethod(true);
                if ((accessor == null) || !accessor.IsStatic)
                {
                    throw new ArgumentException(
                        string.Format(InvalidExpressionMessageAutoFormat, propertyGetterExpression),
                        "propertyGetterExpression");
                }
            }

            return result;
        }

        public static string GetPropertyName<TProperty>(Expression<Func<TProperty>> propertyGetterExpression)
        {
            var propertyInfo = GetPropertyInfo(propertyGetterExpression);
            return propertyInfo.Name;
        }

        public static string GetQualifiedPropertyName<TProperty>(Expression<Func<TProperty>> propertyGetterExpression)
        {
            var propertyInfo = GetPropertyInfo(propertyGetterExpression);
            return propertyInfo.DeclaringType.EnsureNotNull().Name + Type.Delimiter + propertyInfo.Name;
        }

        public static bool IsThreadAbort(this Exception exception)
        {
            return exception is ThreadAbortException || exception is ThreadInterruptedException;
        }

        public static TAttribute GetSoleAttribute<TAttribute>(
            this ICustomAttributeProvider attributeProvider,
            bool inherit)
        {
            #region Argument Check

            if (attributeProvider == null)
            {
                throw new ArgumentNullException("attributeProvider");
            }

            #endregion

            return GetSoleAttributeInternal<TAttribute>(attributeProvider, inherit, Enumerable.SingleOrDefault);
        }

        public static TAttribute GetSoleAttribute<TAttribute>(this ICustomAttributeProvider attributeProvider)
        {
            return GetSoleAttribute<TAttribute>(attributeProvider, DefaultInheritAttributeParameter);
        }

        public static TAttribute GetSoleAttributeStrict<TAttribute>(
            this ICustomAttributeProvider attributeProvider,
            bool inherit)
        {
            #region Argument Check

            if (attributeProvider == null)
            {
                throw new ArgumentNullException("attributeProvider");
            }

            #endregion

            return GetSoleAttributeInternal<TAttribute>(attributeProvider, inherit, Enumerable.Single);
        }

        public static TAttribute GetSoleAttributeStrict<TAttribute>(this ICustomAttributeProvider attributeProvider)
        {
            return GetSoleAttributeStrict<TAttribute>(attributeProvider, DefaultInheritAttributeParameter);
        }

        public static TCollection ReplaceContents<T, TCollection>(
            this TCollection collection,
            IEnumerable<T> newContents)
            where TCollection : ICollection<T>
        {
            #region Argument Check

            if (ReferenceEquals(collection, null))
            {
                throw new ArgumentNullException("collection");
            }

            if (newContents == null)
            {
                throw new ArgumentNullException("newContents");
            }

            #endregion

            collection.Clear();

            var list = collection as List<T>;
            if (list == null)
            {
                foreach (var item in newContents)
                {
                    collection.Add(item);
                }
            }
            else
            {
                list.AddRange(newContents);
            }

            return collection;
        }

        public static IEnumerable<T> AsCollection<T>(this T instance)
        {
            yield return instance;
        }

        [DebuggerNonUserCode]
        public static bool ServerItemExistsSafe(
            this VersionControlServer versionControlServer,
            string path,
            ItemType itemType)
        {
            #region Argument Check

            if (versionControlServer == null)
            {
                throw new ArgumentNullException("versionControlServer");
            }

            #endregion

            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            try
            {
                return versionControlServer.ServerItemExists(path, itemType);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static T GetValueSafe<TControl, T>(this TControl control, Func<TControl, T> getValue)
            where TControl : DispatcherObject
        {
            #region Argument Check

            if (control == null)
            {
                throw new ArgumentNullException("control");
            }

            if (getValue == null)
            {
                throw new ArgumentNullException("getValue");
            }

            #endregion

            var dispatcher = control.Dispatcher;

            return dispatcher.CheckAccess()
                ? getValue(control)
                : (T)dispatcher.Invoke(getValue, control);
        }

        public static void ExecuteActionSafe<TControl>(this TControl control, Action<TControl> action)
            where TControl : DispatcherObject
        {
            #region Argument Check

            if (control == null)
            {
                throw new ArgumentNullException("control");
            }

            if (action == null)
            {
                throw new ArgumentNullException("action");
            }

            #endregion

            var dispatcher = control.Dispatcher;

            if (dispatcher.CheckAccess())
            {
                action(control);
            }
            else
            {
                dispatcher.BeginInvoke(action, control);
            }
        }

        // Workaround
        public static void MoveCaretToEnd(this ComboBox comboBox)
        {
            #region Argument Check

            if (comboBox == null)
            {
                throw new ArgumentNullException("comboBox");
            }

            #endregion

            var inputManager = InputManager.Current.EnsureNotNull();

            var keyboardDevice = inputManager.PrimaryKeyboardDevice;
            if (keyboardDevice == null)
            {
                return;
            }

            var source = keyboardDevice.ActiveSource;
            if (source == null)
            {
                return;
            }

            var args = new KeyEventArgs(keyboardDevice, source, 0, Key.End)
            {
                RoutedEvent = Keyboard.KeyDownEvent
            };

            inputManager.ProcessInput(args);
        }

        public static MessageBoxResult ShowMessageBox(
            this Window window,
            string message,
            MessageBoxButton button,
            MessageBoxImage icon)
        {
            #region Argument Check

            if (window == null)
            {
                throw new ArgumentNullException("window");
            }

            #endregion

            return MessageBox.Show(window, message, window.Title, button, icon);
        }

        public static Window GetWindow(this Control control)
        {
            #region Argument Check

            if (control == null)
            {
                throw new ArgumentNullException("control");
            }

            #endregion

            var currentControl = (FrameworkElement)control;
            while (currentControl != null)
            {
                var result = currentControl as Window;
                if (result != null)
                {
                    return result;
                }

                currentControl = currentControl.Parent as FrameworkElement;
            }

            return null;
        }

        [DebuggerNonUserCode]
        public static bool IsModal(this Window window)
        {
            #region Argument Check

            if (window == null)
            {
                throw new ArgumentNullException("window");
            }

            #endregion

            try
            {
                var windowInteropHelper = new WindowInteropHelper(window);
                var handle = windowInteropHelper.Handle;
                var automationElement = AutomationElement.FromHandle(handle);
                if (automationElement == null)
                {
                    return false;
                }

                var isModalObject = automationElement.GetCurrentPropertyValue(WindowPattern.IsModalProperty, false);
                if (isModalObject is bool)
                {
                    return (bool)isModalObject;
                }

                object patternObject;
                if (automationElement.TryGetCurrentPattern(WindowPattern.Pattern, out patternObject))
                {
                    var pattern = patternObject as WindowPattern;
                    if (pattern != null)
                    {
                        return pattern.Current.IsModal;
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }

            return false;
        }

        #endregion

        #region Private Methods

        private static TAttribute GetSoleAttributeInternal<TAttribute>(
            this ICustomAttributeProvider attributeProvider,
            bool inherit,
            Func<IEnumerable<TAttribute>, TAttribute> getter)
        {
            #region Argument Check

            if (attributeProvider == null)
            {
                throw new ArgumentNullException("attributeProvider");
            }

            #endregion

            var attributes = attributeProvider.GetCustomAttributes(typeof(TAttribute), inherit).OfType<TAttribute>();
            return getter(attributes);
        }

        #endregion
    }
}