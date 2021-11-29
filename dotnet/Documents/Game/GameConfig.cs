
using System.Collections.Generic;

namespace TeamHitori.Mulplay.Container.Web.Documents.Game
{
    public record GameConfig(
        bool fillScreen = false,
        double screenRatio = 16 / 9,
        IEnumerable<string> codeFileNames = null
        );
}
