namespace Battleship.Shared.Models;

using Battleship.Shared.Enums;
using Battleship.Shared.Models.Ships;

public class Board
{
    public const int Size = 10;
    private readonly CellState[,] _grid = new CellState[Size, Size];
    private readonly List<Ship> _ships = new();

    public IReadOnlyList<Ship> Ships => _ships.AsReadOnly();

    public CellState GetCellState(int x, int y)
    {
        ValidateCoordinate(x, y);
        return _grid[x, y];
    }

    public List<Point> GetAllOccupiedPoints()
    {
        return _ships.SelectMany(s => s.OccupiedPoints).ToList();
    }

    public bool CanPlaceShip(int size, Point start, ShipOrientation orientation, out List<Point> prospectivePoints)
    {
        prospectivePoints = new List<Point>();

        for (int i = 0; i < size; i++)
        {
            int cx = orientation == ShipOrientation.Horizontal ? start.X + i : start.X;
            int cy = orientation == ShipOrientation.Vertical ? start.Y + i : start.Y;
            var pt = new Point(cx, cy);

            if (!pt.IsValid()) return false;
            prospectivePoints.Add(pt);
        }

        foreach (var pt in prospectivePoints)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    int checkX = pt.X + dx;
                    int checkY = pt.Y + dy;
                    if (checkX >= 0 && checkX < Size && checkY >= 0 && checkY < Size)
                    {
                        if (_grid[checkX, checkY] == CellState.Ship)
                            return false;
                    }
                }
            }
        }

        return true;
    }

    public bool TryPlaceShip(Ship ship, Point start, ShipOrientation orientation)
    {
        if (!CanPlaceShip(ship.Size, start, orientation, out var prospectivePoints))
            return false;

        ship.Orientation = orientation;
        ship.AssignCoordinates(prospectivePoints);

        foreach (var pt in prospectivePoints)
        {
            _grid[pt.X, pt.Y] = CellState.Ship;
        }

        _ships.Add(ship);
        return true;
    }

    public Ship? RemoveShipAt(Point pt)
    {
        var ship = _ships.FirstOrDefault(s => s.OccupiedPoints.Contains(pt));
        if (ship == null) return null;

        _ships.Remove(ship);
        foreach (var p in ship.OccupiedPoints)
        {
            _grid[p.X, p.Y] = CellState.Empty;
        }
        return ship;
    }

    public void Clear()
    {
        _ships.Clear();
        for (int x = 0; x < Size; x++)
            for (int y = 0; y < Size; y++)
                _grid[x, y] = CellState.Empty;
    }

    public bool IsFleetComplete() => _ships.Count == 10;

    public static Ship CreateShipBySize(int size) => size switch
    {
        4 => new Carrier(),
        3 => new Battleship(),
        2 => new Cruiser(),
        _ => new Destroyer()
    };

    public (bool IsHit, bool IsSunk, Ship? Ship) ProcessShot(Point target)
    {
        ValidateCoordinate(target.X, target.Y);

        if (_grid[target.X, target.Y] == CellState.Ship)
        {
            _grid[target.X, target.Y] = CellState.Hit;
            var ship = _ships.First(s => s.OccupiedPoints.Contains(target));
            ship.RegisterHit();

            if (ship.IsSunk)
            {
                MarkSurroundingAsMiss(ship.OccupiedPoints);
            }

            return (true, ship.IsSunk, ship);
        }

        if (_grid[target.X, target.Y] == CellState.Empty)
        {
            _grid[target.X, target.Y] = CellState.Miss;
        }

        return (false, false, null);
    }

    public static List<Point> GetSurroundingCoordinates(IEnumerable<Point> shipCoordinates)
    {
        var shipSet = new HashSet<Point>(shipCoordinates);
        var surrounding = new HashSet<Point>();

        foreach (var pt in shipSet)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    var neighbor = new Point(pt.X + dx, pt.Y + dy);
                    if (neighbor.IsValid() && !shipSet.Contains(neighbor))
                    {
                        surrounding.Add(neighbor);
                    }
                }
            }
        }

        return surrounding.ToList();
    }

    public void MarkSurroundingAsMiss(IEnumerable<Point> shipCoordinates)
    {
        var surrounding = GetSurroundingCoordinates(shipCoordinates);
        foreach (var pt in surrounding)
        {
            if (_grid[pt.X, pt.Y] == CellState.Empty)
            {
                _grid[pt.X, pt.Y] = CellState.Miss;
            }
        }
    }

    public bool AreAllSunk() => _ships.Count > 0 && _ships.All(s => s.IsSunk);

    public void PlaceFleetRandomly()
    {
        Clear();

        var fleetToPlace = new List<Ship>
        {
            new Carrier(),
            new Battleship(),
            new Battleship(),
            new Cruiser(),
            new Cruiser(),
            new Cruiser(),
            new Destroyer(),
            new Destroyer(),
            new Destroyer(),
            new Destroyer()
        };

        var random = new Random();

        foreach (var ship in fleetToPlace)
        {
            bool placed = false;
            while (!placed)
            {
                int x = random.Next(0, Size);
                int y = random.Next(0, Size);
                var orientation = random.Next(0, 2) == 0 ? ShipOrientation.Horizontal : ShipOrientation.Vertical;

                placed = TryPlaceShip(ship, new Point(x, y), orientation);
            }
        }
    }

    private static void ValidateCoordinate(int x, int y)
    {
        if (x < 0 || x >= Size || y < 0 || y >= Size)
            throw new ArgumentOutOfRangeException("Координата виходить за межі сітки 10x10.");
    }

    public void SetShipsFromPoints(List<Point> points)
    {
        Clear();

        var remaining = new HashSet<Point>(points);

        while (remaining.Count > 0)
        {
            var first = remaining.First();
            remaining.Remove(first);

            var shipPoints = new List<Point> { first };
            var queue = new Queue<Point>();
            queue.Enqueue(first);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                var neighbors = remaining.Where(p =>
                    (Math.Abs(p.X - current.X) == 1 && p.Y == current.Y) ||
                    (Math.Abs(p.Y - current.Y) == 1 && p.X == current.X)
                ).ToList();

                foreach (var n in neighbors)
                {
                    remaining.Remove(n);
                    shipPoints.Add(n);
                    queue.Enqueue(n);
                }
            }

            shipPoints = shipPoints.OrderBy(p => p.X).ThenBy(p => p.Y).ToList();

            Ship ship = CreateShipBySize(shipPoints.Count);

            if (shipPoints.Count > 1)
            {
                ship.Orientation = shipPoints[0].X != shipPoints[1].X
                    ? ShipOrientation.Horizontal
                    : ShipOrientation.Vertical;
            }

            ship.AssignCoordinates(shipPoints);
            _ships.Add(ship);

            foreach (var pt in shipPoints)
            {
                _grid[pt.X, pt.Y] = CellState.Ship;
            }
        }
    }
}
