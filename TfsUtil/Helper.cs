using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows.Threading;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace TfsUtil
{
    internal static class Helper
    {
        #region Constants

        private const bool DefaultInheritAttributeParameter = true;

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

        #region Public Methods

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

            if (collection == null)
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
            catch (ItemNotMappedException)
            {
                return false;
            }
            catch (ItemNotFoundException)
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

        #endregion
    }
}