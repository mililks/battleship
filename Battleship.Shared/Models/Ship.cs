namespace Battleship.Shared.Models;

using Battleship.Shared.Enums;

public abstract class Ship
{
    public abstract string Name { get; }
    public abstract int Size { get; }

    public ShipOrientation Orientation { get; set; } = ShipOrientation.Horizontal;
    public List<Point> OccupiedPoints { get; protected set; } = new();
    public int HitsCount { get; private set; }

    public bool IsSunk => HitsCount >= Size;

    public void RegisterHit()
    {
        if (!IsSunk)
        {
            HitsCount++;
        }
    }

    public void AssignCoordinates(List<Point> points)
    {
        OccupiedPoints = points ?? throw new ArgumentNullException(nameof(points));
    }
}