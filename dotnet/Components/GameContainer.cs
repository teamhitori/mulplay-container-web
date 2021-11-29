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
using System.Net.WebSockets;
using System.Text;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System.IO;

namespace TeamHitori.Mulplay.Container.Web.Components
{
    public class GameContainer
    {
        public IEnumerable<GameInstance> ActiveGameInstances { get { return _gameInstances.items; } }

        private IHubContext<GameHub, IGameClient> _hubContext { get; }
        //private GameService.GameServiceClient _grpcClient;
        private readonly Storage _storage;
        private readonly IStorageConfig _storageConfig;
        private readonly ILogger<GameContainer> _logger;
        private readonly IWebSocketService _webSocketService;
        //private IClientStreamWriter<ConnectedPlayerDocument> _playerEventRequestStream;
        private readonly IHttpService _httpService;

        private Dictionary<string, string> _connections = new Dictionary<string, string>();
        private Dictionary<string, string> _monitorsGame = new Dictionary<string, string>();
        private Dictionary<string, string> _monitorsInstance = new Dictionary<string, string>();
        private Dictionary<string, string> _monitorActivePlayers = new Dictionary<string, string>();
        private GameInstances _gameInstances = new GameInstances(new List<GameInstance>());

        public GameContainer(
            IHubContext<GameHub, IGameClient> hubContext,
            ILogger<GameContainer> logger,
            //GameService.GameServiceClient grpcClient,
            IWebSocketService webSocketService,
            IStorageConfig storageConfig,
            IHttpService httpService)
        {
            _hubContext = hubContext;
            _logger = logger;
            //_grpcClient = grpcClient;
            this._httpService = httpService;
            _storageConfig = storageConfig;
            _webSocketService = webSocketService;
            _storage = storageConfig.ToUserStorage($"TeamHitori.Mulplay.Container.Web.Components.GameContainer");
            _logger.LogInformation("Game Container Called");

        }

        public async Task CreateGame(GameInstance gameInstance, GameDefinition gameDefinition)
        {
            try
            {
                var existingInstance = _gameInstances.items.FirstOrDefault(i => i.gamePrimaryName == gameInstance.gamePrimaryName);

                //if (existingInstance != null)
                //{
                //    this._logger.LogInformation($"Game instance already exists:{gameInstance.gameName}, gamePrimaryName:{gameInstance.gamePrimaryName}");
                //    return;
                //}

                var message = new SocketConnectedDocument(Topic.createGame, null, gameInstance.gamePrimaryName, JsonSerializer.Serialize(gameDefinition));

                await _webSocketService.SendMessage(message);

                //var grpcResponse = _grpcClient.createGame(new Document { Content = JsonSerializer.Serialize(gameDefinition), GamePrimaryName = gameInstance.gamePrimaryName });

                var items = _gameInstances.items.Upsert(gameInstance, inst => inst.gamePrimaryName == gameInstance.gamePrimaryName);

                _gameInstances = new GameInstances(items);

                await _storage.Upsert(_gameInstances, true);

                StartWebsocketReceive(gameInstance.gamePrimaryName);

            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
                throw;
            }
        }

        private void StartWebsocketReceive(string gamePrimaryName)
        {
            try
            {
                _webSocketService.OnMessage
                    .Subscribe(async message =>
                        {
                            _logger.LogInformation($"Websocket msg: {message.topic} ");

                            if (message.topic == Topic.destroyGame)
                            {

                            }
                        });
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
            }
        }

        public async Task DestroyGame(string gamePrimaryName)
        {
            try
            {
                var items = _gameInstances.items.Upsert(null, inst => inst.gamePrimaryName == gamePrimaryName);

                _gameInstances = new GameInstances(items);

                await _webSocketService.SendMessage(new SocketConnectedDocument(Topic.destroyGame, null, gamePrimaryName));

            }
            catch (Exception e)
            {

                _logger.LogError(e.Message);
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

        private Dictionary<string, bool> _compilerStarted = new Dictionary<string, bool>();

        public void StartCompile(string userId, string userName, string gameName, string bodyStr)
        {
            var gameLocation = $"debug:{ userId }:{ gameName }";

            if (_compilerStarted.TryGetValue(gameLocation, out var compilerStarted))
            {
                if (compilerStarted)
                {
                    _logger.LogInformation($"Compile { gameLocation } already started");
                }
            }
            new Task(async () =>
            {
                try
                {
                    _compilerStarted[gameLocation] = true;

                    await _httpService.UrlPostType<object>($"http://mulplay-container-build/set/{ userId }", bodyStr, 0);

                    while (_compilerStarted[gameLocation])
                    {

                        var status = await _httpService.UrlGetType<CompilationStatus>($"http://mulplay-container-build/poll/{ userId }", 0);

                        if (status == null)
                        {
                            return;
                        } else
                        {
                            await NotifyCompilation(gameLocation, status);
                        }

                        if (status.isComplete)
                        {
                            _compilerStarted[gameLocation] = false;

                            // var frontEndCodeDoc = await storage.FindDocumentByPrimaryName<CompiledCode>($"{gameName}:{CodeType.FrontendLogic}");

                            var storage = _storageConfig.ToUserStorage(userId);

                            //Create a unique name for the container
                            string containerName = $"debug-{ userName }-{ gameName }";

                            // Create the container and return a container client object
                            BlobContainerClient containerClient = storage.BlobServiceClient.GetBlobContainerClient(containerName);

                            await containerClient.CreateIfNotExistsAsync();

                            await containerClient.SetAccessPolicyAsync(PublicAccessType.Blob);

                            // Get a reference to a blob
                            var blobClient = containerClient.GetBlobClient("app.js");

                            var exists = await blobClient.ExistsAsync();

                            if (exists)
                            {
                                storage.LogInformation($"blob app.js already exists, deleting");
                                await blobClient.DeleteAsync();
                            }

                            using (var stream = new MemoryStream())
                            using (var writer = new StreamWriter(stream))
                            {
                                writer.Write(status.source);
                                writer.Flush();
                                stream.Position = 0;

                                await blobClient.UploadAsync(stream, new BlobHttpHeaders { ContentType = "application/javascript" });
                            }
                        }
                        else
                        {
                            await Task.Delay(1000);
                        }
                    }
                }
                catch(Exception e) {
                    _logger.LogError(e.Message);
                }
            }).Start();
        }

        private async Task NotifyCompilation(string gameLocation, CompilationStatus status)
        {
            foreach (var item in _monitorsGame)
            {
                if (item.Value == gameLocation)
                {
                    await _hubContext.Clients.Client(item.Key).OnNotifyCompilation(status);
                }
            }
        }

        //public async Task StartMetrics(string connectionId, string gamePrimaryName)
        //{
        //    _logger.LogInformation($"## StartMetrics connectionId:{connectionId}, gamePrimaryName:{gamePrimaryName}");

        //    // await StartGame(connectionId, gamePrimaryName);

        //    await StartMetrics(connectionId);

        //    NotifyActivePlayerCount(gamePrimaryName);
        //}

        //public async Task _PlayerEnter(string connectionId)
        //{
        //    var connectionExists = _connections.TryGetValue(connectionId, out string gamePrimaryName);

        //    if (!connectionExists)
        //    {
        //        _logger.LogError($"QueueNewPlayerEvent could not find connection {connectionId}");
        //        return;
        //    }

        //    try
        //    {
        //        //var grpcResponse = _grpcClient.playerEnter(
        //        //    new ConnectedPlayerDocument { ConnectionId = connectionId, GamePrimaryName = gamePrimaryName });

        //    }
        //    catch (Exception e)
        //    {
        //        _logger.LogError(e.Message);
        //        await DestroyGame(gamePrimaryName);
        //    }
        //}

        //public void _PlayerEvent(string connectionId, string content)
        //{
        //    try
        //    {
        //        if (_playerEventRequestStream == null)
        //        {
        //            return;
        //        }

        //        var connectionExists = _connections.TryGetValue(connectionId, out string gamePrimaryName);

        //        if (!connectionExists)
        //        {
        //            _logger.LogError($"QueuePlayerEvent could not find connection {connectionId}");
        //            return;
        //        }

        //        //var interval = DateTime.Now;

        //        _playerEventRequestStream.WriteAsync(new ConnectedPlayerDocument { ConnectionId = connectionId, Content = content, GamePrimaryName = gamePrimaryName });
        //    }
        //    catch (Exception e)
        //    {
        //        _logger.LogError(e.Message);
        //    }

        //    //this._logger.LogInformation($"[{connectionId}] QueuePlayerEvent");

        //}

        //public async Task _PlayerDisconnect(string connectionId)
        //{
        //    _logger.LogInformation($"## Player Exit Event {connectionId}");

        //    var connectionExists = _connections.TryGetValue(connectionId, out string gamePrimaryName);

        //    _connections.Remove(connectionId);
        //    _monitorsGame.Remove(connectionId);

        //    var activeConnections = false;
        //    foreach (var item in _connections.ToList())
        //    {
        //        if (item.Value == gamePrimaryName)
        //        {
        //            activeConnections = true;
        //        }
        //    }

        //    if (connectionExists)
        //    {
        //        //var grpcResponse = await _grpcClient.playerExitAsync(
        //        //new ConnectedPlayerDocument { ConnectionId = connectionId, GamePrimaryName = gamePrimaryName });

        //        //await _hubContext.Clients.Client(grpcResponse.ConnectionId).OnPlayerExitState(grpcResponse.Content);
        //    }

        //    if (!activeConnections && !String.IsNullOrEmpty(gamePrimaryName))
        //    {
        //        await DestroyGame(gamePrimaryName);
        //    }

        //    NotifyActivePlayerCount(gamePrimaryName);
        //}

        public void MontorGame(string connectionId, string gameLocation)
        {
            _monitorsGame[connectionId] = gameLocation;
        }

        public void MonitorActivePlayers(string connectionId, string gameLocation)
        {
            _monitorActivePlayers[connectionId] = gameLocation;
        }

        public void MonitorInstance(string connectionId, string gamePrimaryName)
        {
            _monitorsInstance[connectionId] = gamePrimaryName;
        }

        //public void Step(string connectionId)
        //{

        //    var connectionExists = _connections.TryGetValue(connectionId, out string gamePrimaryName);

        //    if (!connectionExists)
        //    {
        //        _logger.LogError($"Step could not find connection {connectionId}");
        //        return;
        //    }

        //    //var interval = DateTime.Now;

        //    //var grpcResponse = _grpcClient.stepGame(
        //    //    new Document { Content = JsonSerializer.Serialize("not used"), GamePrimaryName = gamePrimaryName });

        //    //var finish = DateTime.Now;
        //    //var intervalMs = (finish - interval).TotalMilliseconds;
        //    //interval = finish;

        //    //this._logger.LogInformation($"ms: {intervalMs}");

        //    //_hubContext.Clients.All.OnStep(grpcResponse.Content);

        //}

        //public int GetActiveConnectionCount(string gamePrimaryName)
        //{
        //    return _connections.Count(pair => pair.Value == gamePrimaryName);
        //}

        //public async Task EnableDebug(string gameName, Boolean enable)
        //{
        //    var existingInstance = _gameInstances.items.FirstOrDefault(i => i.gameName.StartsWith(gameName));
        //    if (existingInstance != null)
        //    {
        //        var items = _gameInstances.items.Upsert(existingInstance with { isDebug = enable }, i => i.gameName.StartsWith(gameName));

        //        _gameInstances = new GameInstances(items);

        //        await _storage.Upsert(_gameInstances, true);
        //    }
        //}

        //private void NotifyActivePlayerCount(string gamePrimaryName)
        //{
        //    var game = _gameInstances.items.FirstOrDefault(i => i.gamePrimaryName == gamePrimaryName);

        //    if (game != null)
        //    {
        //        var activeInstances = _gameInstances.items.Where(inst => inst.gameName.StartsWith(game.gameName));
        //        var activePlayers = activeInstances.Aggregate(0, (count, inst) => GetActiveConnectionCount(inst.gamePrimaryName) + count);

        //        foreach (var conn in _monitorActivePlayers)
        //        {
        //            if (game.gameName.StartsWith(conn.Value))
        //            {
        //                //_ = _hubContext.Clients.Client(conn.Key).OnActivePlayerChange(activePlayers);
        //            }

        //        }
        //    }

        //}

        //private async Task StartGame(string connectionId, string gamePrimaryName)
        //{
        //    var existingInstance = _gameInstances.items.FirstOrDefault(i => i.gamePrimaryName == gamePrimaryName);

        //    if (existingInstance == null)
        //    {
        //        _logger.LogError($"Could not find Active Game Instance for gamePrimaryName: {gamePrimaryName}");
        //        return;
        //    }

        //    _connections[connectionId] = gamePrimaryName;

        //    if (!existingInstance?.isStarted == true)
        //    {
        //        //_loopActive[gamePrimaryName] = true;

        //        //var playerEvent = _grpcClient.playerEventIn();

        //        _playerEventRequestStream?.CompleteAsync();
        //        //_playerEventRequestStream = playerEvent.RequestStream;

        //        await StartGameLoop(connectionId, gamePrimaryName);

        //        StartPlayerEventLoop(gamePrimaryName);

        //        var items = _gameInstances.items.Upsert(existingInstance with { isStarted = true }, i => i.gamePrimaryName == gamePrimaryName);

        //        _gameInstances = new GameInstances(items);

        //        await _storage.Upsert(_gameInstances, true);
        //    }
        //    else
        //    {
        //        this._logger.LogInformation($"Game Loop restarting - connectionId:{connectionId}, gamePrimaryName:{gamePrimaryName}");

        //        //var grpcResponse = _grpcClient.restartGame(
        //        //    new Document { GamePrimaryName = gamePrimaryName });
        //    }

        //}

        //private async Task StartGameLoop(string connectionId, string gamePrimaryName)
        //{

        //    try
        //    {
        //        this._logger.LogInformation($"Game Loop starting - connectionId:{connectionId}, gamePrimaryName:{gamePrimaryName}");

        //        var interval = DateTime.Now;

        //        //var grpcResponse = _grpcClient.startGame(
        //        //    new Document { Content = JsonSerializer.Serialize("not used"), GamePrimaryName = gamePrimaryName });

        //        new Task(async () =>
        //        {
        //            try
        //            {

        //                //while (await grpcResponse.ResponseStream.MoveNext())
        //                //{
        //                //    var point = grpcResponse.ResponseStream.Current;

        //                //    foreach (var conn in _connections.ToList())
        //                //    {
        //                //        if (conn.Value == gamePrimaryName)
        //                //        {
        //                //            _ = _hubContext.Clients.Client(conn.Key).OnGameState(point.Content);
        //                //        }
        //                //    }

        //                //    var finish = DateTime.Now;
        //                //    var intervalMs = (finish - interval).TotalMilliseconds;
        //                //    interval = finish;

        //                //    //this._logger.LogInformation($"ms: {intervalMs}");
        //                //}

        //                this._logger.LogInformation($"Game Loop ending - connectionId:{connectionId}, gamePrimaryName:{gamePrimaryName}");
        //            }
        //            catch (Exception e)
        //            {
        //                _logger.LogError(e.Message);
        //            }
        //            finally
        //            {
        //                await DestroyGame(gamePrimaryName);
        //            }

        //        }).Start();
        //    }
        //    catch (Exception e)
        //    {
        //        _logger.LogError(e.Message);
        //        await DestroyGame(gamePrimaryName);
        //    }

        //}

        //private void StartPlayerEventLoop(string gamePrimaryName)
        //{
        //    new Task(async () =>
        //    {
        //        try
        //        {
        //            this._logger.LogInformation($"Player Event Loop starting - gamePrimaryName:{gamePrimaryName}");

        //            var interval = DateTime.Now;

        //            //var grpcResponse = _grpcClient.playerEventOut(
        //            //    new Document { GamePrimaryName = gamePrimaryName });

        //            //while (await grpcResponse.ResponseStream.MoveNext())
        //            //{
        //            //    var point = grpcResponse.ResponseStream.Current;

        //            //    if (point == null)
        //            //    {
        //            //        continue;
        //            //    }

        //            //    _ = _hubContext.Clients.Client(point.ConnectionId).onPlayerEvent(point.Content);

        //            //    var finish = DateTime.Now;
        //            //    var intervalMs = (finish - interval).TotalMilliseconds;
        //            //    interval = finish;

        //            //    //this._logger.LogInformation($"ms: {intervalMs}");
        //            //}

        //            this._logger.LogInformation($"Player Event Loop ending -, gamePrimaryName:{gamePrimaryName}");
        //        }
        //        catch (Exception e)
        //        {
        //            _logger.LogError(e.Message);
        //        }

        //    }).Start();
        //}

        //private async Task StartMetrics(string connectionId)
        //{
        //    var connectionExists = _connections.TryGetValue(connectionId, out string gamePrimaryName);

        //    if (!connectionExists)
        //    {
        //        _logger.LogError($"StartMetrics could not find connection {connectionId}");
        //        return;
        //    }

        //    var existingInstance = _gameInstances.items.FirstOrDefault(i => i.gamePrimaryName == gamePrimaryName);


        //    if (!existingInstance.isMetricsActive)
        //    {
        //        var items = _gameInstances.items.Upsert(existingInstance with { isMetricsActive = true }, i => i.gamePrimaryName == gamePrimaryName);

        //        _gameInstances = new GameInstances(items);

        //        await _storage.Upsert(_gameInstances, true);

        //        new Task(async () =>
        //        {
        //            try
        //            {
        //                var interval = DateTime.Now;

        //                //var grpcResponse = _grpcClient.startMetrics(
        //                //    new Document { Content = JsonSerializer.Serialize("not used"), GamePrimaryName = gamePrimaryName });

        //                //while (await grpcResponse.ResponseStream.MoveNext())
        //                //{
        //                //    if (!existingInstance.isDebug)
        //                //    {
        //                //        continue;
        //                //    }

        //                //    var connections = _monitorsInstance.Where(pair => pair.Value == existingInstance.gamePrimaryName).Select(pair => pair.Key);

        //                //    var point = grpcResponse.ResponseStream.Current;

        //                //    foreach (var conn in connections)
        //                //    {
        //                //        //if (conn.Value == gamePrimaryName)
        //                //        {
        //                //            _ = _hubContext.Clients.Client(conn).OnMetrics(point.Content);
        //                //        }
        //                //    }

        //                //    var finish = DateTime.Now;
        //                //    var intervalMs = (finish - interval).TotalMilliseconds;
        //                //    interval = finish;

        //                //    //this._logger.LogInformation(point.Content);
        //                //    //this._logger.LogInformation($"Metrics ms: {intervalMs}");
        //                //}
        //            }
        //            catch (Exception e)
        //            {
        //                _logger.LogError(e.Message);
        //            }
        //            finally
        //            {
        //                var items = _gameInstances.items.Upsert(existingInstance with { isMetricsActive = false }, i => i.gamePrimaryName == gamePrimaryName);

        //                _gameInstances = new GameInstances(items);

        //                await _storage.Upsert(_gameInstances, true);
        //            }

        //        }).Start();

        //    }
        //}

    }
}
