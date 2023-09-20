using Qualia.Tools;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace Qualia;

public partial class App : Application
{
    //[assembly: System.Reflection.AssemblyVersion("1.0.*")]

    public App()
    {
        Logger.LogFileName = WorkingDirectory + "log.txt";

        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_OnUnhandledException;
        TaskScheduler.UnobservedTaskException += TaskScheduler_OnUnobservedTaskException;
    }

    public static string WorkingDirectory => Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar;

    private void TaskScheduler_OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
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

    private void CurrentDomain_OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
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