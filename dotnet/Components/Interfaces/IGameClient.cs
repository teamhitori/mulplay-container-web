using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TeamHitori.Mulplay.Container.Web.Components.Interfaces
{
    public interface IGameClient
    {
        Task onPlayerEvent(string content);
        //Task OnUserEnterState(string content);
        Task OnPlayerExitState(string content);
        Task OnGameState(string content);
        Task OnMetrics(string content);
        Task OnGameEnd(string content);
        Task OnStep(string content);
        Task OnNotifyReload();
    }
}
