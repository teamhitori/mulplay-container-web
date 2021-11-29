using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TeamHitori.Mulplay.Container.Web.Components;
using TeamHitori.Mulplay.shared.storage;

namespace TeamHitori.Mulplay.Container.Web.Documents.Game
{
    public record PublishedDefinition
    (
        Boolean isPublished,
        string publishedPath,
        Boolean isStarted,
        int activePlayerCount,
        string version
    );

    public static class PublishedDefinitionExtensions
    {

        public static PublishedDefinition GetLatest(GameDefinition gameDefinition, GameContainer gameContainer, string userName)
        {

            var activeInstances = gameContainer.ActiveGameInstances.Where(inst => inst.gameName.StartsWith($"{userName}:{gameDefinition.gameName}"));
            return new PublishedDefinition(
                gameDefinition.isPublished,
                $"/{userName}/{gameDefinition.gameName}",
                activeInstances.Any(),
                0, //activeInstances.Aggregate(0, (count, inst) => gameContainer.GetActiveConnectionCount(inst.gamePrimaryName) + count),
                gameDefinition.version
                );
        }
    }
}
