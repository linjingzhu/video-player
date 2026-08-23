using System.IO;
using System.IO.Pipes;
using System.Windows;
using VideoPlayer.Core.Shell;

namespace VideoPlayer.App;

public partial class App : System.Windows.Application
{
    public const string MutexName = @"Local\Ieseo.SingleInstance";
    public const string PipeName = "Ieseo.HandOff";

    private Mutex? _mutex;
    private MainWindow? _main;

    protected override void OnStartup(StartupEventArgs e)
    {
        var createdNew = false;
        _mutex = new Mutex(true, MutexName, out createdNew);
        var args = Environment.GetCommandLineArgs().Skip(1).ToArray();

        if (!createdNew)
        {
            SingleInstance.SendToExisting(args);
            Shutdown();
            return;
        }

        ShutdownMode = ShutdownMode.OnMainWindowClose;
        _main = new MainWindow();
        MainWindow = _main;
        _main.Title = UiCopy.AppTitle;
        _main.Show();
        if (args.Length > 0)
        {
            _main.OpenFromCommandLine(args);
        }

        _ = SingleInstance.ListenAsync(paths =>
            Dispatcher.Invoke(() => _main?.OpenFromCommandLine(paths)));
        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _mutex?.ReleaseMutex();
        _mutex?.Dispose();
        base.OnExit(e);
    }
}

internal static class SingleInstance
{
    public static void SendToExisting(IEnumerable<string> paths)
    {
        try
        {
            using var client = new NamedPipeClientStream(".", App.PipeName, PipeDirection.Out);
            client.Connect(800);
            using var writer = new StreamWriter(client);
            foreach (var path in paths)
            {
                writer.WriteLine(path);
            }
        }
        catch (IOException)
        {
            // Existing window could not accept the hand-off; this process is already exiting.
        }
        catch (TimeoutException)
        {
        }
    }

    public static async Task ListenAsync(Action<string[]> onPaths)
    {
        while (true)
        {
            try
            {
                using var server = new NamedPipeServerStream(App.PipeName, PipeDirection.In, 1);
                await server.WaitForConnectionAsync().ConfigureAwait(false);
                using var reader = new StreamReader(server);
                var lines = new List<string>();
                string? line;
                while ((line = await reader.ReadLineAsync().ConfigureAwait(false)) is not null)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        lines.Add(line);
                    }
                }

                if (lines.Count > 0)
                {
                    onPaths(lines.ToArray());
                }
            }
            catch (IOException)
            {
                await Task.Delay(400).ConfigureAwait(false);
            }
        }
    }
}
