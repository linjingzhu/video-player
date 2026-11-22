using VideoPlayer.Core.Shell;

namespace VideoPlayer.Tests;

public class MenuOrderTests
{
    [Fact]
    public void Shared_menu_puts_quick_items_first_and_view_items_last()
    {
        var arranged = MenuOrder.Arrange(
        [
            "시리즈",
            "capture",
            "열기...",
            "clip",
            "URL 열기",
            "hdrAuto",
            "hdrOff",
            "speed",
            "skipBack",
            "skipForward",
            "playTo",
            "loopRange",
            "점프 초",
            "fit",
            "chapters",
            "viewSeparator",
            "recent",
            "폴더 열기"
        ]);

        Assert.Equal(
            [
                "열기...",
                "URL 열기",
                "recent",
                "speed",
                "skipBack",
                "skipForward",
                "playTo",
                "시리즈",
                "폴더 열기",
                "loopRange",
                "chapters",
                "fit",
                "viewSeparator",
                "clip",
                "점프 초",
                "capture",
                "hdrAuto",
                "hdrOff"
            ],
            arranged);
    }
}
