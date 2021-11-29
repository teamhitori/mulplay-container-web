using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TeamHitori.Mulplay.Container.Web.Documents.Game
{
    public record SocketConnectedDocument(
        Topic topic,
        string connectionId = null,
        string gamePrimaryName = null,
        string content = null

        );


    public enum Topic
    {
        ping,
        createGame,
        startGame,
        restartGame,
        metrics,
        destroyGame,
        playerEnter,
        playerExit,
        playerEventIn,
        playerEventOut,
        gameLoop,
        gameEnd
    }
}
