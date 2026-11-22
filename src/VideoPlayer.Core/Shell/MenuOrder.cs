namespace VideoPlayer.Core.Shell;

/// <summary>
/// Shared quick-menu / view / hamburger / right-click order.
/// Unknown items stay between the quick block and the view block, in their original order.
/// </summary>
public static class MenuOrder
{
    public static IReadOnlyList<string> Arrange(IReadOnlyList<string> keys)
    {
        return keys
            .Select((key, index) => (key, index, rank: Rank(key, index)))
            .OrderBy(row => row.rank)
            .ThenBy(row => row.index)
            .Select(row => row.key)
            .ToList();
    }

    public static int Rank(string key, int index)
    {
        var group = key switch
        {
            "열기..." => 10,
            "URL 열기" => 20,
            "recent" => 30,
            "speed" => 40,
            "skipBack" or "skipForward" => 50,
            "playTo" => 60,
            "loopRange" => 200,
            "chapters" => 210,
            "fit" => 220,
            "viewSeparator" => 230,
            "clip" => 240,
            "점프 초" => 250,
            "capture" => 260,
            "hdrAuto" or "hdrOff" => 270,
            _ => 100
        };
        return (group * 1000) + index;
    }
}
