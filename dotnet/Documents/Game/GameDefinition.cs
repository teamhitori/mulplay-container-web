
using System.Threading.Tasks;
using TeamHitori.Mulplay.Container.Web.Components;
using TeamHitori.Mulplay.shared.storage;

namespace TeamHitori.Mulplay.Container.Web.Documents.Game
{
    public record GameDefinition(
        string gameName,
        double? intervalMs = 100,
        string frontEndLogic = LogicDefaults.FrontEndLogic,
        string startLogic = LogicDefaults.StartLogic,
        string userEnterLogic = LogicDefaults.UserEnterLogic,
        string userExitLogic = LogicDefaults.UserExitLogic,
        string userEventLogic = LogicDefaults.UserEventLogic,
        string gameLoopLogic = LogicDefaults.GameLoopLogic
        );

    public static class GameDefinitionExtensions
    {
        public async static Task<GameDefinition> GetLatestGameDefinition(this Storage storage, string gameName)
        {
            var gameConfigDoc = await storage.FindDocumentByPrimaryName<GameConfig>(gameName);
            var gameConfig = gameConfigDoc.GetObject().DoIfNull(() => {
                var config = new GameConfig(gameName, 100);
                storage.Upsert(config, primaryNameIN: gameName);
                return config;
            }); 

            var frontEndLogicDoc = await storage.FindDocumentByPrimaryName<GameLogic>($"{gameName}:{LogicType.FrontEndLogic}");
            var frontEndLogic = frontEndLogicDoc.GetObject().DoIfNull(() =>
            {
                var logic = new GameLogic(gameName, LogicType.FrontEndLogic, LogicDefaults.FrontEndLogic);
                storage.Upsert(logic, primaryNameIN: $"{gameName}:{LogicType.FrontEndLogic}");
                return logic;
            });

            var startLogicDoc = await storage.FindDocumentByPrimaryName<GameLogic>($"{gameName}:{LogicType.StartLogic}");
            var startLogic = startLogicDoc.GetObject().DoIfNull(() =>
            {
                var logic = new GameLogic(gameName, LogicType.StartLogic, LogicDefaults.StartLogic);
                storage.Upsert(logic, primaryNameIN: $"{gameName}:{LogicType.StartLogic}");
                return logic;
            });

            var gameLoopLogicDoc = await storage.FindDocumentByPrimaryName<GameLogic>($"{gameName}:{LogicType.GameLoopLogic}");
            var gameLoopLogic = gameLoopLogicDoc.GetObject().DoIfNull(() =>
            {
                var logic = new GameLogic(gameName, LogicType.GameLoopLogic, LogicDefaults.GameLoopLogic);
                storage.Upsert(logic, primaryNameIN: $"{gameName}:{LogicType.GameLoopLogic}");
                return logic;
            });

            var userEnterLogicDoc = await storage.FindDocumentByPrimaryName<GameLogic>($"{gameName}:{LogicType.UserEnterLogic}");
            var userEnterLogic = userEnterLogicDoc.GetObject().DoIfNull(() =>
            {
                var logic = new GameLogic(gameName, LogicType.UserEnterLogic, LogicDefaults.UserEnterLogic);
                storage.Upsert(logic, primaryNameIN: $"{gameName}:{LogicType.UserEnterLogic}");
                return logic;
            });

            var userExitLogicDoc = await storage.FindDocumentByPrimaryName<GameLogic>($"{gameName}:{LogicType.UserExitLogic}");
            var userExitLogic = userExitLogicDoc.GetObject().DoIfNull(() =>
            {
                var logic = new GameLogic(gameName, LogicType.UserExitLogic, LogicDefaults.UserExitLogic);
                storage.Upsert(logic, primaryNameIN: $"{gameName}:{LogicType.UserExitLogic}");
                return logic;
            });

            var userEventLogicDoc = await storage.FindDocumentByPrimaryName<GameLogic>($"{gameName}:{LogicType.UserEventLogic}");
            var userEventLogic = userEventLogicDoc.GetObject().DoIfNull(() =>
            {
                var logic = new GameLogic(gameName, LogicType.UserEventLogic, LogicDefaults.UserEventLogic);
                storage.Upsert(logic, primaryNameIN: $"{gameName}:{LogicType.UserEventLogic}");
                return logic;
            });

            return new GameDefinition(
                gameName,
                gameConfig?.intervalMs,
                frontEndLogic?.code, 
                startLogic?.code,
                userEnterLogic?.code,
                userExitLogic?.code,
                userEventLogic?.code,
                gameLoopLogic?.code
                );
        }
    }
}
