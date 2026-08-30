using System.Diagnostics;
using System.Runtime.InteropServices;
using VideoPlayer.Core.Playback;

namespace VideoPlayer.App.Projection;

/// <summary>
/// Windows Miracast / wireless-display host. Opens the OS picker
/// (<see cref="MiracastProjection.ProjectionManagerClass"/> /
/// Connect <c>ms-settings-connect:</c>). Does not build a device list.
/// </summary>
public sealed class WindowsProjectionHost : IWirelessDisplayHost
{
    public bool IsProjecting => NativeWirelessDisplay.HasActiveMiracast();

    public WirelessDisplayResult Start()
        => MiracastProjection.Guarded(() =>
        {
            if (!NativeWirelessDisplay.TryOpenOsPicker())
            {
                return WirelessDisplayResult.Failed();
            }

            return WirelessDisplayResult.PickerOpened();
        });

    public WirelessDisplayResult Stop()
        => MiracastProjection.Guarded(() =>
        {
            if (!NativeWirelessDisplay.HasActiveMiracast())
            {
                return WirelessDisplayResult.Disconnected();
            }

            return NativeWirelessDisplay.TryDisconnectMiracast()
                ? WirelessDisplayResult.Disconnected()
                : WirelessDisplayResult.Failed();
        });
}

internal static class NativeWirelessDisplay
{
    private const uint QdcOnlyActivePaths = 2;
    private const uint QdcAllPaths = 1;
    private const uint DisplayConfigPathActive = 0x1;
    private const uint SdcApply = 0x00000080;
    private const uint SdcUseSuppliedDisplayConfig = 0x00000020;
    private const uint SdcSaveToDatabase = 0x00000100;
    private const uint SdcAllowChanges = 0x00000400;

    public static bool TryOpenOsPicker()
    {
        if (TryRequestStartProjecting())
        {
            return true;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = MiracastProjection.ConnectPickerUri,
                UseShellExecute = true
            });
            return true;
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException or System.IO.IOException)
        {
            return false;
        }
    }

    /// <summary>
    /// ProjectionManager.RequestStartProjectingAsync is the native UWP picker.
    /// Win32 activates the same Connect surface when the runtime class is present.
    /// </summary>
    public static bool TryRequestStartProjecting()
    {
        try
        {
            var type = Type.GetType(
                $"{MiracastProjection.ProjectionManagerClass}, Windows, ContentType=WindowsRuntime",
                throwOnError: false);
            if (type is null)
            {
                return false;
            }

            var method = type.GetMethod("RequestStartProjectingAsync", Type.EmptyTypes);
            method?.Invoke(null, null);
            return method is not null;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public static bool HasActiveMiracast()
    {
        try
        {
            return TryQueryPaths(QdcOnlyActivePaths, out var paths)
                   && paths.Any(IsActiveMiracast);
        }
        catch (Exception)
        {
            return false;
        }
    }

    public static bool TryDisconnectMiracast()
    {
        try
        {
            if (!TryQueryPaths(QdcAllPaths, out var paths, out var modes))
            {
                return false;
            }

            var changed = false;
            for (var i = 0; i < paths.Length; i++)
            {
                if (!IsMiracast(paths[i]))
                {
                    continue;
                }

                paths[i].flags &= ~DisplayConfigPathActive;
                changed = true;
            }

            if (!changed)
            {
                return !HasActiveMiracast();
            }

            var status = SetDisplayConfig(
                (uint)paths.Length,
                paths,
                (uint)modes.Length,
                modes,
                SdcApply | SdcUseSuppliedDisplayConfig | SdcSaveToDatabase | SdcAllowChanges);
            return status == 0 && !HasActiveMiracast();
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static bool IsActiveMiracast(DISPLAYCONFIG_PATH_INFO path)
        => (path.flags & DisplayConfigPathActive) != 0 && IsMiracast(path);

    private static bool IsMiracast(DISPLAYCONFIG_PATH_INFO path)
        => path.targetInfo.outputTechnology == MiracastProjection.MiracastOutputTechnology;

    private static bool TryQueryPaths(uint flags, out DISPLAYCONFIG_PATH_INFO[] paths)
        => TryQueryPaths(flags, out paths, out _);

    private static bool TryQueryPaths(uint flags, out DISPLAYCONFIG_PATH_INFO[] paths, out DISPLAYCONFIG_MODE_INFO[] modes)
    {
        paths = [];
        modes = [];
        var err = GetDisplayConfigBufferSizes(flags, out var pathCount, out var modeCount);
        if (err != 0 || pathCount == 0)
        {
            return false;
        }

        paths = new DISPLAYCONFIG_PATH_INFO[pathCount];
        modes = new DISPLAYCONFIG_MODE_INFO[modeCount];
        err = QueryDisplayConfig(flags, ref pathCount, paths, ref modeCount, modes, IntPtr.Zero);
        if (err != 0)
        {
            return false;
        }

        if (pathCount != paths.Length)
        {
            Array.Resize(ref paths, (int)pathCount);
        }

        if (modeCount != modes.Length)
        {
            Array.Resize(ref modes, (int)modeCount);
        }

        return true;
    }

    [DllImport("user32.dll")]
    private static extern int GetDisplayConfigBufferSizes(
        uint flags,
        out uint numPathArrayElements,
        out uint numModeInfoArrayElements);

    [DllImport("user32.dll")]
    private static extern int QueryDisplayConfig(
        uint flags,
        ref uint numPathArrayElements,
        [Out] DISPLAYCONFIG_PATH_INFO[] pathArray,
        ref uint numModeInfoArrayElements,
        [Out] DISPLAYCONFIG_MODE_INFO[] modeInfoArray,
        IntPtr currentTopologyId);

    [DllImport("user32.dll")]
    private static extern int SetDisplayConfig(
        uint numPathArrayElements,
        [In] DISPLAYCONFIG_PATH_INFO[] pathArray,
        uint numModeInfoArrayElements,
        [In] DISPLAYCONFIG_MODE_INFO[] modeInfoArray,
        uint flags);

    [StructLayout(LayoutKind.Sequential)]
    private struct LUID
    {
        public uint LowPart;
        public int HighPart;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DISPLAYCONFIG_RATIONAL
    {
        public uint Numerator;
        public uint Denominator;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DISPLAYCONFIG_PATH_SOURCE_INFO
    {
        public LUID adapterId;
        public uint id;
        public uint modeInfoIdx;
        public uint statusFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DISPLAYCONFIG_PATH_TARGET_INFO
    {
        public LUID adapterId;
        public uint id;
        public uint modeInfoIdx;
        public uint outputTechnology;
        public uint rotation;
        public uint scaling;
        public DISPLAYCONFIG_RATIONAL refreshRate;
        public uint scanLineOrdering;
        public int targetAvailable;
        public uint statusFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DISPLAYCONFIG_PATH_INFO
    {
        public DISPLAYCONFIG_PATH_SOURCE_INFO sourceInfo;
        public DISPLAYCONFIG_PATH_TARGET_INFO targetInfo;
        public uint flags;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DISPLAYCONFIG_MODE_INFO
    {
        public uint infoType;
        public uint id;
        public LUID adapterId;
        public ulong pixelRate;
        public uint hSyncNumerator;
        public uint hSyncDenominator;
        public uint vSyncNumerator;
        public uint vSyncDenominator;
        public uint activeWidth;
        public uint activeHeight;
        public uint totalWidth;
        public uint totalHeight;
        public uint videoStandard;
        public uint scanLineOrdering;
    }
}
