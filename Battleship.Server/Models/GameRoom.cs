namespace Battleship.Server.Models;

using Battleship.Shared.Models;

public class GameRoom
{
    public string RoomCode { get; }
    public string Player1ConnectionId { get; set; } = string.Empty;
    public string? Player2ConnectionId { get; set; }

    public Board Player1Board { get; } = new();
    public Board Player2Board { get; } = new();

    public bool Player1Ready { get; set; }
    public bool Player2Ready { get; set; }

    public string CurrentTurnConnectionId { get; set; } = string.Empty;

    public GameRoom(string roomCode, string hostConnectionId)
    {
        RoomCode = roomCode;
        Player1ConnectionId = hostConnectionId;
    }

    public bool IsFull => !string.IsNullOrEmpty(Player2ConnectionId);

    public Board GetOpponentBoard(string connectionId) =>
        connectionId == Player1ConnectionId ? Player2Board : Player1Board;
}