using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TeamHitori.Mulplay.Container.Web.Documents.Game
{
    public record GameInstance
    (
        string gameName,
        string gamePrimaryName,
        string createTime,
        bool isStarted = false,
        bool isDebug = false,
        bool isMetricsActive = false
    );
}
