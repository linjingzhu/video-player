using VideoPlayer.Core.Library;
using VideoPlayer.Core.Playback;
using VideoPlayer.Core.Safety;

namespace VideoPlayer.Tests;

public class CrashResilienceTests
{
    [Fact]
    public void Crash_log_entry_names_the_source_and_exception()
    {
        var when = DateTimeOffset.Parse("2026-03-01T12:34:56.789+09:00");
        var entry = CrashLog.FormatEntry(when, "Dispatcher", new InvalidOperationException("boom"), terminating: false);

        Assert.Contains("2026-03-01 12:34:56.789+09:00 [Dispatcher]", entry, StringComparison.Ordinal);
        Assert.Contains("System.InvalidOperationException", entry, StringComparison.Ordinal);
        Assert.Contains("boom", entry, StringComparison.Ordinal);
        Assert.Contains("InvalidOperationException", entry, StringComparison.Ordinal);

        var fatal = CrashLog.FormatEntry(when, "AppDomain", null, terminating: true);
        Assert.Contains("terminating", fatal, StringComparison.Ordinal);
        Assert.Contains("(no exception)", fatal, StringComparison.Ordinal);
    }

    [Fact]
    public void Crash_log_appends_under_the_given_directory_and_survives_a_blocked_path()
    {
        using var workspace = new TempWorkspace();
        var entry = CrashLog.FormatEntry(DateTimeOffset.Parse("2026-03-01T00:00:00Z"), "Task", new IOException("disk"));
        Assert.True(CrashLog.TryAppend(workspace.Data, entry));

        var log = File.ReadAllText(Path.Combine(workspace.Data, CrashLog.FileName));
        Assert.Contains("disk", log, StringComparison.Ordinal);
        Assert.Contains("[Task]", log, StringComparison.Ordinal);
        Assert.Equal("예기치 않은 오류", CrashLog.BannerText);
        Assert.Equal("VideoPlayer", CrashLog.FolderName);

        var blocked = workspace.File("not-a-folder", [1]);
        Assert.False(CrashLog.TryAppend(Path.Combine(blocked, "logs"), entry));
    }

    [Fact]
    public void Volume_debounce_waits_out_a_wheel_burst_then_saves_the_last_level()
    {
        Assert.InRange(VolumePersist.Delay, TimeSpan.FromMilliseconds(250), TimeSpan.FromSeconds(2));

        using var workspace = new TempWorkspace();
        var session = new PlaybackSession(new FakeMediaEngine(), workspace.Data);
        session.SetVolume(0.40);
        session.SetSpeed(1.75);
        session.NudgeVolumeFromWheel(0.05);
        session.NudgeVolumeFromWheel(0.05);

        var settingsPath = Path.Combine(workspace.Data, AppSettings.FileName);
        Assert.False(File.Exists(settingsPath));
        session.Tick(DateTimeOffset.UtcNow - VolumePersist.Delay);
        Assert.False(File.Exists(settingsPath));

        session.Tick(DateTimeOffset.UtcNow + VolumePersist.Delay + TimeSpan.FromMilliseconds(50));
        Assert.True(File.Exists(settingsPath));
        var json = File.ReadAllText(settingsPath);
        Assert.DoesNotContain("speed", json, StringComparison.OrdinalIgnoreCase);
        var loaded = AppSettings.FromJson(json);
        Assert.Equal(0.50, loaded.Volume, 3);
        Assert.False(loaded.Muted);

        var reopened = new PlaybackSession(new FakeMediaEngine(), workspace.Data);
        Assert.Equal(0.50, reopened.Engine.Volume, 3);
        Assert.Equal(1.0, reopened.Speed);
        Assert.Equal(1.0, reopened.Engine.Speed);
    }

    [Fact]
    public void Muted_volume_is_saved_on_the_same_debounce_and_speed_stays_one()
    {
        using var workspace = new TempWorkspace();
        var session = new PlaybackSession(new FakeMediaEngine(), workspace.Data);
        session.SetVolume(0.70);
        session.ToggleMute();
        session.SetSpeed(2.0);
        session.Tick(DateTimeOffset.UtcNow + TimeSpan.FromSeconds(1));

        var reopened = new PlaybackSession(new FakeMediaEngine(), workspace.Data);
        Assert.True(reopened.Shell.Volume.Muted);
        Assert.Equal(0, reopened.Engine.Volume);
        Assert.Equal(0.70, reopened.Settings.Volume, 3);
        Assert.Equal(1.0, reopened.Speed);
    }

    [Fact]
    public void Persist_io_failure_does_not_throw()
    {
        using var workspace = new TempWorkspace();
        var blocker = workspace.File("blocked", [1]);
        var session = new PlaybackSession(new FakeMediaEngine(), Path.Combine(blocker, "data"));
        session.SetVolume(0.25);
        session.SetSpeed(1.5);
        session.RememberVolume();
        session.Tick(DateTimeOffset.UtcNow + TimeSpan.FromSeconds(1));

        Assert.Equal(0.25, session.Engine.Volume, 3);
        Assert.Equal(1.5, session.Speed);

        var tooLong = new string('a', 5000) + ".json";
        Assert.False(SessionFiles.TryWrite(workspace.Data, tooLong, "{}"));
        _ = SessionFiles.TryRead(Path.Combine(workspace.Data, new string('b', 5000)));
    }

    [Fact]
    public void Unreadable_resume_is_not_replaced_with_an_empty_store()
    {
        using var workspace = new TempWorkspace();
        var resumePath = Path.Combine(workspace.Data, "resume.json");
        File.WriteAllText(resumePath, "KEEP-ME");
        var missing = SessionFiles.TryRead(Path.Combine(workspace.Data, "absent.json"));
        Assert.True(missing.Readable);
        Assert.Null(missing.Text);

        PlaybackSession session;
        using (var hold = new FileStream(resumePath, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            var blocked = SessionFiles.TryRead(resumePath);
            Assert.False(blocked.Readable);
            session = new PlaybackSession(new FakeMediaEngine(), workspace.Data);
        }

        session.SetVolume(0.33);
        session.RememberVolume();
        Assert.Equal("KEEP-ME", File.ReadAllText(resumePath));
        Assert.Equal(1.0, session.Speed);
    }
}
