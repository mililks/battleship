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

    public bool TryPlaceShip(Ship ship, Point start, ShipOrientation orientation)
    {
        var prospectivePoints = new List<Point>();

        for (int i = 0; i < ship.Size; i++)
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

        ship.Orientation = orientation;
        ship.AssignCoordinates(prospectivePoints);

        foreach (var pt in prospectivePoints)
        {
            _grid[pt.X, pt.Y] = CellState.Ship;
        }

        _ships.Add(ship);
        return true;
    }

    public (bool IsHit, bool IsSunk, Ship? Ship) ProcessShot(Point target)
    {
        ValidateCoordinate(target.X, target.Y);

        if (_grid[target.X, target.Y] == CellState.Ship)
        {
            _grid[target.X, target.Y] = CellState.Hit;
            var ship = _ships.First(s => s.OccupiedPoints.Contains(target));
            ship.RegisterHit();
            return (true, ship.IsSunk, ship);
        }

        if (_grid[target.X, target.Y] == CellState.Empty)
        {
            _grid[target.X, target.Y] = CellState.Miss;
        }

        return (false, false, null);
    }

    public bool AreAllSunk() => _ships.Count > 0 && _ships.All(s => s.IsSunk);
    public void PlaceFleetRandomly()
    {
        _ships.Clear();
        for (int x = 0; x < Size; x++)
            for (int y = 0; y < Size; y++)
                _grid[x, y] = CellState.Empty;

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
        _ships.Clear();
        for (int x = 0; x < Size; x++)
            for (int y = 0; y < Size; y++)
                _grid[x, y] = CellState.Empty;

        foreach (var pt in points)
        {
            _grid[pt.X, pt.Y] = CellState.Ship;
            var ship = new Destroyer();
            ship.AssignCoordinates(new List<Point> { pt });
            _ships.Add(ship);
        }
    }
}