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
using TeamHitori.Mulplay.shared.storage;

namespace TeamHitori.Mulplay.Container.Web.Components
{
    public class GameContainer
    {
        public IEnumerable<GameInstance> ActiveGameInstances { get { return _gameInstances.items; } }

        private IHubContext<GameHub, IGameClient> _hubContext { get; }
        private GameService.GameServiceClient _grpcClient;
        private Storage _storage;
        private readonly ILogger<GameContainer> _logger;
        private IClientStreamWriter<ConnectedPlayerDocument> _playerEventRequestStream;
        private Dictionary<string, string> _connections = new Dictionary<string, string>();
        private Dictionary<string, string> _monitorsGame = new Dictionary<string, string>();
        private Dictionary<string, string> _monitorsInstance = new Dictionary<string, string>();
        private Dictionary<string, string> _monitorActivePlayers = new Dictionary<string, string>();
        private GameInstances _gameInstances = new GameInstances(new List<GameInstance>());

        public GameContainer(
            IHubContext<GameHub, IGameClient> hubContext,
            ILogger<GameContainer> logger,
            GameService.GameServiceClient grpcClient,
            IStorageConfig storageConfig)
        {
            _hubContext = hubContext;
            _logger = logger;
            _grpcClient = grpcClient;
            _storage = storageConfig.ToUserStorage($"TeamHitori.Mulplay.Container.Web.Components.GameContainer");

            _logger.LogInformation("Game Container Called");
        }

        public async Task CreateGame(GameInstance gameInstance, GameDefinition gameDefinition)
        {
            try
            {
                var existingInstance = _gameInstances.items.FirstOrDefault(i => i.gamePrimaryName == gameInstance.gamePrimaryName);

                if (existingInstance != null)
                {
                    this._logger.LogInformation($"Game instance already exists:{gameInstance.gameName}, gamePrimaryName:{gameInstance.gamePrimaryName}");
                    return;
                }

                var grpcResponse = _grpcClient.createGame(new Document { Content = JsonSerializer.Serialize(gameDefinition), GamePrimaryName = gameInstance.gamePrimaryName });

                var items = _gameInstances.items.Upsert(gameInstance, inst => inst.gamePrimaryName == gameInstance.gamePrimaryName);

                _gameInstances = new GameInstances(items);

                await _storage.Upsert(_gameInstances, true);

            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
                throw;
            }
        }

        public async Task DestroyGame(string gamePrimaryName)
        {
            try
            {
                var grpcResponse = _grpcClient.destroyGame(new Document() { Content = JsonSerializer.Serialize(""), GamePrimaryName = gamePrimaryName });

                foreach (var conn in _connections.ToList())
                {
                    if (conn.Value == gamePrimaryName)
                    {
                        _ = _hubContext.Clients.Client(conn.Key).OnGameEnd(grpcResponse.Content);

                        _connections.Remove(conn.Key);
                    }
                }
            }
            catch (Exception e)
            {

                _logger.LogError(e.Message);
            }
            finally
            {
                var items = _gameInstances.items.Replace(i => i with { isStarted = false }, i => i.gamePrimaryName == gamePrimaryName);

                _gameInstances = new GameInstances(items);

                await _storage.Upsert(_gameInstances, true);
            }
        }

        public async Task NotifyReload(string gameName)
        {
            foreach (var item in _monitorsGame)
            {
                if (item.Value == gameName)
                {
                    await _hubContext.Clients.Client(item.Key).OnNotifyReload();
                }
            }
        }

        public async Task Start(string connectionId, string gamePrimaryName)
        {
            _logger.LogInformation($"## Start connectionId:{connectionId}, gamePrimaryName:{gamePrimaryName}");

            await StartGame(connectionId, gamePrimaryName);

            await StartMetrics(connectionId);

            NotifyActivePlayerCount(gamePrimaryName);
        }

        private void NotifyActivePlayerCount(string gamePrimaryName)
        {
            var game = _gameInstances.items.FirstOrDefault(i => i.gamePrimaryName == gamePrimaryName);

            if (game != null)
            {
                var activeInstances = _gameInstances.items.Where(inst => inst.gameName.StartsWith(game.gameName));
                var activePlayers = activeInstances.Aggregate(0, (count, inst) => GetActiveConnectionCount(inst.gamePrimaryName) + count);

                foreach (var conn in _monitorActivePlayers)
                {
                    if (game.gameName.StartsWith(conn.Value))
                    {
                        _ = _hubContext.Clients.Client(conn.Key).OnActivePlayerChange(activePlayers);
                    }

                }
            }

        }

        public async Task PlayerEnter(string connectionId)
        {
            var connectionExists = _connections.TryGetValue(connectionId, out string gamePrimaryName);

            if (!connectionExists)
            {
                _logger.LogError($"QueueNewPlayerEvent could not find connection {connectionId}");
                return;
            }

            try
            {
                var grpcResponse = _grpcClient.playerEnter(
                    new ConnectedPlayerDocument { ConnectionId = connectionId, GamePrimaryName = gamePrimaryName });

            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
                await DestroyGame(gamePrimaryName);
            }
        }

        public void PlayerEvent(string connectionId, string content)
        {
            try
            {
                if (_playerEventRequestStream == null)
                {
                    return;
                }

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
            _monitorsGame.Remove(connectionId);

            var activeConnections = false;
            foreach (var item in _connections.ToList())
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

            if (!activeConnections && !String.IsNullOrEmpty(gamePrimaryName))
            {
                await DestroyGame(gamePrimaryName);
            }

            NotifyActivePlayerCount(gamePrimaryName);
        }

        public void MontorGame(string connectionId, string gameName)
        {
            _monitorsGame[connectionId] = gameName;
        }


        public void MonitorActivePlayers(string connectionId, string gameLocation)
        {
            _monitorActivePlayers[connectionId] = gameLocation;
        }


        public void MonitorInstance(string connectionId, string gamePrimaryName)
        {
            _monitorsInstance[connectionId] = gamePrimaryName;
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

        public int GetActiveConnectionCount(string gamePrimaryName)
        {
            return _connections.Count(pair => pair.Value == gamePrimaryName);
        }

        public async Task EnableDebug(string gameName, Boolean enable)
        {
            var existingInstance = _gameInstances.items.FirstOrDefault(i => i.gameName.StartsWith(gameName));
            if (existingInstance != null)
            {
                var items = _gameInstances.items.Upsert(existingInstance with { isDebug = enable }, i => i.gameName.StartsWith(gameName));

                _gameInstances = new GameInstances(items);

                await _storage.Upsert(_gameInstances, true);
            }
        }

        private async Task StartGame(string connectionId, string gamePrimaryName)
        {
            var existingInstance = _gameInstances.items.FirstOrDefault(i => i.gamePrimaryName == gamePrimaryName);

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

                await StartGameLoop(connectionId, gamePrimaryName);

                StartPlayerEventLoop(gamePrimaryName);

                var items = _gameInstances.items.Upsert(existingInstance with { isStarted = true }, i => i.gamePrimaryName == gamePrimaryName);

                _gameInstances = new GameInstances(items);

                await _storage.Upsert(_gameInstances, true);
            }
            else
            {
                this._logger.LogInformation($"Game Loop restarting - connectionId:{connectionId}, gamePrimaryName:{gamePrimaryName}");

                var grpcResponse = _grpcClient.restartGame(
                    new Document { GamePrimaryName = gamePrimaryName });
            }

        }

        private async Task StartGameLoop(string connectionId, string gamePrimaryName)
        {

            try
            {
                this._logger.LogInformation($"Game Loop starting - connectionId:{connectionId}, gamePrimaryName:{gamePrimaryName}");

                var interval = DateTime.Now;

                var grpcResponse = _grpcClient.startGame(
                    new Document { Content = JsonSerializer.Serialize("not used"), GamePrimaryName = gamePrimaryName });

                new Task(async () =>
                {
                    try
                    {

                        while (await grpcResponse.ResponseStream.MoveNext())
                        {
                            var point = grpcResponse.ResponseStream.Current;

                            foreach (var conn in _connections.ToList())
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
                        await DestroyGame(gamePrimaryName);
                    }

                }).Start();
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
                await DestroyGame(gamePrimaryName);
            }

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

                        if (point == null)
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

        private async Task StartMetrics(string connectionId)
        {
            var connectionExists = _connections.TryGetValue(connectionId, out string gamePrimaryName);

            if (!connectionExists)
            {
                _logger.LogError($"StartMetrics could not find connection {connectionId}");
                return;
            }

            var existingInstance = _gameInstances.items.FirstOrDefault(i => i.gamePrimaryName == gamePrimaryName);


            if (!existingInstance.isMetricsActive)
            {
                var items = _gameInstances.items.Upsert(existingInstance with { isMetricsActive = true }, i => i.gamePrimaryName == gamePrimaryName);

                _gameInstances = new GameInstances(items);

                await _storage.Upsert(_gameInstances, true);

                new Task(async () =>
                {
                    try
                    {
                        var interval = DateTime.Now;

                        var grpcResponse = _grpcClient.startMetrics(
                            new Document { Content = JsonSerializer.Serialize("not used"), GamePrimaryName = gamePrimaryName });

                        while (await grpcResponse.ResponseStream.MoveNext())
                        {
                            if (!existingInstance.isDebug)
                            {
                                continue;
                            }

                            var connections = _monitorsInstance.Where(pair => pair.Value == existingInstance.gamePrimaryName).Select(pair => pair.Key);

                            var point = grpcResponse.ResponseStream.Current;

                            foreach (var conn in connections)
                            {
                                //if (conn.Value == gamePrimaryName)
                                {
                                    _ = _hubContext.Clients.Client(conn).OnMetrics(point.Content);
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
                        var items = _gameInstances.items.Upsert(existingInstance with { isMetricsActive = false }, i => i.gamePrimaryName == gamePrimaryName);

                        _gameInstances = new GameInstances(items);

                        await _storage.Upsert(_gameInstances, true);
                    }

                }).Start();

            }
        }

    }
}
