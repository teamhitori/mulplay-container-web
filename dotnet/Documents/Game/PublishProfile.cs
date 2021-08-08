using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TeamHitori.Mulplay.Container.Web.Documents.Game
{
    public record PublishProfile(
        string author,
        string version,
        DateTime publishDate,
        GameDefinition gameDefinition);
}
