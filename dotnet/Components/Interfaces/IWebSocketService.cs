using System;
using System.Threading.Tasks;
using TeamHitori.Mulplay.Container.Web.Documents.Game;

namespace TeamHitori.Mulplay.Container.Web.Components.Interfaces
{
    public interface IWebSocketService
    {
        public IObservable<SocketConnectedDocument> OnMessage { get; }
        Task<bool> SendMessage(SocketConnectedDocument messageDoc, int retries = 3);
    }
}