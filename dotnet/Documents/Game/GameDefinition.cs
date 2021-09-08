
using System.Threading.Tasks;
using TeamHitori.Mulplay.Container.Web.Components;
using TeamHitori.Mulplay.shared.storage;

namespace TeamHitori.Mulplay.Container.Web.Documents.Game
{
    public record GameDefinition(
        string gameName,
        string frontendLogic,
        string backendLogic,
        GameConfig gameConfig,
        bool isPublished,
        string publishedPath
        );

    public static class GameDefinitionExtensions
    {
        public async static Task<GameDefinition> GetLatest(Storage storage, Storage storagePublish, string gameName)
        {
            var gameConfigDoc = await storage.FindDocumentByPrimaryName<GameConfig>(gameName);
            var gameConfig = gameConfigDoc.GetObject().DoIfNull(() => {
                var config = new GameConfig();
                storage.Upsert(config, primaryNameIN: gameName);
                return config;
            }); 

            var frontEndLogicDoc = await storage.FindDocumentByPrimaryName<GameLogic>($"{gameName}:{LogicType.FrontendLogic}");
            var frontEndLogic = frontEndLogicDoc.GetObject().DoIfNull(() =>
            {
                var logic = new GameLogic(gameName, LogicType.FrontendLogic, LogicDefaults.FrontendLogic);
                storage.Upsert(logic, primaryNameIN: $"{gameName}:{LogicType.FrontendLogic}");
                return logic;
            });

            var backendLogicDoc = await storage.FindDocumentByPrimaryName<GameLogic>($"{gameName}:{LogicType.BackendLogic}");
            var backendLogic = backendLogicDoc.GetObject().DoIfNull(() =>
            {
                var logic = new GameLogic(gameName, LogicType.BackendLogic, LogicDefaults.BackendLogic);
                storage.Upsert(logic, primaryNameIN: $"{gameName}:{LogicType.BackendLogic}");
                return logic;
            });

            var publishProfileDoc = await storagePublish.GetSingleton<PublishProfile>();
            var publishProfile = publishProfileDoc?.GetObject();
            var publishedGameUrl = publishProfile == null ? string.Empty : $"/{publishProfile.author}/{gameName}";

            return new GameDefinition(
                gameName,
                frontEndLogic?.code,
                backendLogic?.code,
                gameConfig,
                publishProfileDoc != null,
                publishedGameUrl
                );
        }
    }
}
