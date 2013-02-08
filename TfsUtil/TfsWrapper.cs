using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Framework.Client;
using Microsoft.TeamFoundation.Framework.Common;
using Microsoft.TeamFoundation.VersionControl.Client;
using TfsUtil.Properties;

namespace TfsUtil
{
    public sealed class TfsWrapper : IDisposable
    {
        #region Constants

        public const string TfsRoot = "$";
        public const string TfsPathSeparator = "/";

        #endregion

        #region Fields

        private readonly TfsTeamProjectCollection _teamProjectCollection;
        private readonly Lazy<VersionControlServer> _versionControlServerLazy;

        #endregion

        #region Constructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="TfsWrapper"/> class
        ///     using the specified URL.
        /// </summary>
        public TfsWrapper(Uri url)
        {
            #region Argument Check

            if (url == null)
            {
                throw new ArgumentNullException("url");
            }

            #endregion

            _teamProjectCollection = new TfsTeamProjectCollection(url);
            _versionControlServerLazy = new Lazy<VersionControlServer>(() => GetService<VersionControlServer>());
        }

        #endregion

        #region Public Properties

        public bool IsDisposed
        {
            get;
            private set;
        }

        public TfsTeamProjectCollection TeamProjectCollection
        {
            get
            {
                EnsureNotDisposed();
                return _teamProjectCollection;
            }
        }

        public VersionControlServer VersionControlServer
        {
            get
            {
                EnsureNotDisposed();
                return _versionControlServerLazy.Value;
            }
        }

        #endregion

        #region Public Methods

        public T GetService<T>()
        {
            EnsureNotDisposed();
            return _teamProjectCollection.GetService<T>();
        }

        public IEnumerable<string> GetSuggestions(string path)
        {
            var actualPath = path == null ? string.Empty : path.Trim();
            if (string.IsNullOrEmpty(actualPath))
            {
                actualPath = TfsRoot + TfsPathSeparator;
            }
            else if (actualPath == TfsRoot)
            {
                actualPath += TfsPathSeparator;
            }
            else if (!actualPath.StartsWith(TfsRoot, StringComparison.OrdinalIgnoreCase))
            {
                return Enumerable.Empty<string>();
            }

            string folder;
            string beginning;

            var lastChar = actualPath.Last();
            if (lastChar == Path.DirectorySeparatorChar || lastChar == Path.AltDirectorySeparatorChar)
            {
                folder = actualPath.Substring(0, actualPath.Length - 1);
                beginning = null;
            }
            else
            {
                folder = Path.GetDirectoryName(actualPath);
                beginning = Path.GetFileName(actualPath);
            }

            var items = GetItemsInternal(folder);
            if (items == null)
            {
                return Enumerable.Empty<string>();
            }

            var filteredItems = items.Items.Select(item => item.ServerItem);

            if (!string.IsNullOrWhiteSpace(beginning))
            {
                filteredItems = filteredItems
                    .Where(item => item.StartsWith(actualPath, StringComparison.OrdinalIgnoreCase));
            }

            var result = filteredItems
                .OrderBy(item => item, StringComparer.OrdinalIgnoreCase)
                .ThenBy(item => item, StringComparer.Ordinal)
                .ToList();

            if (result.Count == 1 && StringComparer.OrdinalIgnoreCase.Equals(result.First(), actualPath))
            {
                return Enumerable.Empty<string>();
            }

            return result;
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            if (this.IsDisposed)
            {
                return;
            }

            _teamProjectCollection.Dispose();

            this.IsDisposed = true;
        }

        #endregion

        #region Private Methods

        private void EnsureNotDisposed()
        {
            if (this.IsDisposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }

        [DebuggerNonUserCode]
        private ItemSet GetItemsInternal(string folder)
        {
            if (string.IsNullOrWhiteSpace(folder))
            {
                return null;
            }

            if (folder == TfsRoot)
            {
                folder += TfsPathSeparator;
            }

            try
            {
                return this.VersionControlServer.GetItems(folder, RecursionType.OneLevel);
            }
            catch (Exception)
            {
                return null;
            }
        }

        #endregion
    }
}