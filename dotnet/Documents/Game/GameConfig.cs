
namespace TeamHitori.Mulplay.Container.Web.Documents.Game
{
    public record GameConfig(
        double? intervalMs = 100,
        bool fillScreen = false,
        double screenRatio = 16 / 9
        );
}
