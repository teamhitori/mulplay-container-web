using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TeamHitori.Mulplay.Container.Web.Documents.Game
{
    public record PublishProfile(
        GameDefinition gameDefinition,
        string author,
        string version,
        DateTime publishDate,
        bool debugEnabled);
}
