
using System.Threading.Tasks;
using TeamHitori.Mulplay.Container.Web.Components;
using TeamHitori.Mulplay.shared.storage;

namespace TeamHitori.Mulplay.Container.Web.Documents.Game
{
    public record PublishedGameInstance(
        string frontEndLogic,
        GameConfig GameConfig,
        GameInstance gameInstance
        );
}
