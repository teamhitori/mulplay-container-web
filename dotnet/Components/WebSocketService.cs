using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Reactive.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using TeamHitori.Mulplay.Container.Web.Components.Helpers;
using TeamHitori.Mulplay.Container.Web.Components.Interfaces;
using TeamHitori.Mulplay.Container.Web.Documents.Game;

namespace TeamHitori.Mulplay.Container.Web.Components
{
    public class WebSocketService : IWebSocketService
    {
        public IObservable<SocketConnectedDocument> OnMessage { get; private set; }
        private ClientWebSocket _webSocket;
        private readonly ILogger<WebSocketService> _logger;
        private readonly Random _jitterer;
        private IEnumerable<string> _gameConnectionCache = new List<string>();

        public WebSocketService(
            ILogger<WebSocketService> logger
            )
        {
            _logger = logger;

            _jitterer = new Random();

            OnMessage = Observable.Create<SocketConnectedDocument>(async observer =>
            {
                var policy = Policy
                .Handle<Exception>()
                .WaitAndRetryForeverAsync(
                    sleepDurationProvider: (retry, ts) => { return TimeSpan.FromMinutes(1); },
                    onRetry: (Exception ex, int retry, TimeSpan ts, Context ctx) =>
                    {
                        _logger.LogInformation($"Retrying (reason: {ex.Message}) (retry count {retry})");
                    });

                await policy.ExecuteAsync(async () =>
                {
                    await SortoutWebsocket();

                    var buffer = new Byte[8192];
                    while (true)
                    {
                        ArraySegment<byte> bytesRec = new(buffer);
                        var cts = new CancellationTokenSource();
                        cts.CancelAfter(TimeSpan.FromDays(1));

                        var response = await this._webSocket.ReceiveAsync(bytesRec, cts.Token);

                        var msg = Encoding.UTF8.GetString(buffer, 0, response.Count);

                        var doc = JsonSerializer.Deserialize<SocketConnectedDocument>(msg);

                        observer.OnNext(doc);

                    }
                });

                return new Disposable(() =>
                {
                    observer.OnCompleted();
                });

            });
        }

        public async Task<bool> SendMessage(SocketConnectedDocument messageDoc, int retries = 3)
        {
            var message = JsonSerializer.Serialize(messageDoc);
            var bytesToSend = new ArraySegment<byte>(Encoding.UTF8.GetBytes(message));

            var res = await Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(retries, (retryAttempt, timespan) =>
                {
                    return TimeSpan.FromSeconds(Math.Pow(2, retryAttempt * 2))
                              + TimeSpan.FromMilliseconds(_jitterer.Next(0, 100));
                }, (ex, timespan, retryCount, context) =>
                {
                    _logger.LogError(ex, $"{ ex.Message }, retry: {retryCount}, timespan: {timespan}");
                })
                .ExecuteAndCaptureAsync(async () =>
                {
                    await SortoutWebsocket();

                    var cts = new CancellationTokenSource();
                    cts.CancelAfter(TimeSpan.FromSeconds(15));
                    await _webSocket.SendAsync(bytesToSend, WebSocketMessageType.Text, true, cts.Token);
                });

            reconcileNewGameConnections(messageDoc);

            if (res.Outcome == OutcomeType.Failure)
            {
                return false;
            }

            return true;
        }

        private void reconcileNewGameConnections(SocketConnectedDocument messageDoc)
        {
            if (messageDoc.topic == Topic.createGame)
            {
                _gameConnectionCache = _gameConnectionCache.Upsert(messageDoc.gamePrimaryName, x => x == messageDoc.gamePrimaryName);
            }
            else if (messageDoc.topic == Topic.destroyGame)
            {
                _gameConnectionCache = _gameConnectionCache.Upsert(null, x => x == messageDoc.gamePrimaryName);
            }
        }

        async Task SortoutWebsocket()
        {
            if (_webSocket == null || _webSocket.State != WebSocketState.Open)
            {
                _webSocket = new ClientWebSocket();
                var serviceUrl = new Uri("ws://mulplay-container-game:8080");

                var res = await Policy
                    .Handle<WebSocketException>()
                    .Or<InvalidOperationException>()
                    .Or<Exception>()
                    .WaitAndRetryAsync(10000, (retryAttempt, timespan) =>
                    {
                        return TimeSpan.FromSeconds(retryAttempt * 1.5 + 1)
                                  + TimeSpan.FromMilliseconds(_jitterer.Next(0, 100));
                    }, (ex, timespan, retryCount, context) =>
                    {
                        _logger.LogError(ex, $"{ ex.Message }, retry: {retryCount}, timespan: {timespan}");
                        this._webSocket?.Abort();

                        if(this._webSocket != null && !(this._webSocket.State == WebSocketState.Aborted || this._webSocket.State == WebSocketState.Closed))
                        {
                            var cts = new CancellationTokenSource();
                            cts.CancelAfter(TimeSpan.FromSeconds(15));
                            this._webSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", cts.Token).Wait();
                        }
                        
                        this._webSocket = new ClientWebSocket();
                    })
                    .ExecuteAndCaptureAsync(async () =>
                    {
                        try
                        {
                            var cts = new CancellationTokenSource();
                            cts.CancelAfter(TimeSpan.FromSeconds(15));
                            await _webSocket.ConnectAsync(serviceUrl, cts.Token);

                            _logger.LogInformation($"Websocket connected: {_webSocket.State}");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Wesockets connection error");
                            throw;
                        }


                    });

                foreach (var gamePrimaryName in _gameConnectionCache)
                {
                    await SendMessage(new SocketConnectedDocument(Topic.createGame, null, gamePrimaryName));
                }
            }
        }
    }
}
