using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Shell;
using System.Windows.Threading;

namespace TfsUtil
{
    /// <summary>
    ///     Contains interaction logic for ProgressWindow.xaml.
    /// </summary>
    public partial class ProgressWindow : Window
    {
        #region Fields

        private readonly object m_resultLock = new object();
        private ProgressResult m_result;
        private readonly Thread m_thread;
        private Func<ProgressWindow, object> m_action;
        private bool m_isCancelling;

        #endregion

        #region Constructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="ProgressWindow"/> class.
        /// </summary>
        private ProgressWindow()
        {
            InitializeComponent();

            m_thread = new Thread(this.DoWork) { Name = GetType().Name };
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

            if (m_isCancelling)
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

            m_isCancelling = true;
            this.CancelButton.IsEnabled = false;

            if (m_thread.IsAlive)
            {
                m_thread.Abort();
            }

            return true;
        }

        private void DoWork()
        {
            object actionResult;
            try
            {
                actionResult = m_action(this);
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
            if (!ComponentDispatcher.IsThreadModal)
            {
                throw new InvalidOperationException(
                    string.Format("The '{0}' instance must only be shown modally.", GetType().Name));
            }

            if (this.Result != null)
            {
                throw new InvalidOperationException(
                    string.Format("The '{0}' instance can only be run once.", GetType().Name));
            }

            if (m_action != null)
            {
                m_thread.Start();
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            AskCancel();
        }

        #endregion

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
                lock (m_resultLock)
                {
                    return m_result;
                }
            }
            [DebuggerNonUserCode]
            private set
            {
                lock (m_resultLock)
                {
                    m_result = value;
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
                m_action = action
            };

            progressWindow.ShowDialog();

            return progressWindow.Result;  // ?? new ProgressResult(null, true, null);
        }

        #endregion
    }
}