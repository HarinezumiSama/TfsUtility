using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.TeamFoundation.Client;

namespace TfsUtil.Wrappers
{
    public sealed class TfsServerInfo
    {
        #region Constructors and Destructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="TfsServerInfo"/> class.
        /// </summary>
        public TfsServerInfo(RegisteredProjectCollection registeredProjectCollection)
        {
            #region Argument Check

            if (registeredProjectCollection == null)
            {
                throw new ArgumentNullException("registeredProjectCollection");
            }

            #endregion

            this.Name = registeredProjectCollection.Name;
            this.Uri = registeredProjectCollection.Uri;
        }

        #endregion

        #region Public Properties

        public Uri Uri
        {
            get;
            private set;
        }

        public string Name
        {
            get;
            private set;
        }

        #endregion

        #region Public Methods

        public override string ToString()
        {
            return string.Format("{0}. Name = \"{1}\", Uri = \"{2}\"", GetType().Name, this.Name, this.Uri);
        }

        #endregion
    }
}