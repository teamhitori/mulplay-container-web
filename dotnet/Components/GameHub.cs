using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using TeamHitori.Mulplay.Container.Web.Components.Interfaces;

namespace TeamHitori.Mulplay.Container.Web.Components
{
    public class GameHub : Hub<IGameClient>
    {
        private ILogger<GameHub> _logger;
        private readonly GameContainer _gameContainer;

        public GameHub(ILogger<GameHub> logger, GameContainer gameContainer)
        {
            _logger = logger;
            _gameContainer = gameContainer;
        }

        //[Authorize]
        //public async Task StartMetrics(string gamePrimaryName)
        //{
        //    _logger.LogInformation($"StartMetrics Called, {gamePrimaryName}");

        //    await _gameContainer.StartMetrics(Context.ConnectionId, gamePrimaryName);

        //}

        //public void Enter()
        //{
        //    _logger.LogInformation("User Enter");

        //    _gameContainer.PlayerEnter(Context.ConnectionId);
        //}

        //public void PlayerEvent(string content)
        //{
        //    try
        //    {
        //        _gameContainer.PlayerEvent(Context.ConnectionId, content);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, ex.Message);
        //    }
        //}

        //[Authorize]
        //public void MonitorActivePlayers(string gameName)
        //{
        //    var userName = Context.User.Identity.Name.ToLower();

        //    _gameContainer.MonitorActivePlayers(Context.ConnectionId, $"{userName}:{gameName}");
        //}

        [Authorize]
        public void MonitorInstance(string gamePrimaryName)
        {
            //var userName = Context.User.Identity.Name.ToLower();
            

            _gameContainer.MonitorInstance(Context.ConnectionId, gamePrimaryName);
        }


        [Authorize]
        public void MonitorGame(string gameName)
        {
            var userId = Context.User.Claims.FirstOrDefault(claim =>
                    claim.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier"
                )?.Value ??
                "1111-1111-1111-1111-1111";

            _gameContainer.MontorGame(Context.ConnectionId, $"debug:{userId}:{gameName}");
        }

        //[Authorize]
        //public void Step()
        //{
        //    _logger.LogInformation("Step Called");

        //    _gameContainer.Step(Context.ConnectionId);
        //}

        //[Authorize]
        //public async Task Stop(string gamePrimaryName)
        //{
        //    _logger.LogInformation($"Stop {gamePrimaryName}");

        //    await _gameContainer.DestroyGame(gamePrimaryName);
        //}

        //public async override Task OnDisconnectedAsync(Exception exception)
        //{
        //    _logger.LogInformation($"${Context.ConnectionId} disconnected");

        //    await _gameContainer.PlayerDisconnect(Context.ConnectionId);

        //    await base.OnDisconnectedAsync(exception);
        //}

    }
}
