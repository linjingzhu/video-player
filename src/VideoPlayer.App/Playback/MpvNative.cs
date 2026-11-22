using System.Runtime.InteropServices;
using System.Text;
using VideoPlayer.Core.Playback;

namespace VideoPlayer.App.Playback;

internal static class MpvNative
{
    private const string Library = "libmpv-2";

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr mpv_create();

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    public static extern int mpv_initialize(IntPtr ctx);

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    public static extern void mpv_destroy(IntPtr ctx);

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    public static extern int mpv_set_option_string(IntPtr ctx, byte[] name, byte[] data);

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    public static extern int mpv_set_property_string(IntPtr ctx, byte[] name, byte[] data);

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr mpv_get_property_string(IntPtr ctx, byte[] name);

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    public static extern int mpv_command_string(IntPtr ctx, byte[] args);

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    public static extern void mpv_free(IntPtr data);

    public static byte[] Utf8(string value) => Encoding.UTF8.GetBytes(value + "\0");

    public static string? ReadString(IntPtr ctx, string name)
    {
        var ptr = mpv_get_property_string(ctx, Utf8(name));
        if (ptr == IntPtr.Zero)
        {
            return null;
        }

        try
        {
            return Marshal.PtrToStringUTF8(ptr);
        }
        finally
        {
            mpv_free(ptr);
        }
    }

    public static int Set(IntPtr ctx, string name, string value)
        => mpv_set_property_string(ctx, Utf8(name), Utf8(value));

    public static int Option(IntPtr ctx, string name, string value)
        => mpv_set_option_string(ctx, Utf8(name), Utf8(value));

    public static int Command(IntPtr ctx, params string[] args)
    {
        if (args.Length == 0)
        {
            return -1;
        }

        var command = args[0];
        for (var i = 1; i < args.Length; i++)
        {
            command += " " + Quote(args[i]);
        }

        return mpv_command_string(ctx, Utf8(command));
    }

    private static string Quote(string value)
        => "\"" + value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal) + "\"";

    public const int StringFormat = 1;
    public const int EventNone = 0;
    public const int EventEndFile = 7;
    public const int EventFileLoaded = 8;
    public const int EventPlaybackRestart = 21;
    public const int EventPropertyChange = 22;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void WakeupCallback(IntPtr data);

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    private static extern int mpv_observe_property(IntPtr ctx, ulong replyUserdata, byte[] name, int format);

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr mpv_wait_event(IntPtr ctx, double timeout);

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    private static extern void mpv_set_wakeup_callback(IntPtr ctx, WakeupCallback? callback, IntPtr data);

    public static int Observe(IntPtr ctx, ulong replyUserdata, string name, int format)
        => mpv_observe_property(ctx, replyUserdata, Utf8(name), format);

    public static void SetWakeupCallback(IntPtr ctx, WakeupCallback? callback)
        => mpv_set_wakeup_callback(ctx, callback, IntPtr.Zero);

    public static MpvEventView WaitEvent(IntPtr ctx, double timeoutSeconds)
    {
        var ptr = mpv_wait_event(ctx, timeoutSeconds);
        if (ptr == IntPtr.Zero)
        {
            return default;
        }

        var raw = Marshal.PtrToStructure<MpvEventRaw>(ptr);
        return new MpvEventView(raw.EventId, raw.Error, raw.ReplyUserdata, raw.Data);
    }

    public static (int Reason, int Error) ReadEndFile(IntPtr data)
    {
        if (data == IntPtr.Zero)
        {
            return (OpenSignal.NoEnd, 0);
        }

        var end = Marshal.PtrToStructure<MpvEventEndFileRaw>(data);
        return (end.Reason, end.Error);
    }

    public static (string? Name, string? Value) ReadPropertyChange(IntPtr data)
    {
        if (data == IntPtr.Zero)
        {
            return (null, null);
        }

        var property = Marshal.PtrToStructure<MpvEventPropertyRaw>(data);
        var name = property.Name == IntPtr.Zero ? null : Marshal.PtrToStringUTF8(property.Name);
        string? value = null;
        // mpv_event_property.data for MPV_FORMAT_STRING is char**: *(char **)data.
        if (property.Format == StringFormat && property.Data != IntPtr.Zero)
        {
            var text = Marshal.ReadIntPtr(property.Data);
            if (text != IntPtr.Zero)
            {
                value = Marshal.PtrToStringUTF8(text);
            }
        }

        return (name, value);
    }

    public readonly record struct MpvEventView(int Id, int Error, ulong ReplyUserdata, IntPtr Data);

    // libmpv-2 mpv_event: two ints, then uint64, then pointer. 24 bytes on 64-bit.
    [StructLayout(LayoutKind.Sequential)]
    private struct MpvEventRaw
    {
        public int EventId;
        public int Error;
        public ulong ReplyUserdata;
        public IntPtr Data;
    }

    // First two fields of mpv_event_end_file. Later playlist ids are not read.
    [StructLayout(LayoutKind.Sequential)]
    private struct MpvEventEndFileRaw
    {
        public int Reason;
        public int Error;
    }

    // mpv_event_property. For MPV_FORMAT_STRING, Data is char**.
    [StructLayout(LayoutKind.Sequential)]
    private struct MpvEventPropertyRaw
    {
        public IntPtr Name;
        public int Format;
        public IntPtr Data;
    }
}
