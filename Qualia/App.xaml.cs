using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace Qualia
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            Logger.LogFileName = WorkingDirectory + "log.txt";
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
        }

        public static string WorkingDirectory => AppDomain.CurrentDomain.BaseDirectory;

        private void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            try
            {
                Logger.OnUnhandledException(e.Exception);
            }
            finally
            {
                TerminateApplication();
            }
        }

        private void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            try
            {
                Logger.OnUnhandledException(e.Exception);
            }
            finally
            {
                TerminateApplication();
            }
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            try
            {
                Logger.OnUnhandledException((Exception)e.ExceptionObject);
            }
            finally
            {
                TerminateApplication();
            }
        }

        private void TerminateApplication()
        {
            try
            {
                if (Current != null)
                {
                    Current.Dispatcher.Invoke(() =>
                    {
                        try
                        {
                            Current.Shutdown();
                        }
                        catch (Exception ex)
                        {
                            Logger.LogException(ex);
                        }
                    });
                }
            }
            catch (Exception ex2)
            {
                Logger.LogException(ex2);
            }
        }
    }
}
