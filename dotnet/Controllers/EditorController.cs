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

namespace TeamHitori.Mulplay.Container.Web.Controllers
{
    [Authorize]
    public class EditorController : Controller
    {

        private readonly ILogger<EditorApiController> _logger;
        private readonly GameContainer _gameHub;
        private readonly IStorageConfig _storageConfig;
        private GameService.GameServiceClient _grpcClient;

        public EditorController(
            ILogger<EditorApiController> logger,
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


        public IActionResult Index()
        {
            return View();
        }

        [HttpGet("editor/{gameName}")]
        public IActionResult Index(string gameName)
        {
            return View();
        }

    }
}
