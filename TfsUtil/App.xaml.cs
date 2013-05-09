using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;

namespace TfsUtil
{
    /// <summary>
    ///     Interaction logic for App.xaml.
    /// </summary>
    public partial class App
    {
        #region Constructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="App"/> class.
        /// </summary>
        public App()
        {
            Initialize();

            AppDomain.CurrentDomain.UnhandledException += this.CurrentDomain_UnhandledException;
        }

        #endregion

        #region Public Properties

        public static new App Current
        {
            [DebuggerNonUserCode]
            get
            {
                return (App)Application.Current;
            }
        }

        public string ProductName
        {
            get;
            private set;
        }

        public Version ProductVersion
        {
            get;
            private set;
        }

        public string ProductCopyright
        {
            get;
            private set;
        }

        public string FullProductName
        {
            get;
            private set;
        }

        public string FullProductDescription
        {
            get;
            private set;
        }

        #endregion

        #region Public Methods

        /// <summary>
        ///     Temporary workaround till multithreaded execution is implemented.
        /// </summary>
        public static void DoEvents()
        {
            Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { }));
        }

        #endregion

        #region Private Methods

        private static void KillThisProcess()
        {
            Process.GetCurrentProcess().Kill();
        }

        private void Initialize()
        {
            try
            {
                InitializeProperties();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    string.Format(
                        "The application failed to initialize properly:{0}"
                            + "{0}"
                            + "[{1}] {2}{0}"
                            + "{0}"
                            + "The application will now terminate.",
                        Environment.NewLine,
                        ex.GetType().FullName,
                        ex.Message),
                    typeof(App).Namespace,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                KillThisProcess();
            }
        }

        private void InitializeProperties()
        {
            var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();

            //// Independent properties

            this.ProductName = assembly.GetSoleAttributeStrict<AssemblyProductAttribute>().Product;
            this.ProductVersion = assembly.GetName().Version;
            this.ProductCopyright = assembly.GetSoleAttributeStrict<AssemblyCopyrightAttribute>().Copyright;

            //// Dependent properties

            this.FullProductName = string.Format("{0} {1}", this.ProductName, this.ProductVersion);
            this.FullProductDescription = string.Format(
                "{0} {1} {2}",
                this.ProductName,
                this.ProductVersion,
                this.ProductCopyright);
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception;
            var typeName = exception == null ? "<UnknownException>" : exception.GetType().FullName;
            var message = exception == null ? "(Unknown error)" : exception.Message;

            MessageBox.Show(
                string.Format(
                    "Unhandled exception has occurred:{0}"
                        + "{0}"
                        + "[{1}] {2}{0}"
                        + "{0}"
                        + "The application will now terminate.",
                    Environment.NewLine,
                    typeName,
                    message),
                this.ProductName,
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            KillThisProcess();
        }

        #endregion
    }
}