namespace Battleship.Shared.DTOs;

using Battleship.Shared.Models;

public record PlayerMoveDto(string RoomCode, Point Target);