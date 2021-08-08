using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TeamHitori.Mulplay.Container.Web.Documents.Game
{
    public record GameLogic(
        string gameName,
        LogicType logicType,
        string code
        );
}
