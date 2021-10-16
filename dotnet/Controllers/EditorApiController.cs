

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using TeamHitori.Mulplay.shared.storage;
using TeamHitori.Mulplay.Shared.Poco;
using TeamHitori.Mulplay.Container.Web.Documents.Game;
using TeamHitori.Mulplay.Container.Web.Components;
using TeamHitori.Mulplay.Container.Web.Components.Interfaces;
using Microsoft.AspNetCore.Authorization;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace TeamHitori.Mulplay.Container.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class EditorApiController : ControllerBase
    {
        private readonly ILogger<EditorApiController> _logger;
        private readonly GameContainer _gameHub;
        private readonly IStorageConfig _storageConfig;
        private GameContainer _gameContainer;

        public EditorApiController(
            ILogger<EditorApiController> logger,
            GameContainer gameHub,
            IStorageConfig storageConfig,
            GameContainer gameContainer
            )
        {
            _logger = logger;
            _gameHub = gameHub;
            this._storageConfig = storageConfig;
            _gameContainer = gameContainer;
        }

        [HttpGet("create-game/{gameName}")]
        public async Task<GameInstance> createGame(string gameName)
        {
            try
            {
                // To lower
                var userName = User.Identity.Name.ToLower();
                gameName = gameName.ToLower();

                var storage = _storageConfig.ToUserStorage(HttpContext);
                var storagePublish = _storageConfig.ToUserStorage($"{userName}:{gameName}");

                storage.LogDebug("Game Start Called");

                var gameDefinition = await GameDefinitionExtensions.GetLatest(storage, storagePublish, gameName);

                var gamePrimaryName = Guid.NewGuid().ToString();

                var gameInstance = new GameInstance($"debug:{userName}:{gameName}", gamePrimaryName, DateTime.Now.ToString("yyyy/MM/dd HH:mm:ssZ"), false, true);

                await _gameContainer.CreateGame(gameInstance, gameDefinition);

                await _gameContainer.NotifyReload(gameInstance.gameName);

                var res = gamePrimaryName.ToJDoc().content;

                return gameInstance;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return null;
            }
        }

        [HttpPost("upsert-config/{gameName}")]
        public async Task<bool> UpsertConfig(string gameName, [FromBody] GameConfig gameConfig)
        {
            var storage = _storageConfig.ToUserStorage(HttpContext);

            storage.LogDebug($"Upsert Config Called");

            try
            {
                // primaryNameIn - case insensitive
                await storage.Upsert(gameConfig, primaryNameIN: gameName);


                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

            return false;
        }

        [HttpPost("upsert-logic")]
        public async Task<bool> UpsertLogic([FromBody] GameLogic gameLogic)
        {
            var storage = _storageConfig.ToUserStorage(HttpContext);

            storage.LogDebug($"Upsert Logic {gameLogic} Called");

            try
            {
                // primaryNameIn - case insensitive
                await storage.Upsert(gameLogic, primaryNameIN: $"{gameLogic.gameName}:{gameLogic.logicType}");

                await _gameContainer.NotifyReload(gameLogic.gameName);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

            return false;
        }

        [HttpGet("get-active/{gameName}")]
        public IEnumerable<GameInstance> getActive(string gameName)
        {
            var userName = User.Identity.Name.ToLower();
            gameName = gameName.ToLower();

            var gameInstances = _gameContainer.ActiveGameInstances;

            foreach (var item in gameInstances)
            {
                if (item.gameName == $"debug:{userName}:{gameName}")
                {
                    yield return item;
                }
            }
        }

        [HttpGet("get-all")]
        public async Task<IEnumerable<string>> getAll()
        {
            var storage = _storageConfig.ToUserStorage(HttpContext);

            storage.LogDebug("Game Get All Called");

            var gameDefs = await storage.FindAllByType<GameConfig>();

            return gameDefs.Select(x => x.primaryName);
        }

        [HttpGet("get-definition/{gameName}")]
        public async Task<GameDefinition> getDefinition(string gameName)
        {
            // To lower
            var userName = User.Identity.Name.ToLower();
            gameName = gameName.ToLower();

            var storage = _storageConfig.ToUserStorage(HttpContext);
            var storagePublish = _storageConfig.ToUserStorage($"{userName}:{gameName}");

            storage.LogDebug("Game Get Called");

            var gameDefinition = await GameDefinitionExtensions.GetLatest(storage, storagePublish, gameName);

            return gameDefinition;
        }

        [HttpGet("get-publish-definition/{gameName}")]
        public async Task<PublishedDefinition> getPublishDefinition(string gameName)
        {
            // To lower
            var userName = User.Identity.Name.ToLower();
            gameName = gameName.ToLower();

            var storage = _storageConfig.ToUserStorage(HttpContext);
            var storagePublish = _storageConfig.ToUserStorage($"{userName}:{gameName}");

            storage.LogDebug("Game Get Called");

            var gameDefinition = await GameDefinitionExtensions.GetLatest(storage, storagePublish, gameName);

            var publishedDefinition = PublishedDefinitionExtensions.GetLatest(gameDefinition, _gameContainer, userName);

            return publishedDefinition;
        }

        [HttpGet("get-logic/{gameName}/{logicType}")]
        public async Task<GameLogic> getLogic(string gameName, LogicType logicType)
        {
            var storage = _storageConfig.ToUserStorage(HttpContext);

            storage.LogDebug("Game Get Called");

            var gameLogicDoc = await storage.FindDocumentByPrimaryName<GameLogic>($"{gameName}:{logicType}");
            var gameLogic = gameLogicDoc?.GetObject();

            return gameLogic;
        }

        [HttpPost("get-instance-pn/{gameName}")]
        public string GetPublishedInstancePrimaryName(string gameName)
        {
            // TO LOWER
            var userName = User.Identity.Name.ToLower();
            gameName = gameName.ToLower();

            // VERSIONING
            var storagePublish = _storageConfig.ToUserStorage($"{userName}:{gameName}");
            var publishProfile = storagePublish.GetSingleton<PublishProfile>()?.Result.GetObject();

            if (publishProfile == null)
            {
                return null;
            }

            var gameInstances = _gameContainer.ActiveGameInstances;
            var activeInstance = gameInstances.FirstOrDefault(i => i.gameName == $"{userName}:{gameName}:{publishProfile.version}");

            return activeInstance == null ? string.Empty.ToJDoc().content : activeInstance.gamePrimaryName.ToJDoc().content;

        }

        [HttpGet("get-config/{gameName}")]
        public async Task<GameConfig> getConfig(string gameName)
        {
            var storage = _storageConfig.ToUserStorage(HttpContext);

            storage.LogDebug("Game Get Called");

            var gameConfigDoc = await storage.FindDocumentByPrimaryName<GameConfig>(gameName);
            var gameConfig = gameConfigDoc?.GetObject();

            return gameConfig;
        }

        [HttpGet("publish/{gameName}")]
        public async Task<bool> Publish(string gameName)
        {
            // To lower
            var userName = User.Identity.Name.ToLower();
            gameName = gameName.ToLower();

            var storage = _storageConfig.ToUserStorage(HttpContext);
            var storagePublish = _storageConfig.ToUserStorage($"{userName}:{gameName}");

            storage.LogDebug("Game Get Called");

            var gameDefinition = await GameDefinitionExtensions.GetLatest(storage, storagePublish, gameName);
            var publishProfile = new PublishProfile(gameDefinition, userName, "0.0.0.0", DateTime.Now, false);

            await storagePublish.Upsert(publishProfile, true);

            var gameInstances = _gameContainer.ActiveGameInstances;
            var activeInstance = gameInstances.FirstOrDefault(i => i.gameName.StartsWith($"{userName}:{gameName}:{publishProfile.version}"));

            if (activeInstance != null && activeInstance.gameName != $"{userName}:{gameName}:{publishProfile.version}")
            {
                await _gameContainer.DestroyGame(activeInstance.gamePrimaryName);
                activeInstance = null;
            }

            if (activeInstance == null)
            {
                var gamePrimaryName = Guid.NewGuid().ToString();

                activeInstance = new GameInstance($"{userName}:{gameName}:{publishProfile.version}", gamePrimaryName, DateTime.Now.ToString("yyyy/MM/dd HH:mm:ssZ"), false, false);

                await _gameContainer.CreateGame(activeInstance, publishProfile.gameDefinition);

                await _gameContainer.EnableDebug($"{userName}:{gameName}", publishProfile.debugEnabled);
            }

            return true;
        }

        [HttpGet("un-publish/{gameName}")]
        public async Task<bool> UnPublish(string gameName)
        {
            // To lower
            var userName = User.Identity.Name.ToLower();
            gameName = gameName.ToLower();

            var storage = _storageConfig.ToUserStorage(HttpContext);
            var storagePublish = _storageConfig.ToUserStorage($"{userName}:{gameName}");

            storage.LogDebug("Game Get Called");

            var gameDefinition = await GameDefinitionExtensions.GetLatest(storage, storagePublish, gameName);

            var publishProfile = new PublishProfile(gameDefinition, userName, "0.0.0.0", DateTime.Now, false);

            var doc = storagePublish.CreateSingleton(publishProfile);

            await storagePublish.DeleteDocument(doc);

            return true;
        }

        [HttpGet("enable-debug/{gameName}/{enable}")]
        public async Task EnableDebug(string gameName, Boolean enable)
        {
            // To lower
            var userName = User.Identity.Name.ToLower();
            gameName = gameName.ToLower();

            var storage = _storageConfig.ToUserStorage(HttpContext);
            var storagePublish = _storageConfig.ToUserStorage($"{userName}:{gameName}");

            var publishProfile = storagePublish.GetSingleton<PublishProfile>()?.Result.GetObject();

            if (publishProfile == null)
            {
                return;
            }

            await storagePublish.Upsert(publishProfile with { debugEnabled = enable }, true);

           await  _gameContainer.EnableDebug($"{userName}:{gameName}", enable);
        }

        [HttpGet("game-action/{gameName}/{start}/{stop}")]
        public async Task GameAction(string gameName, bool start, bool stop)
        {
            // To lower
            var userName = User.Identity.Name.ToLower();
            gameName = gameName.ToLower();

            var storage = _storageConfig.ToUserStorage(HttpContext);
            var storagePublish = _storageConfig.ToUserStorage($"{userName}:{gameName}");

            var publishProfile = storagePublish.GetSingleton<PublishProfile>()?.Result.GetObject();

            if (publishProfile == null)
            {
                return;
            }

            var gameInstances = _gameContainer.ActiveGameInstances;
            var activeInstances = gameInstances.Where(i => i.gameName.StartsWith($"{userName}:{gameName}"));

            if (stop && activeInstances.Any())
            {
                activeInstances.Foreach(async i =>
                {
                    await _gameContainer.DestroyGame(i.gamePrimaryName);
                });
            }

            if (start)
            {
                var gamePrimaryName = Guid.NewGuid().ToString();

                var gameDefinition = await GameDefinitionExtensions.GetLatest(storage, storagePublish, gameName);
                publishProfile = new PublishProfile(gameDefinition, userName, "0.0.0.0", DateTime.Now, false);

                await storagePublish.Upsert(publishProfile, true);

                var activeInstance = new GameInstance($"{userName}:{gameName}:{publishProfile.version}", gamePrimaryName, DateTime.Now.ToString("yyyy/MM/dd HH:mm:ssZ"), false, false);

                await _gameContainer.CreateGame(activeInstance, publishProfile.gameDefinition);

                await _gameContainer.EnableDebug($"{userName}:{gameName}", publishProfile.debugEnabled);
            }
        }
    }
}
