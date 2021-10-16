using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using TeamHitori.Mulplay.Container.Web.Components;
using TeamHitori.Mulplay.Container.Web.Components.Interfaces;
using TeamHitori.Mulplay.Container.Web.Models;
using TeamHitori.Mulplay.Shared.Poco;
using TeamHitori.Mulplay.shared.storage;
using TeamHitori.Mulplay.Container.Web.Documents.Game;
using Microsoft.AspNetCore.Authorization;
using System.Linq;

namespace TeamHitori.Mulplay.Container.Web.Controllers
{
    public class GameController : Controller
    {

        private readonly ILogger<EditorApiController> _logger;
        private readonly GameContainer _gameContainer;
        private readonly IStorageConfig _storageConfig;

        public GameController(
            ILogger<EditorApiController> logger,
            GameContainer gameContainer,
            IStorageConfig storageConfig
            )
        {
            _logger = logger;
            _gameContainer = gameContainer;
            this._storageConfig = storageConfig;
        }

        [HttpGet("{author}/{gameName}")]
        public IActionResult Index(string author, string gameName)
        {
            if (author.ToLower() == "editor")
            {
                if (User.Identity.IsAuthenticated)
                {
                    return RedirectToAction("Index", "Editor", new { gameName = gameName });
                } else
                {
                    return Redirect("/MicrosoftIdentity/Account/SignIn");
                } 
            }

            return View();
        }

        [HttpPost("{author}/{gameName}")]
        public async Task<PublishedGameInstance> GameInstance(string author, string gameName)
        {
            // TO LOWER
            author = author.ToLower();
            gameName = gameName.ToLower();

            // VERSIONING
            var storagePublish = _storageConfig.ToUserStorage($"{author}:{gameName}");
            var publishProfile = storagePublish.GetSingleton<PublishProfile>()?.Result.GetObject();

            if(publishProfile == null)
            {
                return null;
            }

            var gameInstances = _gameContainer.ActiveGameInstances;
            var activeInstance = gameInstances.FirstOrDefault(i => i.gameName == $"{author}:{gameName}:{publishProfile.version}");

            if(activeInstance == null)
            {
                //var gamePrimaryName = Guid.NewGuid().ToString();

                //activeInstance = new GameInstance($"{author}:{gameName}:{publishProfile.version}", gamePrimaryName, DateTime.Now.ToString("yyyy/MM/dd HH:mm:ssZ"), false, false);

                //await _gameContainer.CreateGame(activeInstance, publishProfile.gameDefinition);

                //_gameContainer.EnableDebug($"{author}:{gameName}", publishProfile.debugEnabled);

                return null;
            }

            return new PublishedGameInstance(publishProfile.gameDefinition.frontendLogic, publishProfile.gameDefinition.gameConfig, activeInstance);
        }
    }
}
