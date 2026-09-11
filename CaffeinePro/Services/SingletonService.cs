using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Windows;
using CaffeinePro.Classes;
using CaffeinePro.Localization;
using CaffeinePro.Windows;

namespace CaffeinePro.Services;

/// <summary>
/// This class is used to ensure that only one instance of the application is running
/// </summary>
public class SingletonService(ParameterProcessorService parameterProcessorService) : IDisposable
{

    private static Mutex? _mutex;
    private NamedPipeServerStream? _pipeServer;

    private const string MutexName = "CaffeineProMutex";
    private const string PipeName = "CaffeineProPipe";

    public void IfAnotherInstanceExistsSendArgsAndExit(string[] commandlineArgs)
    {
        if (IsThisTheOnlyInstance())
        {
            return;
        }

        switch (commandlineArgs.Length)
        {
            case 0:
                Dialogs.Show(LocalizationService.Get("Singleton_AlreadyRunning"),
                    LocalizationService.Get("App_Name"), MessageBoxButton.OK,
                    MessageBoxImage.Information);
                break;
            default:
                // If another instance is already running, send the arguments to that instance
                SendCommandToTheRunningInstance(string.Join(" ", commandlineArgs));
                break;
        }

        App.CurrentApp.Shutdown();
    }
    
    /// <summary>
    /// Checks if the application is already running
    /// </summary>
    /// <returns></returns>
    private bool IsThisTheOnlyInstance()
    {
        _mutex = new Mutex(true, MutexName, out var createdNew);
        if (!createdNew)
        {
            return false;
        }

        // Listen for commands from other instances through a named pipe
        StartListening();

        return true;
    }

    /// <summary>
    /// Puts a fresh pipe server up and waits for the next command.
    /// </summary>
    /// <remarks>
    /// A server stream serves one connection, so this is called again after each command. Without
    /// that the first forwarded command was the only one the running instance ever accepted, and
    /// every later <c>CaffeinePro.exe activate</c> blocked in Connect() forever - a process left
    /// hanging with no window, for what looks like a command that simply did nothing.
    /// </remarks>
    private void StartListening()
    {
        try
        {
            _pipeServer = new NamedPipeServerStream(PipeName, PipeDirection.In);
            _pipeServer.BeginWaitForConnection(OtherInstanceRequestedACommand, _pipeServer);
        }
        catch (IOException)
        {
            // The name is taken, which means another instance is already listening. Nothing to do:
            // this one is not the singleton and is on its way out.
        }
        catch (ObjectDisposedException)
        {
            // Shutting down.
        }
    }

    /// <summary>
    /// Send a command to the running instance through a named pipe
    /// </summary>
    /// <param name="command"></param>
    private void SendCommandToTheRunningInstance(string command)
    {
        try
        {
            using var pipeClient = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);

            // Bounded, so a running instance that is wedged or shutting down costs a few seconds
            // rather than leaving this process hanging in Connect() with nothing on screen.
            pipeClient.Connect(ConnectTimeoutMs);

            // The command may ask for something that has to come to the front - the tray menu does,
            // and a menu without the focus closes itself. Only the process that currently holds the
            // foreground can grant that, and for these few seconds that is this one, on its way out.
            AllowSetForegroundWindow(AsfwAny);

            using var writer = new StreamWriter(pipeClient);
            writer.Write(command);
        }
        catch (TimeoutException)
        {
            Dialogs.Show(LocalizationService.Get("Singleton_NoResponse"),
                LocalizationService.Get("App_Name"), MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    /// <summary>
    /// How long to wait for the running instance to pick up the pipe.
    /// </summary>
    private const int ConnectTimeoutMs = 5000;

    /// <summary>
    /// ASFW_ANY: let any process take the foreground, rather than naming one by process id.
    /// </summary>
    private const int AsfwAny = -1;

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AllowSetForegroundWindow(int dwProcessId);

    /// <summary>
    /// This function is the handler for the named pipe server, and it is called when another instance sends a command
    /// </summary>
    /// <param name="ar"></param>
    private void OtherInstanceRequestedACommand(IAsyncResult ar)
    {
        var server = (NamedPipeServerStream)ar.AsyncState!;
        string command;

        try
        {
            server.EndWaitForConnection(ar);

            using var reader = new StreamReader(server);
            command = reader.ReadToEnd();
        }
        catch (ObjectDisposedException)
        {
            // Disposed from under us while shutting down.
            return;
        }
        catch (IOException)
        {
            // The sender went away mid-write. Still worth listening again below.
            command = string.Empty;
        }
        finally
        {
            server.Dispose();
        }

        // Listening again before the command runs, not after: processing can show a dialog and
        // block this thread for as long as it is up, and a command arriving meanwhile should queue
        // on the pipe rather than find nothing listening.
        StartListening();

        if (!string.IsNullOrEmpty(command))
        {
            parameterProcessorService.ProcessArgs(command.Split(" "));
        }
    }

    /// <summary>
    /// Clean up resources
    /// </summary>
    ~SingletonService()
    {
        Dispose();
    }

    /// <summary>
    /// Clean up resources
    /// </summary>
    public void Dispose()
    {
        _mutex?.Dispose();
        _mutex = null;

        _pipeServer?.Close();
        _pipeServer?.Dispose();
        _pipeServer = null;
        GC.SuppressFinalize(this);
    }
}
