using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace TfsUtil
{
    /// <summary>
    ///     Contains interaction logic for ProgressWindow.xaml.
    /// </summary>
    public sealed partial class ProgressWindow : Window
    {
        #region Fields

        private readonly object _resultLock = new object();
        private readonly Thread _thread;
        private ProgressResult _result;
        private Func<ProgressWindow, object> _action;
        private bool _isCancelling;

        #endregion

        #region Constructors

        /// <summary>
        ///     Prevents a default instance of the <see cref="ProgressWindow"/> class from being created.
        /// </summary>
        private ProgressWindow()
        {
            InitializeComponent();

            _thread = new Thread(this.DoWork) { Name = GetType().Name };
        }

        #endregion

        #region Public Properties

        public string OperationDescription
        {
            get
            {
                return this.OperationDescriptionTextBlock.GetValueSafe(c => c.Text);
            }

            set
            {
                this.OperationDescriptionTextBlock.ExecuteActionSafe(c => c.Text = value ?? string.Empty);
            }
        }

        public ProgressResult Result
        {
            [DebuggerNonUserCode]
            get
            {
                lock (_resultLock)
                {
                    return _result;
                }
            }

            [DebuggerNonUserCode]
            private set
            {
                lock (_resultLock)
                {
                    _result = value;
                }
            }
        }

        #endregion

        #region Public Methods

        public static ProgressResult Execute(
            Window owner,
            string title,
            string operationDescription,
            Func<ProgressWindow, object> action)
        {
            #region Argument Check

            if (string.IsNullOrEmpty(title))
            {
                throw new ArgumentException("The value can be neither empty string nor null.", "title");
            }

            if (string.IsNullOrEmpty(operationDescription))
            {
                throw new ArgumentException("The value can be neither empty string nor null.", "operationDescription");
            }

            if (action == null)
            {
                throw new ArgumentNullException("action");
            }

            #endregion

            var progressWindow = new ProgressWindow
            {
                Owner = owner,
                Title = title,
                OperationDescription = operationDescription,
                _action = action
            };

            progressWindow.ShowDialog();

            return progressWindow.Result.EnsureNotNull();
        }

        #endregion

        #region Protected Methods

        protected override void OnClosing(CancelEventArgs e)
        {
            if (AskCancel())
            {
                e.Cancel = true;
            }

            base.OnClosing(e);
        }

        #endregion

        #region Private Methods

        #region Regular

        private bool AskCancel()
        {
            if (this.Result != null)
            {
                return false;
            }

            if (_isCancelling)
            {
                return true;
            }

            var mbr = MessageBox.Show(
                this,
                "Do you wish to interrupt the current operation?",
                this.Title,
                MessageBoxButton.YesNo,
                MessageBoxImage.Question,
                MessageBoxResult.Yes);
            if (mbr != MessageBoxResult.Yes)
            {
                return true;
            }

            _isCancelling = true;
            this.CancelButton.IsEnabled = false;

            if (_thread.IsAlive)
            {
                _thread.Abort();
            }

            return true;
        }

        private void DoWork()
        {
            object actionResult;
            try
            {
                actionResult = _action(this);
            }
            catch (Exception ex)
            {
                if (ex.IsThreadAbort())
                {
                    OnFinishedAsync(new ProgressResult(null, true, null));
                    throw;
                }

                OnFinishedAsync(new ProgressResult(null, false, ex));
                return;
            }

            OnFinishedAsync(new ProgressResult(actionResult, false, null));
        }

        private void OnFinishedAsync(ProgressResult result)
        {
            this.Dispatcher.BeginInvoke(
                DispatcherPriority.Normal,
                new Action<ProgressResult>(this.OnFinished),
                result);
        }

        private void OnFinished(ProgressResult result)
        {
            #region Argument Check

            if (result == null)
            {
                throw new ArgumentNullException("result");
            }

            #endregion

            this.Result = result;
            this.Close();
        }

        #endregion

        #region Event Handlers

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (!this.IsModal())
            {
                throw new InvalidOperationException(
                    string.Format("The '{0}' instance must only be shown modally.", GetType().Name));
            }

            if (this.Result != null)
            {
                throw new InvalidOperationException(
                    string.Format("The '{0}' instance can only be run once.", GetType().Name));
            }

            if (_action != null)
            {
                _thread.Start();
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            AskCancel();
        }

        #endregion

        #endregion
    }
}