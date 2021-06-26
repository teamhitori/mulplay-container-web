using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TeamHitori.Mulplay.Container.Web.Components.Interfaces;
using GrpcConsole;
using Grpc.Net.Client;
using System.Threading;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using System.Reactive.Linq;

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

        public void Start()
        {
            _logger.LogInformation("Start Called");

            _gameContainer.Start(Context.ConnectionId);

            _gameContainer.QueueNewUserEvent(Context.ConnectionId);
        }

        public void Stop()
        {
            _logger.LogInformation("Stop Called");

            _gameContainer.Destroy();
        }


        public void UserEvent(string content)
        {
            _gameContainer.QueueUserEvent(Context.ConnectionId, content);
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            _logger.LogInformation($"${Context.ConnectionId} disconnected");

            _gameContainer.UserExitEvent(Context.ConnectionId).Wait();

            return base.OnDisconnectedAsync(exception);
        }

    }
}
