
using GrpcConsole;
using TeamHitori.Mulplay.Container.Web.Components;
using TeamHitori.Mulplay.Container.Web.Components.Interfaces;
using TeamHitori.Mulplay.Container.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using UA.loops.shared.storage;
using UA.loops.shared.storage.documents;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace TeamHitori.Mulplay.Container.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EditorController : ControllerBase
    {
        private readonly ILogger<EditorController> _logger;
        private readonly GameContainer _gameHub;
        private readonly IStorageConfig _storageConfig;
        private GameService.GameServiceClient _grpcClient;

        public EditorController(
            ILogger<EditorController> logger,
            GameContainer gameHub,
            IStorageConfig storageConfig,
            GameService.GameServiceClient grpcClient
            )
        {
            _logger = logger;
            _gameHub = gameHub;
            this._storageConfig = storageConfig;
            _grpcClient = grpcClient;
        }

        [HttpPost("upsert")]
        public async Task<bool> Upsert([FromBody] string gameConfigStr)
        {
            try
            {
                var gameConfig = JsonSerializer.Deserialize<GameConfig>(gameConfigStr);
                var storage = _storageConfig.ToUserStorage(HttpContext);

                storage.LogDebug("Upsert Called");

                await storage.Upsert(gameConfig, primaryNameIN: gameConfig.gameName);

                var grpcResponse = _grpcClient.createGame(
                    new Document { Content = JsonSerializer.Serialize(gameConfig) });

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

            return false;

        }

        [HttpGet]
        public async Task<IEnumerable<string>> getAll()
        {
            var storage = _storageConfig.ToUserStorage(HttpContext);

            storage.LogDebug("Game Get All Called");

            var gameConfigs = await storage.FindAllByType<GameConfig>();

            return gameConfigs.Select(x => x.GetObject().gameName);
        }

        [HttpGet("{gameName}")]
        public async Task<GameConfig> get(string gameName)
        {
            var storage = _storageConfig.ToUserStorage(HttpContext);

            storage.LogDebug("Game Get Called");

            var gameConfigDoc = await storage.FindDocumentByPrimaryName<GameConfig>(gameName);
            var gameConfig = gameConfigDoc?.GetObject() ??
                new GameConfig(
                    gameName,
                    2000);

            return gameConfig;
        }
    }
}
