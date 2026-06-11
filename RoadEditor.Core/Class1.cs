namespace RoadEditor.Core;

public class RoadMap
{
    private readonly string[,] tiles;

    public int Width { get; }
    public int Height { get; }

    public RoadMap(int width, int height)
    {
        if (width < 3)
            throw new ArgumentException("Ширина карты должна быть не меньше 3.", nameof(width));

        if (height < 3)
            throw new ArgumentException("Высота карты должна быть не меньше 3.", nameof(height));

        Width = width;
        Height = height;
        tiles = new string[height, width];

        Clear();
    }

    public string GetTile(int x, int y)
    {
        CheckCoordinates(x, y);
        return tiles[y, x];
    }

    public void SetTile(int x, int y, string tileType)
    {
        CheckCoordinates(x, y);

        if (string.IsNullOrWhiteSpace(tileType))
            throw new ArgumentException("Тип элемента не может быть пустым.", nameof(tileType));

        tiles[y, x] = tileType;
    }

    public void Fill(string tileType)
    {
        if (string.IsNullOrWhiteSpace(tileType))
            throw new ArgumentException("Тип элемента не может быть пустым.", nameof(tileType));

        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                tiles[y, x] = tileType;
            }
        }
    }

    public void Clear()
    {
        Fill("Empty");
    }

    private void CheckCoordinates(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
            throw new ArgumentOutOfRangeException("Координаты находятся за пределами карты.");
    }
}
