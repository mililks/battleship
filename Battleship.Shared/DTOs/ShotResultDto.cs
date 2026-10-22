namespace Battleship.Shared.DTOs;

using Battleship.Shared.Models;

public record ShotResultDto(
    Point Target,
    bool IsHit,
    bool IsSunk,
    string? SunkShipName,
    List<Point>? SunkShipCoordinates,
    bool IsGameOver,
    string NextPlayerTurnId
);
