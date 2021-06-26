using Microsoft.AspNetCore.SignalR;
using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using TeamHitori.Mulplay.Container.Web.Components.Interfaces;
using Grpc.Net.Client;
using GrpcConsole;
using System.Threading;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using System.Reactive.Subjects;
using Grpc.Core;
using Microsoft.Extensions.Configuration;

namespace TeamHitori.Mulplay.Container.Web.Components
{
    public class GameContainer
    {
        public IHubContext<GameHub, IGameClient> _hubContext { get; }
        //private IDisposable _gameLoop;
        private GameService.GameServiceClient _grpcClient;
        private readonly ILogger<GameContainer> _logger;
        private readonly IConfiguration _configuration;
        IClientStreamWriter<ConnectedUserDocument> _queueUserEvent;
        CancellationTokenSource _gameTokensource;


        public GameContainer(
            IHubContext<GameHub, IGameClient> hubContext,
            ILogger<GameContainer> logger,
            GameService.GameServiceClient grpcClient,
            IConfiguration configuration)
        {
            _hubContext = hubContext;
            _logger = logger;
            this._configuration = configuration;
            _logger.LogInformation("Game Container Called");

            _grpcClient = grpcClient;
        }

        public void Start(string connectionId)
        {
            _gameTokensource?.Cancel();
            _queueUserEvent?.CompleteAsync();

            _queueUserEvent = _grpcClient.queueUserEvent().RequestStream;

            _gameTokensource = new CancellationTokenSource();

            StartGame(connectionId)
                .Subscribe(state =>
                {
                    _hubContext.Clients.All.OnGameState(state);
                });

            StartMetrics(connectionId)
                .Subscribe(state =>
                {
                    _hubContext.Clients.All.OnMetrics(state);
                });
        }

        private IObservable<string> StartMetrics(string connectionId)
        {
            var subject = new Subject<string>();

            new Task(async () =>
            {
                try
                {
                    var interval = DateTime.Now;

                    var grpcResponse = _grpcClient.startMetrics(
                        new ConnectedUserDocument { ConnectionId = connectionId, Content = JsonSerializer.Serialize("not used") });

                    while (await grpcResponse.ResponseStream.MoveNext(_gameTokensource.Token))
                    {
                        var point = grpcResponse.ResponseStream.Current;

                        subject.OnNext(point.Content);

                        //var finish = DateTime.Now;
                        //var intervalMs = (finish - interval).TotalMilliseconds;
                        //interval = finish;

                        //this._logger.LogDebug($"ms: {intervalMs}");
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e.Message);
                }
                finally
                {
                    subject.OnCompleted();
                }

            }, _gameTokensource.Token).Start();

            return subject;
        }

        private IObservable<string> StartGame(string connectionId)
        {
            Subject<string> subject = new Subject<string>();

            new Task(async () =>
            {
                try
                {
                    //var interval = DateTime.Now;

                    var grpcResponse = _grpcClient.startGame(
                        new ConnectedUserDocument { ConnectionId = connectionId, Content = JsonSerializer.Serialize("not used") });

                    while (await grpcResponse.ResponseStream.MoveNext(_gameTokensource.Token))
                    {
                        var point = grpcResponse.ResponseStream.Current;

                        subject.OnNext(point.Content);

                        //var finish = DateTime.Now;
                        //var intervalMs = (finish - interval).TotalMilliseconds;
                        //interval = finish;

                        //this._logger.LogDebug($"ms: {intervalMs}");
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e.Message);
                }
                finally
                {
                    subject.OnCompleted();
                }

            }, _gameTokensource.Token).Start();

            return subject;
        }

        public void QueueNewUserEvent(string connectionId)
        {

           // var interval = DateTime.Now;

            var grpcResponse = _grpcClient.queueNewUser(
                new ConnectedUserDocument { ConnectionId = connectionId });

            //var finish = DateTime.Now;
            //var intervalMs = (finish - interval).TotalMilliseconds;
            //interval = finish;

            //this._logger.LogDebug($"ms: {intervalMs}");

            _hubContext.Clients.Client(grpcResponse.ConnectionId).OnUserEnterState(grpcResponse.Content);

        }

        public async Task QueueUserEvent(string connectionId, string content)
        {
            var interval = DateTime.Now;

            await _queueUserEvent.WriteAsync(new ConnectedUserDocument { ConnectionId = connectionId, Content = content });

            //this._logger.LogDebug($"[{connectionId}] QueueUserEvent");

        }

        public async Task UserExitEvent(string connectionId)
        {

            var grpcResponse = await _grpcClient.exitUserAsync(
                new ConnectedUserDocument { ConnectionId = connectionId });

            await _hubContext.Clients.Client(grpcResponse.ConnectionId).OnUserExitState(grpcResponse.Content);
        }

        public void Step(string connectionId)
        {

            //var interval = DateTime.Now;

            var grpcResponse = _grpcClient.stepGame(
                new ConnectedUserDocument { ConnectionId = connectionId, Content = JsonSerializer.Serialize("not used") });

            //var finish = DateTime.Now;
            //var intervalMs = (finish - interval).TotalMilliseconds;
            //interval = finish;

            //this._logger.LogDebug($"ms: {intervalMs}");

            _hubContext.Clients.Client(connectionId).OnStep(grpcResponse.Content);

        }

        public void Destroy()
        {
            // Define the cancellation token.
            CancellationTokenSource source = new CancellationTokenSource();
            CancellationToken token = source.Token;

            //this._logger.LogDebug($"connecting on http://loops-game-container");

            //var interval = DateTime.Now;

            var grpcResponse = _grpcClient.destroyGame(new Document() { Content = JsonSerializer.Serialize("") });


            _hubContext.Clients.All.OnGameEnd(grpcResponse.Content);

        }
    }
}
