using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
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

        public void Start(string gamePrimaryName)
        {
            _logger.LogInformation("Start Called");

            _gameContainer.Start(Context.ConnectionId, gamePrimaryName);

        }

        public void Enter()
        {
            _logger.LogInformation("User Enter");

            _gameContainer.PlayerEnter(Context.ConnectionId);
        }


        [Authorize]
        public void Monitor(string gameName)
        {
            var userName = Context.User.Identity.Name.ToLower();

            _gameContainer.Monitor(Context.ConnectionId, $"debug:{userName}:{gameName}");
        }

        [Authorize]
        public void Step()
        {
            _logger.LogInformation("Step Called");

            _gameContainer.Step(Context.ConnectionId);
        }

        [Authorize]
        public void Stop()
        {
            _logger.LogInformation("Stop Called");

            _gameContainer.Destroy(Context.ConnectionId);
        }


        public void PlayerEvent(string content)
        {
            try
            {
                _gameContainer.PlayerEvent(Context.ConnectionId, content);
            } catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            _logger.LogInformation($"${Context.ConnectionId} disconnected");

            _gameContainer.PlayerDisconnect(Context.ConnectionId).Wait();

            return base.OnDisconnectedAsync(exception);
        }

    }
}
