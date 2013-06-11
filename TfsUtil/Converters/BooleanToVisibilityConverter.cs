using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace TfsUtil.Converters
{
    public sealed class BooleanToVisibilityConverter : IValueConverter
    {
        #region Constructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="BooleanToVisibilityConverter"/> class.
        /// </summary>
        public BooleanToVisibilityConverter()
        {
            this.TrueVisibility = Visibility.Visible;
            this.FalseVisibility = Visibility.Hidden;
        }

        #endregion

        #region Public Properties

        public Visibility TrueVisibility
        {
            get;
            set;
        }

        public Visibility FalseVisibility
        {
            get;
            set;
        }

        #endregion

        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            #region Argument Check

            if (!(value is bool))
            {
                throw new ArgumentException(@"The value type must be Boolean.", "value");
            }

            #endregion

            var convertedValue = (bool)value;
            return convertedValue ? this.TrueVisibility : this.FalseVisibility;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        #endregion
    }
}