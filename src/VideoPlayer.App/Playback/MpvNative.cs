using System.Runtime.InteropServices;
using System.Text;

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
}
