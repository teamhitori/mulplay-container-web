using GrpcConsole;
using TeamHitori.Mulplay.Container.Web.Components;
using TeamHitori.Mulplay.Container.Web.Components.Interfaces;
using TeamHitori.Mulplay.Container.Web.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using UA.loops.shared.storage;

namespace TeamHitori.Mulplay.Container.Web.Controllers
{
    

    [Route("api/[controller]")]
    [ApiController]
    public class GameController : ControllerBase
    {

        private readonly ILogger<EditorController> _logger;
        private readonly GameContainer _gameHub;
        private readonly IStorageConfig _storageConfig;
        private GameService.GameServiceClient _grpcClient;

        public GameController(
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

        [HttpGet("{gameName}")]
        public async Task<Boolean> Start(string gameName)
       {
            try
            {
                var storage = _storageConfig.ToUserStorage(HttpContext);

                storage.LogDebug("Upsert Called");

                var gameConfigDoc = await storage.FindDocumentByPrimaryName<GameConfig>(gameName);
                var gameConfig = gameConfigDoc?.GetObject();

                var grpcResponse = _grpcClient.createGame(
                    new Document { Content = JsonSerializer.Serialize(gameConfig) });

                return true;
            } catch(Exception ex)
            {
                _logger.LogError(ex.Message);
            }

            return false;
        
        }
    }
}
