namespace Battleship.Shared.Models;

public record Point(int X, int Y)
{
    public bool IsValid() => X >= 0 && X < 10 && Y >= 0 && Y < 10;
}