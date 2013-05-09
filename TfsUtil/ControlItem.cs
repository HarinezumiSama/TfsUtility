using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace TfsUtil
{
    public sealed class ControlItem<T> : IEquatable<ControlItem<T>>
    {
        #region Constructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="ControlItem&lt;T&gt;"/> class
        ///     using the specified item and corresponding text.
        /// </summary>
        public ControlItem(T item, string text)
        {
            this.Item = item;
            this.Text = text ?? string.Empty;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ControlItem&lt;T&gt;"/> class
        ///     using the specified item.
        /// </summary>
        public ControlItem(T item)
            : this(item, ReferenceEquals(item, null) ? string.Empty : item.ToString())
        {
            // Nothing to do
        }

        #endregion

        #region Public Properties

        public T Item
        {
            get;
            private set;
        }

        public string Text
        {
            get;
            private set;
        }

        #endregion

        #region Public Methods

        public override bool Equals(object obj)
        {
            return Equals(obj as ControlItem<T>);
        }

        public override int GetHashCode()
        {
            return ReferenceEquals(this.Item, null) ? 0 : this.Item.GetHashCode();
        }

        public override string ToString()
        {
            return this.Text;
        }

        #endregion

        #region IEquatable<ControlItem<T>> Members

        public bool Equals(ControlItem<T> other)
        {
            if (ReferenceEquals(other, null))
            {
                return false;
            }

            if (ReferenceEquals(other, this))
            {
                return true;
            }

            return EqualityComparer<T>.Default.Equals(this.Item, other.Item);
        }

        #endregion
    }
}