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
        private IClientStreamWriter<ConnectedPlayerDocument> _playerEventRequestStream;
        //private IAsyncStreamReader<ConnectedPlayerDocument> _playerEventResponseStream;

        //private CancellationTokenSource _gameTokensource;
        private bool _metricsActive;
        //private Dictionary<string, bool> _loopActive = new Dictionary<string, bool>();
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

        public async Task CreateGame(GameInstance gameInstance, GameDefinition gameDefinition)
        {
            try
            {
                var existingInstance = ActiveGameInstances.FirstOrDefault(i =>  i.gamePrimaryName == gameInstance.gamePrimaryName );

                if (existingInstance !=  null)
                {
                    this._logger.LogInformation($"Game instance already exists:{gameInstance.gameName}, gamePrimaryName:{gameInstance.gamePrimaryName}");
                    return;
                }

                var grpcResponse = _grpcClient.createGame(new Document { Content = JsonSerializer.Serialize(gameDefinition), GamePrimaryName = gameInstance.gamePrimaryName });

                ActiveGameInstances = ActiveGameInstances.Upsert(gameInstance, inst => inst.gamePrimaryName == gameInstance.gamePrimaryName);

                foreach (var item in _monitor)
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

        public void Start(string connectionId, string gamePrimaryName)
        {
            _logger.LogInformation($"## Start connectionId:{connectionId}, gamePrimaryName:{gamePrimaryName}");

            StartGame(connectionId, gamePrimaryName);

            StartMetrics(connectionId);

        }


        public void PlayerEnter(string connectionId)
        {
            try
            {
                var connectionExists = _connections.TryGetValue(connectionId, out string gamePrimaryName);

                if (!connectionExists)
                {
                    _logger.LogError($"QueueNewPlayerEvent could not find connection {connectionId}");
                    return;
                }

                var grpcResponse = _grpcClient.playerEnter(
                    new ConnectedPlayerDocument { ConnectionId = connectionId, GamePrimaryName = gamePrimaryName });

                //var finish = DateTime.Now;
                //var intervalMs = (finish - interval).TotalMilliseconds;
                //interval = finish;

                //this._logger.LogInformation($"Player {connectionId}, {grpcResponse.Content}");


                //_playerEventResponseStream = grpcResponse.ResponseStream;

                

                //_hubContext.Clients.Client(grpcResponse.ConnectionId).OnPlayerEnterState(grpcResponse.Content);

            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
            }
        }

        public void PlayerEvent(string connectionId, string content)
        {
            try
            {
                var connectionExists = _connections.TryGetValue(connectionId, out string gamePrimaryName);

                if (!connectionExists)
                {
                    _logger.LogError($"QueuePlayerEvent could not find connection {connectionId}");
                    return;
                }

                //var interval = DateTime.Now;

                _playerEventRequestStream.WriteAsync(new ConnectedPlayerDocument { ConnectionId = connectionId, Content = content, GamePrimaryName = gamePrimaryName });
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
            }

            //this._logger.LogInformation($"[{connectionId}] QueuePlayerEvent");

        }

        public async Task PlayerDisconnect(string connectionId)
        {
            _logger.LogInformation($"## Player Exit Event {connectionId}");

            var connectionExists = _connections.TryGetValue(connectionId, out string gamePrimaryName);

            _connections.Remove(connectionId);
            _monitor.Remove(connectionId);

            var activeConnections = false;
            foreach (var item in _connections)
            {
                if (item.Value == gamePrimaryName)
                {
                    activeConnections = true;
                }
            }

            if (connectionExists)
            {
                var grpcResponse = await _grpcClient.playerExitAsync(
                new ConnectedPlayerDocument { ConnectionId = connectionId, GamePrimaryName = gamePrimaryName });

                await _hubContext.Clients.Client(grpcResponse.ConnectionId).OnPlayerExitState(grpcResponse.Content);
            }

            if (!activeConnections)
            {
                DestroyGame(gamePrimaryName);
            }
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
                new Document { Content = JsonSerializer.Serialize("not used"), GamePrimaryName = gamePrimaryName });

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
            DestroyGame(gamePrimaryName);
        }

        private void DestroyGame(string gamePrimaryName)
        {
            try
            {
                var grpcResponse = _grpcClient.destroyGame(new Document() { Content = JsonSerializer.Serialize(""), GamePrimaryName = gamePrimaryName });

                foreach (var conn in _connections)
                {
                    if (conn.Value == gamePrimaryName)
                    {
                        _ = _hubContext.Clients.Client(conn.Key).OnGameEnd(grpcResponse.Content);
                    }
                }

            }
            catch (Exception e)
            {

                _logger.LogError(e.Message);
            }
            finally
            {
                ActiveGameInstances = ActiveGameInstances.Remove(i => i.gamePrimaryName == gamePrimaryName);
            }

        }

        private void StartGame(string connectionId, string gamePrimaryName)
        {
            var existingInstance = ActiveGameInstances.FirstOrDefault(i => i.gamePrimaryName == gamePrimaryName);

            if (existingInstance == null)
            {
                _logger.LogError($"Could not find Active Game Instance for gamePrimaryName: {gamePrimaryName}");
                return;
            }

            _connections[connectionId] = gamePrimaryName;

            if (!existingInstance?.isStarted == true)
            {
                //_loopActive[gamePrimaryName] = true;

                var playerEvent = _grpcClient.playerEventIn();

                _playerEventRequestStream?.CompleteAsync();
                _playerEventRequestStream = playerEvent.RequestStream;

                StartGameLoop(connectionId, gamePrimaryName);

                StartPlayerEventLoop(gamePrimaryName);

                ActiveGameInstances = ActiveGameInstances.Upsert(existingInstance with { isStarted = true }, i => i.gamePrimaryName == gamePrimaryName);
            }
            else
            {
                this._logger.LogInformation($"Game Loop restarting - connectionId:{connectionId}, gamePrimaryName:{gamePrimaryName}");

                var grpcResponse = _grpcClient.restartGame(
                    new Document { GamePrimaryName = gamePrimaryName });
            }

        }

        private void StartGameLoop(string connectionId, string gamePrimaryName)
        {
            new Task(async () =>
            {
                try
                {
                    this._logger.LogInformation($"Game Loop starting - connectionId:{connectionId}, gamePrimaryName:{gamePrimaryName}");

                    var interval = DateTime.Now;

                    var grpcResponse = _grpcClient.startGame(
                        new Document { Content = JsonSerializer.Serialize("not used"), GamePrimaryName = gamePrimaryName });

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
                    DestroyGame(gamePrimaryName);
                }

            }).Start();
        }

        private void StartPlayerEventLoop(string gamePrimaryName)
        {
            new Task(async () =>
            {
                try
                {
                    this._logger.LogInformation($"Player Event Loop starting - gamePrimaryName:{gamePrimaryName}");

                    var interval = DateTime.Now;

                    var grpcResponse = _grpcClient.playerEventOut(
                        new Document { GamePrimaryName = gamePrimaryName });

                    while (await grpcResponse.ResponseStream.MoveNext())
                    {
                        var point = grpcResponse.ResponseStream.Current;

                        if(point == null)
                        {
                            continue;
                        }

                        _ = _hubContext.Clients.Client(point.ConnectionId).onPlayerEvent(point.Content);

                        var finish = DateTime.Now;
                        var intervalMs = (finish - interval).TotalMilliseconds;
                        interval = finish;

                        //this._logger.LogInformation($"ms: {intervalMs}");
                    }

                    this._logger.LogInformation($"Player Event Loop ending -, gamePrimaryName:{gamePrimaryName}");
                }
                catch (Exception e)
                {
                    _logger.LogError(e.Message);
                }

            }).Start();
        }

        private void StartMetrics(string connectionId)
        {
            var connectionExists = _connections.TryGetValue(connectionId, out string gamePrimaryName);

            if (!connectionExists)
            {
                _logger.LogError($"StartMetrics could not find connection {connectionId}");
                return;
            }

            var existingInstance = ActiveGameInstances.FirstOrDefault(i => i.gamePrimaryName == gamePrimaryName);

            if (!existingInstance.isDebug)
            {
                _logger.LogError($"Game:{existingInstance.gameName}, Instance:{gamePrimaryName} is not Debuggable");
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
                            new Document { Content = JsonSerializer.Serialize("not used"), GamePrimaryName = gamePrimaryName });

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

    }
}
