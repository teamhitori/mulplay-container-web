using Microsoft.AspNetCore.SignalR;
using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using TeamHitori.Mulplay.Container.Web.Components.Interfaces;
using Grpc.Net.Client;
using System.Threading;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using System.Reactive.Subjects;
using Grpc.Core;
using Microsoft.Extensions.Configuration;
using TeamHitori.Mulplay.Shared.Poco;
using System.Collections.Generic;
using TeamHitori.Mulplay.Container.Web.Documents.Game;
using System.Linq;

namespace TeamHitori.Mulplay.Container.Web.Components
{
    public class GameContainer
    {
        public IEnumerable<GameInstance> ActiveGameInstances { get; private set; } = new List<GameInstance>();

        private IHubContext<GameHub, IGameClient> _hubContext { get; }
        private GameService.GameServiceClient _grpcClient;
        private readonly ILogger<GameContainer> _logger;
        private IClientStreamWriter<ConnectedUserDocument> _queueUserEvent;
        //private CancellationTokenSource _gameTokensource;
        private bool _metricsActive;
        private Dictionary<string, bool> _loopActive = new Dictionary<string, bool>();
        private Dictionary<string, string> _connections = new Dictionary<string, string>();
        private Dictionary<string, string> _monitor = new Dictionary<string, string>();

        public GameContainer(
            IHubContext<GameHub, IGameClient> hubContext,
            ILogger<GameContainer> logger,
            GameService.GameServiceClient grpcClient)
        {
            _hubContext = hubContext;
            _logger = logger;
            _grpcClient = grpcClient;
            _logger.LogInformation("Game Container Called");
        }

        public void Start(string connectionId, string gamePrimaryName)
        {
            _logger.LogInformation($"## Start connectionId:{connectionId}, gamePrimaryName:{gamePrimaryName}");

            _connections[connectionId] = gamePrimaryName;

            StartGame(connectionId);

            StartMetrics(connectionId);

        }

        private void StartGame(string connectionId)
        {

            var connectionExists = _connections.TryGetValue(connectionId, out string gamePrimaryName);

            if (!connectionExists)
            {
                _logger.LogError($"StartGame could not find connection {connectionId}");
                return;
            }

            var entryExists = _loopActive.TryGetValue(gamePrimaryName, out bool loopActive);

            if (!loopActive)
            {
                _loopActive[gamePrimaryName] = true;

                //_gameTokensource?.Cancel();
                //_gameTokensource = new CancellationTokenSource();

                _queueUserEvent?.CompleteAsync();
                _queueUserEvent = _grpcClient.queueUserEvent().RequestStream;

                new Task(async () =>
                {
                    try
                    {
                        this._logger.LogInformation($"Game Loop starting - connectionId:{connectionId}, gamePrimaryName:{gamePrimaryName}");

                        var interval = DateTime.Now;

                        var grpcResponse = _grpcClient.startGame(
                            new ConnectedUserDocument { ConnectionId = connectionId, Content = JsonSerializer.Serialize("not used"), GamePrimaryName = gamePrimaryName });

                        while (await grpcResponse.ResponseStream.MoveNext())
                        {
                            var point = grpcResponse.ResponseStream.Current;

                            foreach (var conn in _connections)
                            {
                                if (conn.Value == gamePrimaryName)
                                {
                                    _ = _hubContext.Clients.Client(conn.Key).OnGameState(point.Content);
                                }
                            }

                            var finish = DateTime.Now;
                            var intervalMs = (finish - interval).TotalMilliseconds;
                            interval = finish;

                            //this._logger.LogInformation($"ms: {intervalMs}");
                        }

                        this._logger.LogInformation($"Game Loop ending - connectionId:{connectionId}, gamePrimaryName:{gamePrimaryName}");
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e.Message);
                    }
                    finally
                    {
                        _loopActive[gamePrimaryName] = false;
                    }

                }).Start();
            }
            else
            {
                this._logger.LogInformation($"Game Loop restarting - connectionId:{connectionId}, gamePrimaryName:{gamePrimaryName}");

                var grpcResponse = _grpcClient.restartGame(
                    new ConnectedUserDocument { ConnectionId = connectionId, GamePrimaryName = gamePrimaryName });
            }

        }

        public async Task CreateGame(GameInstance gameInstance, GameDefinition gameDefinition)
        {
            try
            {
                var grpcResponse = _grpcClient.createGame(new Document { Content = JsonSerializer.Serialize(gameDefinition), GamePrimaryName = gameInstance.gamePrimaryName });

                ActiveGameInstances = ActiveGameInstances.Upsert(gameInstance, i => i.gamePrimaryName == gameInstance.gamePrimaryName);

                foreach(var item in _monitor)
                {
                    if (item.Value == gameInstance.gameName)
                    {
                        await _hubContext.Clients.Client(item.Key).OnNotifyReload();
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
            }
        }

        private void StartMetrics(string connectionId)
        {
            var connectionExists = _connections.TryGetValue(connectionId, out string gamePrimaryName);

            if (!connectionExists)
            {
                _logger.LogError($"StartMetrics could not find connection {connectionId}");
                return;
            }

            if (!_metricsActive)
            {
                _metricsActive = true;

                new Task(async () =>
                {
                    try
                    {
                        var interval = DateTime.Now;

                        var grpcResponse = _grpcClient.startMetrics(
                            new ConnectedUserDocument { ConnectionId = connectionId, Content = JsonSerializer.Serialize("not used"), GamePrimaryName = gamePrimaryName });

                        while (await grpcResponse.ResponseStream.MoveNext())
                        {
                            var point = grpcResponse.ResponseStream.Current;

                            foreach (var conn in _connections)
                            {
                                if (conn.Value == gamePrimaryName)
                                {
                                    _ = _hubContext.Clients.Client(conn.Key).OnMetrics(point.Content);
                                }
                            }

                            var finish = DateTime.Now;
                            var intervalMs = (finish - interval).TotalMilliseconds;
                            interval = finish;

                            //this._logger.LogInformation(point.Content);
                            //this._logger.LogInformation($"Metrics ms: {intervalMs}");
                        }
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e.Message);
                    }
                    finally
                    {
                        _metricsActive = false;
                    }

                }).Start();

            }
        }

        public void QueueNewUserEvent(string connectionId)
        {
            try
            {
                var connectionExists = _connections.TryGetValue(connectionId, out string gamePrimaryName);

                if (!connectionExists)
                {
                    _logger.LogError($"QueueNewUserEvent could not find connection {connectionId}");
                    return;
                }

                var grpcResponse = _grpcClient.queueNewUser(
                    new ConnectedUserDocument { ConnectionId = connectionId, GamePrimaryName = gamePrimaryName });

                //var finish = DateTime.Now;
                //var intervalMs = (finish - interval).TotalMilliseconds;
                //interval = finish;

                //this._logger.LogInformation($"User {connectionId}, {grpcResponse.Content}");

                _hubContext.Clients.Client(grpcResponse.ConnectionId).OnUserEnterState(grpcResponse.Content);

            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
            }
        }

        public void QueueUserEvent(string connectionId, string content)
        {
            try
            {
                var connectionExists = _connections.TryGetValue(connectionId, out string gamePrimaryName);

                if (!connectionExists)
                {
                    _logger.LogError($"QueueUserEvent could not find connection {connectionId}");
                    return;
                }

                //var interval = DateTime.Now;

                _queueUserEvent.WriteAsync(new ConnectedUserDocument { ConnectionId = connectionId, Content = content, GamePrimaryName = gamePrimaryName });
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
            }

            //this._logger.LogInformation($"[{connectionId}] QueueUserEvent");

        }

        public async Task UserDisconnect(string connectionId)
        {
            _logger.LogInformation($"## User Exit Event {connectionId}");

            var connectionExists = _connections.TryGetValue(connectionId, out string gamePrimaryName);

            _connections.Remove(connectionId);
            _monitor.Remove(connectionId);

            var activeConnections = false;
            foreach (var item in _connections)
            {
                if(item.Value == gamePrimaryName)
                {
                    activeConnections = true;
                }
            }

            if (!activeConnections)
            {
                ActiveGameInstances = ActiveGameInstances.Remove(i => i.gamePrimaryName == gamePrimaryName);
            }

            if (!connectionExists)
            {
                _logger.LogError($"UserExitEvent could not find connection {connectionId}");
                return;
            }

            var grpcResponse = await _grpcClient.exitUserAsync(
                new ConnectedUserDocument { ConnectionId = connectionId, GamePrimaryName = gamePrimaryName });

            await _hubContext.Clients.Client(grpcResponse.ConnectionId).OnUserExitState(grpcResponse.Content);
        }

        public void Monitor(string connectionId, string gameName)
        {
            _monitor[connectionId] = gameName;
        }



        public void Step(string connectionId)
        {

            var connectionExists = _connections.TryGetValue(connectionId, out string gamePrimaryName);

            if (!connectionExists)
            {
                _logger.LogError($"Step could not find connection {connectionId}");
                return;
            }

            //var interval = DateTime.Now;

            var grpcResponse = _grpcClient.stepGame(
                new ConnectedUserDocument { ConnectionId = connectionId, Content = JsonSerializer.Serialize("not used"), GamePrimaryName = gamePrimaryName });

            //var finish = DateTime.Now;
            //var intervalMs = (finish - interval).TotalMilliseconds;
            //interval = finish;

            //this._logger.LogInformation($"ms: {intervalMs}");

            _hubContext.Clients.All.OnStep(grpcResponse.Content);

        }

        public void Destroy(string connectionId)
        {
            _logger.LogInformation($"## Destroy Called");

            var connectionExists = _connections.TryGetValue(connectionId, out string gamePrimaryName);

            if (!connectionExists)
            {
                _logger.LogError($"Destroy could not find connection {connectionId}");
                return;
            }

            // Define the cancellation token.
            // CancellationTokenSource source = new CancellationTokenSource();
            // CancellationToken token = source.Token;

            //this._logger.LogInformation($"connecting on http://loops-game-container");

            //var interval = DateTime.Now;

            var grpcResponse = _grpcClient.destroyGame(new Document() { Content = JsonSerializer.Serialize(""), GamePrimaryName = gamePrimaryName });

            _loopActive[gamePrimaryName] = false;

            ActiveGameInstances = ActiveGameInstances.Remove(i => i.gamePrimaryName == gamePrimaryName);

            foreach (var conn in _connections)
            {
                if (conn.Value == gamePrimaryName)
                {
                    _ = _hubContext.Clients.Client(conn.Key).OnGameEnd(grpcResponse.Content);
                }
            }
                }
    }
}
