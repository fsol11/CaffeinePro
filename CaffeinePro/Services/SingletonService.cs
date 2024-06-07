using System.IO;
using System.IO.Pipes;
using System.Windows;
using CaffeinePro.Classes;

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
                MessageBox.Show("An instance of the application is already running.",
                    App.AppName, MessageBoxButton.OK, MessageBoxImage.Information);
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
        _pipeServer = new NamedPipeServerStream(PipeName, PipeDirection.In);
        _pipeServer.BeginWaitForConnection(OtherInstanceRequestedACommand, _pipeServer);

        return true;
    }

    /// <summary>
    /// Send a command to the running instance through a named pipe
    /// </summary>
    /// <param name="command"></param>
    private void SendCommandToTheRunningInstance(string command)
    {
        using var pipeClient = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
        pipeClient.Connect();
        using var writer = new StreamWriter(pipeClient);
        writer.Write(command);
    }

    /// <summary>
    /// This function is the handler for the named pipe server, and it is called when another instance sends a command
    /// </summary>
    /// <param name="ar"></param>
    private void OtherInstanceRequestedACommand(IAsyncResult ar)
    {
        _pipeServer = (NamedPipeServerStream)ar.AsyncState!;
        using var reader = new StreamReader(_pipeServer);
        var command = reader.ReadToEnd();
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
