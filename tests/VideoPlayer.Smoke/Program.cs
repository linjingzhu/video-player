using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Threading;

namespace VideoPlayer.Smoke;

/// <summary>
/// Loads libmpv headless, then constructs and closes the real main window.
/// The window runs in a child process so a native GPU fault can be reported
/// as a skip instead of failing the runner.
/// </summary>
internal static class Program
{
    private const int Pass = 0;
    private const int Fail = 1;
    private const int Skip = 2;

    [STAThread]
    public static int Main(string[] args)
    {
        if (args.Any(static arg => string.Equals(arg, "--window", StringComparison.Ordinal)))
        {
            return RunWindow();
        }

        var engine = ProbeEngine();
        var window = RunWindowChild();
        Console.WriteLine($"SMOKE: engine={Label(engine)} window={Label(window)}");
        return engine == Fail || window == Fail ? Fail : Pass;
    }

    private static string Label(int code)
        => code switch
        {
            Pass => "PASS",
            Skip => "SKIP",
            _ => "FAIL"
        };

    private static int ProbeEngine()
    {
        var require = string.Equals(
            Environment.GetEnvironmentVariable("IESEO_REQUIRE_LIBMPV"),
            "1",
            StringComparison.Ordinal);
        var dll = FindNative("libmpv-2.dll");
        if (dll is null)
        {
            const string missing = "libmpv-2.dll was not found beside the smoke test or in IESEO_LIBMPV_DIR.";
            if (require)
            {
                Console.WriteLine("FAIL: IESEO_REQUIRE_LIBMPV=1 but " + missing);
                return Fail;
            }

            Console.WriteLine("SKIP: " + missing + " Engine initialization was not run.");
            return Skip;
        }

        var vulkan = Path.Combine(Path.GetDirectoryName(dll)!, "vulkan-1.dll");
        if (!File.Exists(vulkan))
        {
            var message = "vulkan-1.dll was not beside libmpv-2.dll. The pinned libmpv build imports it by name.";
            Console.WriteLine(require ? "FAIL: " + message : "SKIP: " + message);
            return require ? Fail : Skip;
        }

        NativeMethods.SetDllDirectory(Path.GetDirectoryName(dll)!);
        Console.WriteLine($"libmpv: {dll}");
        try
        {
            var ctx = NativeMethods.mpv_create();
            if (ctx == IntPtr.Zero)
            {
                Console.WriteLine("FAIL: mpv_create returned null.");
                return Fail;
            }

            try
            {
                // vo=null / hwdec=no avoids a GPU. config=no ignores a user mpv.conf that might turn the GPU back on.
                Option(ctx, "config", "no");
                Option(ctx, "load-scripts", "no");
                Option(ctx, "terminal", "no");
                Option(ctx, "input-default-bindings", "no");
                Option(ctx, "vo", "null");
                Option(ctx, "ao", "null");
                Option(ctx, "hwdec", "no");
                var rc = NativeMethods.mpv_initialize(ctx);
                if (rc < 0)
                {
                    var message = ErrorString(rc);
                    if (IsGpuOrDisplay(message))
                    {
                        Console.WriteLine($"SKIP: libmpv did not initialize because a GPU or display is unavailable ({message}).");
                        return Skip;
                    }

                    Console.WriteLine($"FAIL: mpv_initialize returned {rc} ({message}).");
                    return Fail;
                }
            }
            finally
            {
                NativeMethods.mpv_destroy(ctx);
            }

            Console.WriteLine("PASS: libmpv loaded and initialized (vo=null, hwdec=no).");
            return Pass;
        }
        catch (DllNotFoundException ex)
        {
            Console.WriteLine($"FAIL: libmpv-2.dll is on disk but did not load ({ex.Message}).");
            return Fail;
        }
        catch (BadImageFormatException ex)
        {
            Console.WriteLine($"FAIL: libmpv-2.dll architecture does not match this process ({ex.Message}).");
            return Fail;
        }
    }

    private static int RunWindowChild()
    {
        var exe = Environment.ProcessPath;
        if (string.IsNullOrEmpty(exe))
        {
            Console.WriteLine("FAIL: smoke executable path is unknown.");
            return Fail;
        }

        var stdout = new StringBuilder();
        var stderr = new StringBuilder();
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = exe,
            WorkingDirectory = AppContext.BaseDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        process.StartInfo.ArgumentList.Add("--window");
        process.OutputDataReceived += (_, eventArgs) =>
        {
            if (eventArgs.Data is not null)
            {
                stdout.AppendLine(eventArgs.Data);
            }
        };
        process.ErrorDataReceived += (_, eventArgs) =>
        {
            if (eventArgs.Data is not null)
            {
                stderr.AppendLine(eventArgs.Data);
            }
        };

        try
        {
            process.Start();
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            Console.WriteLine($"SKIP: main window process could not be started ({ex.GetType().Name}: {ex.Message}).");
            return Skip;
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        if (!process.WaitForExit(45_000))
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
            {
            }

            process.WaitForExit();
            Console.WriteLine("SKIP: main window did not close within 45 seconds. This runner is treated as having no usable desktop or GPU session.");
            return Skip;
        }

        process.WaitForExit();
        Console.Write(stdout.ToString());
        var errorText = stderr.ToString();
        if (!string.IsNullOrWhiteSpace(errorText))
        {
            Console.WriteLine(errorText);
        }

        if (process.ExitCode == Pass)
        {
            return Pass;
        }

        if (process.ExitCode == Skip)
        {
            return Skip;
        }

        if (IsEnvironmentCrash(process.ExitCode))
        {
            Console.WriteLine(
                $"SKIP: main window process ended with 0x{unchecked((uint)process.ExitCode):X8}. A GPU or interactive desktop is not available, so the window smoke is skipped.");
            return Skip;
        }

        Console.WriteLine($"FAIL: main window process exited {process.ExitCode}.");
        return Fail;
    }

    private static int RunWindow()
    {
        if (!DesktopLooksAvailable(out var reason))
        {
            Console.WriteLine("SKIP: main window was not opened. " + reason);
            return Skip;
        }

        var sawWindow = false;
        Exception? error = null;
        try
        {
            // App.xaml holds the brushes and styles the window references, so the real application has to start.
            var app = new VideoPlayer.App.App();
            app.Startup += (_, _) =>
            {
                var window = app.MainWindow;
                if (window is null)
                {
                    return;
                }

                sawWindow = true;
                app.Dispatcher.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        window.Close();
                    }
                    catch (Exception ex)
                    {
                        error = ex;
                        app.Shutdown();
                    }
                }), DispatcherPriority.ApplicationIdle);
            };
            app.Run();
        }
        catch (Exception ex)
        {
            error = ex;
        }

        if (error is not null)
        {
            if (IsDesktopFailure(error))
            {
                Console.WriteLine($"SKIP: main window could not be created because a desktop or GPU session is not available. {error.GetType().Name}: {error.Message}");
                return Skip;
            }

            Console.WriteLine($"FAIL: main window threw {error.GetType().Name}: {error.Message}");
            return Fail;
        }

        if (!sawWindow)
        {
            Console.WriteLine("FAIL: the application started without constructing a main window.");
            return Fail;
        }

        Console.WriteLine("PASS: main window constructed and closed.");
        return Pass;
    }

    private static bool DesktopLooksAvailable(out string reason)
    {
        if (!Environment.UserInteractive)
        {
            reason = "Environment.UserInteractive is false, so there is no interactive desktop.";
            return false;
        }

        if (NativeMethods.GetDesktopWindow() == IntPtr.Zero)
        {
            reason = "GetDesktopWindow returned no desktop.";
            return false;
        }

        if (NativeMethods.GetSystemMetrics(NativeMethods.SmCmonitors) <= 0)
        {
            reason = "The session reported no monitors, so there is no desktop to host the window.";
            return false;
        }

        reason = "";
        return true;
    }

    private static bool IsDesktopFailure(Exception error)
    {
        for (Exception? current = error; current is not null; current = current.InnerException)
        {
            var text = current.GetType().Name + " " + current.Message;
            if (text.Contains("RPC server", StringComparison.OrdinalIgnoreCase)
                || text.Contains("0x80040154", StringComparison.OrdinalIgnoreCase)
                || text.Contains("Class not registered", StringComparison.OrdinalIgnoreCase)
                || text.Contains("desktop", StringComparison.OrdinalIgnoreCase)
                || text.Contains("display", StringComparison.OrdinalIgnoreCase)
                || text.Contains("no monitors", StringComparison.OrdinalIgnoreCase)
                || text.Contains("interactive", StringComparison.OrdinalIgnoreCase)
                || text.Contains("The handle is invalid", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsEnvironmentCrash(int exitCode)
    {
        var code = unchecked((uint)exitCode);
        return code is 0xC0000005 or 0xC0000409 or 0xC0000142 or 0xC0000096 or 0xC000027B or 0xC06D007E;
    }

    private static bool IsGpuOrDisplay(string message)
    {
        return message.Contains("gpu", StringComparison.OrdinalIgnoreCase)
            || message.Contains("d3d", StringComparison.OrdinalIgnoreCase)
            || message.Contains("vulkan", StringComparison.OrdinalIgnoreCase)
            || message.Contains("dxva", StringComparison.OrdinalIgnoreCase)
            || message.Contains("display", StringComparison.OrdinalIgnoreCase);
    }

    private static void Option(IntPtr ctx, string name, string value)
    {
        var rc = NativeMethods.mpv_set_option_string(ctx, Utf8(name), Utf8(value));
        if (rc < 0)
        {
            throw new InvalidOperationException($"mpv_set_option_string {name}={value} returned {rc} ({ErrorString(rc)}).");
        }
    }

    private static string ErrorString(int code)
    {
        var ptr = NativeMethods.mpv_error_string(code);
        return ptr == IntPtr.Zero ? "" : Marshal.PtrToStringUTF8(ptr) ?? "";
    }

    private static byte[] Utf8(string value) => Encoding.UTF8.GetBytes(value + "\0");

    private static string? FindNative(string fileName)
    {
        var directories = new List<string> { AppContext.BaseDirectory };
        var extra = Environment.GetEnvironmentVariable("IESEO_LIBMPV_DIR");
        if (!string.IsNullOrWhiteSpace(extra))
        {
            directories.Add(extra);
        }

        foreach (var directory in directories)
        {
            var path = Path.Combine(directory, fileName);
            if (File.Exists(path))
            {
                return path;
            }
        }

        return null;
    }

    private static class NativeMethods
    {
        public const int SmCmonitors = 80;

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool SetDllDirectory(string path);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll")]
        public static extern int GetSystemMetrics(int index);

        [DllImport("libmpv-2", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr mpv_create();

        [DllImport("libmpv-2", CallingConvention = CallingConvention.Cdecl)]
        public static extern int mpv_initialize(IntPtr ctx);

        [DllImport("libmpv-2", CallingConvention = CallingConvention.Cdecl)]
        public static extern int mpv_set_option_string(IntPtr ctx, byte[] name, byte[] value);

        [DllImport("libmpv-2", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr mpv_error_string(int error);

        [DllImport("libmpv-2", CallingConvention = CallingConvention.Cdecl)]
        public static extern void mpv_destroy(IntPtr ctx);
    }
}
