namespace Battleship.Client.Services;

using Microsoft.AspNetCore.SignalR.Client;
using Battleship.Shared.DTOs;
using Battleship.Shared.Models;

public class GameHubService : IAsyncDisposable
{
    private HubConnection? _hubConnection;

    public event Action<string>? OnPlayerJoined;
    public event Action<ShotResultDto>? OnShotResultReceived;
    public event Action? OnOpponentDisconnected;
    public event Action<string>? OnGameStarted;

    public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;

    public async Task StartConnectionAsync(string serverUrl = "https://battleship-server-api-h7bzc9e0excrcned.polandcentral-01.azurewebsites.net/gamehub")
    {
        if (_hubConnection != null) return;

        _hubConnection = new HubConnectionBuilder()
            .WithUrl(serverUrl)
            .WithAutomaticReconnect()
            .Build();

        _hubConnection.On<string>("PlayerJoined", roomCode => OnPlayerJoined?.Invoke(roomCode));
        _hubConnection.On<ShotResultDto>("ReceiveShotResult", result => OnShotResultReceived?.Invoke(result));
        _hubConnection.On("OpponentDisconnected", () => OnOpponentDisconnected?.Invoke());
        _hubConnection.On<string>("GameStarted", firstTurnId => OnGameStarted?.Invoke(firstTurnId));
        await _hubConnection.StartAsync();
    }

   

    public async Task<string> CreateRoomAsync()
    {
        if (_hubConnection == null) throw new InvalidOperationException("З'єднання не встановлено.");
        return await _hubConnection.InvokeAsync<string>("CreateRoom");
    }

    public async Task<bool> JoinRoomAsync(string roomCode)
    {
        if (_hubConnection == null) throw new InvalidOperationException("З'єднання не встановлено.");
        return await _hubConnection.InvokeAsync<bool>("JoinRoom", roomCode);
    }

    public async Task SendShotAsync(string roomCode, Point target)
    {
        if (_hubConnection == null) return;
        var move = new PlayerMoveDto(roomCode, target);
        await _hubConnection.SendAsync("FireShot", move);
    }

    public async ValueTask DisposeAsync()
    {
        if (_hubConnection != null)
        {
            await _hubConnection.DisposeAsync();
        }
    }
    public async Task LeaveRoomAsync(string roomCode)
    {
        if (_hubConnection != null && _hubConnection.State == HubConnectionState.Connected)
        {
            await _hubConnection.SendAsync("LeaveRoom", roomCode);
        }
    }
    public async Task SetReadyAsync(string roomCode, List<Point> shipCoordinates)
    {
        if (_hubConnection == null) return;
        await _hubConnection.SendAsync("PlayerReady", roomCode, shipCoordinates);
    }
}