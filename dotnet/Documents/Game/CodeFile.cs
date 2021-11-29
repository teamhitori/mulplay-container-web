using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TeamHitori.Mulplay.Container.Web.Documents.v2.Game
{
    public record CodeFile(
        string gameName,
        string fileName,
        string code
        );
}
