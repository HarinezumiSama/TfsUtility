using System;
using System.Collections.Generic;
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
        #region Fields

        private readonly TfsTeamProjectCollection m_teamProjectCollection;
        private readonly Lazy<VersionControlServer> m_versionControlServerLazy;

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

            m_teamProjectCollection = new TfsTeamProjectCollection(url);
            m_versionControlServerLazy = new Lazy<VersionControlServer>(() => GetService<VersionControlServer>());
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
                return m_teamProjectCollection;
            }
        }

        public VersionControlServer VersionControlServer
        {
            get
            {
                EnsureNotDisposed();
                return m_versionControlServerLazy.Value;
            }
        }

        #endregion

        #region Public Methods

        public T GetService<T>()
        {
            EnsureNotDisposed();
            return m_teamProjectCollection.GetService<T>();
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            this.IsDisposed = true;

            m_teamProjectCollection.Dispose();
        }

        #endregion
    }
}