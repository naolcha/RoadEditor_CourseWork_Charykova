using System;
using System.Collections;
using System.Collections.Generic;

namespace RoadEditor.Core;

public enum RoadTileType
{
    Empty,
    Pavement,
    RoadHorizontal,
    RoadVertical,
    CornerRightDown,
    CornerLeftDown,
    CornerRightUp,
    CornerLeftUp,
    TUp,
    TDown,
    TLeft,
    TRight,
    Cross
}

public enum RoadMapChangeKind
{
    TileChanged,
    MapFilled,
    MapCleared
}

public interface IRoadMap : IEnumerable<RoadTile>
{
    int Width { get; }

    int Height { get; }

    RoadTile this[int x, int y] { get; }

    event EventHandler<RoadMapChangedEventArgs>? MapChanged;

    string GetTile(int x, int y);

    RoadTileType GetTileType(int x, int y);

    void SetTile(int x, int y, string tileName);

    void SetTile(int x, int y, RoadTileType tileType);

    void Fill(string tileName);

    void Fill(RoadTileType tileType);

    void Clear();
}

public abstract class MapElement
{
    protected MapElement(int x, int y)
    {
        X = x;
        Y = y;
    }

    public int X { get; }

    public int Y { get; }
}

public sealed class RoadTile : MapElement
{
    public RoadTile(
        int x,
        int y,
        RoadTileType type = RoadTileType.Empty)
        : base(x, y)
    {
        Type = type;
    }

    public RoadTileType Type { get; private set; }

    public string Name => Type.ToString();

    public bool IsEmpty => Type == RoadTileType.Empty;

    internal bool ChangeType(RoadTileType newType)
    {
        if (Type == newType)
            return false;

        Type = newType;
        return true;
    }

    public override string ToString()
    {
        return $"{X}, {Y}: {Name}";
    }
}

public sealed class RoadMapChangedEventArgs : EventArgs
{
    public RoadMapChangedEventArgs(
        RoadMapChangeKind changeKind,
        RoadTileType tileType,
        int? x = null,
        int? y = null)
    {
        ChangeKind = changeKind;
        TileType = tileType;
        X = x;
        Y = y;
    }

    public RoadMapChangeKind ChangeKind { get; }

    public RoadTileType TileType { get; }

    public int? X { get; }

    public int? Y { get; }
}

public sealed class RoadMap : IRoadMap
{
    private readonly RoadTile[,] tiles;

    public RoadMap(int width, int height)
    {
        if (width < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(width),
                "Ширина карты должна быть больше нуля.");
        }

        if (height < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(height),
                "Высота карты должна быть больше нуля.");
        }

        Width = width;
        Height = height;

        tiles = new RoadTile[height, width];

        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                tiles[y, x] = new RoadTile(
                    x,
                    y,
                    RoadTileType.Empty);
            }
        }
    }

    public int Width { get; }

    public int Height { get; }

    public int TileCount => Width * Height;

    public RoadTile this[int x, int y]
    {
        get
        {
            ValidateCoordinates(x, y);
            return tiles[y, x];
        }
    }

    public event EventHandler<RoadMapChangedEventArgs>? MapChanged;

    public string GetTile(int x, int y)
    {
        return GetTileType(x, y).ToString();
    }

    public RoadTileType GetTileType(int x, int y)
    {
        ValidateCoordinates(x, y);
        return tiles[y, x].Type;
    }

    public void SetTile(
        int x,
        int y,
        string tileName)
    {
        RoadTileType tileType =
            ParseTileType(tileName);

        SetTile(x, y, tileType);
    }

    public void SetTile(
        int x,
        int y,
        RoadTileType tileType)
    {
        ValidateCoordinates(x, y);

        RoadTile tile = tiles[y, x];

        if (!tile.ChangeType(tileType))
            return;

        OnMapChanged(
            new RoadMapChangedEventArgs(
                RoadMapChangeKind.TileChanged,
                tileType,
                x,
                y));
    }

    public void Fill(string tileName)
    {
        RoadTileType tileType =
            ParseTileType(tileName);

        Fill(tileType);
    }

    public void Fill(RoadTileType tileType)
    {
        bool mapChanged = false;

        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                if (tiles[y, x].ChangeType(tileType))
                {
                    mapChanged = true;
                }
            }
        }

        if (!mapChanged)
            return;

        OnMapChanged(
            new RoadMapChangedEventArgs(
                RoadMapChangeKind.MapFilled,
                tileType));
    }

    public void Clear()
    {
        bool mapChanged = false;

        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                if (tiles[y, x].ChangeType(
                    RoadTileType.Empty))
                {
                    mapChanged = true;
                }
            }
        }

        if (!mapChanged)
            return;

        OnMapChanged(
            new RoadMapChangedEventArgs(
                RoadMapChangeKind.MapCleared,
                RoadTileType.Empty));
    }

    public RoadMap Clone()
    {
        var copy = new RoadMap(
            Width,
            Height);

        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                copy.tiles[y, x].ChangeType(
                    tiles[y, x].Type);
            }
        }

        return copy;
    }

    public string[][] ToArray()
    {
        var result = new string[Height][];

        for (int y = 0; y < Height; y++)
        {
            result[y] = new string[Width];

            for (int x = 0; x < Width; x++)
            {
                result[y][x] =
                    tiles[y, x].Name;
            }
        }

        return result;
    }

    public IEnumerator<RoadTile> GetEnumerator()
    {
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                yield return tiles[y, x];
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    private static RoadTileType ParseTileType(
        string tileName)
    {
        if (string.IsNullOrWhiteSpace(tileName))
        {
            return RoadTileType.Empty;
        }

        if (Enum.TryParse(
                tileName,
                ignoreCase: true,
                out RoadTileType tileType))
        {
            return tileType;
        }

        throw new ArgumentException(
            $"Неизвестный тип дорожного элемента: {tileName}",
            nameof(tileName));
    }

    private void ValidateCoordinates(
        int x,
        int y)
    {
        if (x < 0 || x >= Width)
        {
            throw new ArgumentOutOfRangeException(
                nameof(x),
                $"Координата X должна находиться в диапазоне от 0 до {Width - 1}.");
        }

        if (y < 0 || y >= Height)
        {
            throw new ArgumentOutOfRangeException(
                nameof(y),
                $"Координата Y должна находиться в диапазоне от 0 до {Height - 1}.");
        }
    }

    private void OnMapChanged(
        RoadMapChangedEventArgs eventArgs)
    {
        MapChanged?.Invoke(
            this,
            eventArgs);
    }
}
