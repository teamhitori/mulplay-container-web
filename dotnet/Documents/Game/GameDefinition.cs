
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TeamHitori.Mulplay.Container.Web.Components;
using TeamHitori.Mulplay.Container.Web.Documents.v2.Game;
using TeamHitori.Mulplay.shared.storage;

namespace TeamHitori.Mulplay.Container.Web.Documents.Game
{
    public record GameDefinition(
        string gameName,
        IEnumerable<CodeFile> codeFiles,
        GameConfig gameConfig,
        bool isPublished,
        string publishedPath,
        string version = "",
        bool debugEnabled = false
        );


    public static class GameDefinitionExtensions
    {
        public async static Task<CompiledCode> GetFECode(this Storage storage, string gameName)
        {
            var frontEndCodeDoc = await storage.FindDocumentByPrimaryName<CompiledCode>($"{gameName}:{CodeType.FrontendLogic}");
            var frontEndCode = frontEndCodeDoc.GetObject().DoIfNull(() =>
            {
                var code = new CompiledCode(gameName, CodeType.FrontendLogic, "");
                storage.Upsert(code, primaryNameIN: $"{gameName}:{CodeType.FrontendLogic}");
                return code;
            });
            return frontEndCode;
        }

        public async static Task<CompiledCode> GetBECode(this Storage storage, string gameName)
        {
            var backendCodeDoc = await storage.FindDocumentByPrimaryName<CompiledCode>($"{gameName}:{CodeType.BackendLogic}");
            var backendCode = backendCodeDoc.GetObject().DoIfNull(() =>
            {
                var code = new CompiledCode(gameName, CodeType.BackendLogic, "");
                storage.Upsert(code, primaryNameIN: $"{gameName}:{CodeType.BackendLogic}");
                return code;
            });

            return backendCode;
        }

        public async static Task<GameDefinition> GetLatest(Storage storage, Storage storagePublish, string gameName)
        {
            var gameConfigDoc = await storage.FindDocumentByPrimaryName<GameConfig>(gameName);
            var gameConfig = gameConfigDoc.GetObject().DoIfNull(() =>
            {
                var config = new GameConfig();
                storage.Upsert(config, primaryNameIN: gameName);
                return config;
            });

            var codeFiles = gameConfig.codeFileNames?.Select(async fileName =>
            {
                var codeDoc = await storage.FindDocumentByPrimaryName<CodeFile>($"{gameName}:{fileName}");
                return codeDoc.GetObject().DoIfNull(() =>
                {
                    var logic = new CodeFile(gameName, fileName, "");
                    storage.Upsert(logic, primaryNameIN: $"{gameName}:{CodeType.FrontendLogic}");
                    return logic;
                });
            }).Select(t => t.Result).ToList();

            codeFiles ??= new List<CodeFile>();

            var publishProfileDoc = await storagePublish.GetSingleton<PublishProfile>();
            var publishProfile = publishProfileDoc?.GetObject();
            var publishedGameUrl = publishProfile == null ? string.Empty : $"/{publishProfile.author}/{gameName}";

            return new GameDefinition(
                gameName,
                codeFiles,
                gameConfig,
                publishProfileDoc != null,
                publishedGameUrl,
                publishProfile?.version,
                publishProfile?.debugEnabled ?? false
                );
        }
    }
}
