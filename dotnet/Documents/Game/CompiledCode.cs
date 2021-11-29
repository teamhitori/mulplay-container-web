using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TeamHitori.Mulplay.Container.Web.Documents.Game
{
    public record CompiledCode(
        string gameName,
        CodeType codeType,
        string code
        );
}
