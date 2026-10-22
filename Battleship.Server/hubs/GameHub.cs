namespace Battleship.Server.Hubs;

using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using Battleship.Server.Models;
using Battleship.Shared.DTOs;
using Battleship.Shared.Models;

public class GameHub : Hub
{    
    private static readonly ConcurrentDictionary<string, GameRoom> Rooms = new();

    public async Task<string> CreateRoom()
    {
        string roomCode = Guid.NewGuid().ToString()[..6].ToUpper();
        var room = new GameRoom(roomCode, Context.ConnectionId);

        Rooms[roomCode] = room;
        await Groups.AddToGroupAsync(Context.ConnectionId, roomCode);

        return roomCode;
    }

    public async Task<bool> JoinRoom(string roomCode)
    {
        roomCode = roomCode.ToUpper().Trim();

        if (Rooms.TryGetValue(roomCode, out var room) && !room.IsFull)
        {
            room.Player2ConnectionId = Context.ConnectionId;
            await Groups.AddToGroupAsync(Context.ConnectionId, roomCode);

            await Clients.Group(roomCode).SendAsync("PlayerJoined", roomCode);
            return true;
        }

        return false;
    }
    public override async Task OnConnectedAsync()
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"[+] Нове підключення! ConnectionId: {Context.ConnectionId}");
        Console.ResetColor();

        await base.OnConnectedAsync();
    }


    public async Task FireShot(PlayerMoveDto move)
    {
        if (!Rooms.TryGetValue(move.RoomCode, out var room)) return;

        if (room.CurrentTurnConnectionId != Context.ConnectionId) return;

        var targetBoard = room.GetOpponentBoard(Context.ConnectionId);
        var (isHit, isSunk, sunkShip) = targetBoard.ProcessShot(move.Target);
        bool isGameOver = targetBoard.AreAllSunk();

        if (!isHit)
        {
            room.CurrentTurnConnectionId = (Context.ConnectionId == room.Player1ConnectionId)
                ? room.Player2ConnectionId!
                : room.Player1ConnectionId;
        }

        var result = new ShotResultDto(
            Target: move.Target,
            IsHit: isHit,
            IsSunk: isSunk,
            SunkShipName: sunkShip?.Name,
            SunkShipCoordinates: isSunk ? sunkShip?.OccupiedPoints : null,
            IsGameOver: isGameOver,
            NextPlayerTurnId: room.CurrentTurnConnectionId
        );

        await Clients.Group(move.RoomCode).SendAsync("ReceiveShotResult", result);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"[-] Клієнт відключився: {Context.ConnectionId}");
        Console.ResetColor();

        var room = Rooms.Values.FirstOrDefault(r =>
            r.Player1ConnectionId == Context.ConnectionId || r.Player2ConnectionId == Context.ConnectionId);

        if (room != null)
        {
            await Clients.Group(room.RoomCode).SendAsync("OpponentDisconnected");
            Rooms.TryRemove(room.RoomCode, out _);
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task LeaveRoom(string roomCode)
    {
        roomCode = roomCode.ToUpper().Trim();

        if (Rooms.TryRemove(roomCode, out var room))
        {
            // Сповіщаємо іншого гравця про вихід суперника
            await Clients.Group(roomCode).SendAsync("OpponentDisconnected");
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomCode);
        }
    }

    public async Task PlayerReady(string roomCode, List<Point> shipCoordinates)
    {
        if (!Rooms.TryGetValue(roomCode, out var room)) return;

        var targetBoard = Context.ConnectionId == room.Player1ConnectionId
            ? room.Player1Board
            : room.Player2Board;

        // Очищаємо і наповнюємо дошку на сервері саме тими точками, які бачить гравець
        targetBoard.SetShipsFromPoints(shipCoordinates);

        if (Context.ConnectionId == room.Player1ConnectionId)
            room.Player1Ready = true;
        else if (Context.ConnectionId == room.Player2ConnectionId)
            room.Player2Ready = true;

        if (room.Player1Ready && room.Player2Ready)
        {
            room.CurrentTurnConnectionId = room.Player1ConnectionId;
            await Clients.Group(roomCode).SendAsync("GameStarted", room.CurrentTurnConnectionId);
        }
    }
}