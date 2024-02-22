using System.IO;
using System.Windows;
using System.IO.Pipes;

namespace Caffeine_Pro.Classes;

internal class SingletonService : IDisposable
{
    public delegate void CommandReceivedHandler(string command);
    public event CommandReceivedHandler? CommandReceived;

    private static Mutex? _mutex;
    private NamedPipeServerStream? _pipeServer;


    private const string AppName = "CaffeinePro";
    private const string PipeName = AppName + "Pipe7898761321544623435";

    public bool CheckSingleton()
    {
        _mutex = new Mutex(true, AppName, out var createdNew);
        if (!createdNew)
            return false;

        // Listen for commands from other instances
        _pipeServer = new NamedPipeServerStream(PipeName, PipeDirection.In);
        _pipeServer.BeginWaitForConnection(OtherInstanceRequestedACommand, _pipeServer);

        return true;
    }

    private void OtherInstanceRequestedACommand(IAsyncResult ar)
    {
        _pipeServer = (NamedPipeServerStream)ar.AsyncState!;
        using var reader = new StreamReader(_pipeServer);
        var command = reader.ReadToEnd();
        if (!string.IsNullOrEmpty(command))
        {
            CommandReceived?.Invoke(command);
        }
    }

    public void SendCommandToTheRunningInstance(string command)
    {
        using var pipeClient = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
        pipeClient.Connect();
        using var writer = new StreamWriter(pipeClient);
        writer.Write(command);
    }

    ~SingletonService()
    {
        ReleaseUnmanagedResources();
    }

    private void ReleaseUnmanagedResources()
    {
        _mutex?.Dispose();
        _mutex = null;

        _pipeServer?.Close();
        _pipeServer?.Dispose();
        _pipeServer = null;
    }

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }
}
