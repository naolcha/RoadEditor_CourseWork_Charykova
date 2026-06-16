using RoadEditor.Core;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace RoadEditor.App;

public partial class MainWindow : Window
{
    private const int CellSize = 80;
    private const int PaletteCellSize = 72;
    private const int PaletteColumns = 4;

    private RoadMap roadMap = new(12, 7);
    private string selectedTile = nameof(RoadTileType.RoadHorizontal);

    private readonly Stack<MapSnapshot> undoHistory = new();
    private readonly Stack<MapSnapshot> redoHistory = new();

    private static readonly string[] Palette =
    {
        nameof(RoadTileType.Empty),
        nameof(RoadTileType.Pavement),
        nameof(RoadTileType.RoadHorizontal),
        nameof(RoadTileType.RoadVertical),
        nameof(RoadTileType.CornerRightDown),
        nameof(RoadTileType.CornerLeftDown),
        nameof(RoadTileType.CornerRightUp),
        nameof(RoadTileType.CornerLeftUp),
        nameof(RoadTileType.TUp),
        nameof(RoadTileType.TDown),
        nameof(RoadTileType.TLeft),
        nameof(RoadTileType.TRight),
        nameof(RoadTileType.Cross)
    };

    private static readonly IReadOnlyDictionary<string, string> TileNames =
        new Dictionary<string, string>
        {
            [nameof(RoadTileType.Empty)] = "Пустая ячейка",
            [nameof(RoadTileType.Pavement)] = "Тротуарная плитка",
            [nameof(RoadTileType.RoadHorizontal)] = "Горизонтальная дорога",
            [nameof(RoadTileType.RoadVertical)] = "Вертикальная дорога",
            [nameof(RoadTileType.CornerRightDown)] = "Поворот вправо и вниз",
            [nameof(RoadTileType.CornerLeftDown)] = "Поворот влево и вниз",
            [nameof(RoadTileType.CornerRightUp)] = "Поворот вправо и вверх",
            [nameof(RoadTileType.CornerLeftUp)] = "Поворот влево и вверх",
            [nameof(RoadTileType.TUp)] = "Развилка вверх",
            [nameof(RoadTileType.TDown)] = "Развилка вниз",
            [nameof(RoadTileType.TLeft)] = "Развилка влево",
            [nameof(RoadTileType.TRight)] = "Развилка вправо",
            [nameof(RoadTileType.Cross)] = "Перекресток"
        };

    private static readonly IReadOnlyDictionary<string, TileDefinition> TileDefinitions =
        new Dictionary<string, TileDefinition>
        {
            [nameof(RoadTileType.RoadHorizontal)] = new(true, true, false, false),
            [nameof(RoadTileType.RoadVertical)] = new(false, false, true, true),
            [nameof(RoadTileType.CornerRightDown)] = new(false, true, false, true),
            [nameof(RoadTileType.CornerLeftDown)] = new(true, false, false, true),
            [nameof(RoadTileType.CornerRightUp)] = new(false, true, true, false),
            [nameof(RoadTileType.CornerLeftUp)] = new(true, false, true, false),
            [nameof(RoadTileType.TUp)] = new(true, true, true, false, Crosswalk.Top),
            [nameof(RoadTileType.TDown)] = new(true, true, false, true, Crosswalk.Bottom),
            [nameof(RoadTileType.TLeft)] = new(true, false, true, true, Crosswalk.Left),
            [nameof(RoadTileType.TRight)] = new(false, true, true, true, Crosswalk.Right),
            [nameof(RoadTileType.Cross)] = new(
                true,
                true,
                true,
                true,
                Crosswalk.Top | Crosswalk.Bottom | Crosswalk.Left | Crosswalk.Right)
        };

    public MainWindow()
    {
        InitializeComponent();
        BuildPalette();
        DrawMap();
        DrawSelectedPreview();
        UpdateSelectedTileName();
    }

    private void ClearMap_Click(object sender, RoutedEventArgs e)
    {
        SaveStateForUndo();
        roadMap.Clear();
        DrawMap();
        Focus();
    }

    private void FillMap_Click(object sender, RoutedEventArgs e)
    {
        SaveStateForUndo();
        roadMap.Fill(selectedTile);
        DrawMap();
        Focus();
    }

    private void BuildPalette()
    {
        PaletteCanvas.Children.Clear();

        for (int index = 0; index < Palette.Length; index++)
        {
            double left = index % PaletteColumns * PaletteCellSize;
            double top = index / PaletteColumns * PaletteCellSize;
            string tile = Palette[index];

            DrawCellBackground(PaletteCanvas, left, top, PaletteCellSize);
            DrawTile(PaletteCanvas, tile, left, top, PaletteCellSize);

            AddRectangle(
                PaletteCanvas,
                left,
                top,
                PaletteCellSize,
                PaletteCellSize,
                Brushes.Transparent,
                new SolidColorBrush(
                    tile == selectedTile
                        ? Color.FromRgb(0, 174, 255)
                        : Color.FromRgb(95, 95, 100)),
                tile == selectedTile ? 3 : 1.2);
        }
    }

    private void PaletteCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        Point position = e.GetPosition(PaletteCanvas);
        int index = (int)(position.Y / PaletteCellSize) * PaletteColumns +
                    (int)(position.X / PaletteCellSize);

        if ((uint)index >= (uint)Palette.Length)
            return;

        selectedTile = Palette[index];
        BuildPalette();
        DrawSelectedPreview();
        UpdateSelectedTileName();
        Focus();
    }

    private void DrawSelectedPreview()
    {
        SelectedTilePreview.Children.Clear();
        DrawCellBackground(SelectedTilePreview, 0, 0, 120);
        DrawTile(SelectedTilePreview, selectedTile, 0, 0, 120);
    }

    private void UpdateSelectedTileName()
    {
        SelectedTileNameText.Text =
            TileNames.TryGetValue(selectedTile, out string? name)
                ? name
                : "Дорожный элемент";
    }

    private void MapCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        Point point = e.GetPosition(MapCanvas);
        int x = (int)(point.X / CellSize);
        int y = (int)(point.Y / CellSize);

        if (x < 0 || y < 0 || x >= roadMap.Width || y >= roadMap.Height ||
            roadMap.GetTile(x, y) == selectedTile)
        {
            return;
        }

        SaveStateForUndo();
        roadMap.SetTile(x, y, selectedTile);
        DrawMap();
        Focus();
    }

    private void MapCanvas_MouseMove(object sender, MouseEventArgs e)
    {
        Point point = e.GetPosition(MapCanvas);
        int x = (int)(point.X / CellSize);
        int y = (int)(point.Y / CellSize);

        if (x >= 0 && y >= 0 && x < roadMap.Width && y < roadMap.Height)
            CellInfoText.Text = $"Номер ячейки: {x}, {y}";
    }

    private void DrawMap()
    {
        MapCanvas.Children.Clear();
        MapCanvas.Width = roadMap.Width * CellSize;
        MapCanvas.Height = roadMap.Height * CellSize;

        for (int y = 0; y < roadMap.Height; y++)
        {
            for (int x = 0; x < roadMap.Width; x++)
            {
                double left = x * CellSize;
                double top = y * CellSize;
                DrawCellBackground(MapCanvas, left, top, CellSize);
                DrawTile(MapCanvas, roadMap.GetTile(x, y), left, top, CellSize);
            }
        }
    }

    private static void DrawCellBackground(Canvas canvas, double x, double y, double size)
    {
        AddRectangle(
            canvas,
            x,
            y,
            size,
            size,
            new SolidColorBrush(Color.FromRgb(21, 21, 28)),
            new SolidColorBrush(Color.FromRgb(78, 78, 82)),
            1.2);
    }

    private void DrawTile(Canvas canvas, string tile, double x, double y, double size)
    {
        if (tile == nameof(RoadTileType.Empty))
            return;

        DrawPavement(canvas, x, y, size);

        if (tile == nameof(RoadTileType.Pavement) ||
            !TileDefinitions.TryGetValue(tile, out TileDefinition definition))
        {
            return;
        }

        DrawRoad(
            canvas,
            x,
            y,
            size,
            definition.Left,
            definition.Right,
            definition.Up,
            definition.Down);

        DrawCrosswalks(canvas, x, y, size, definition.Crosswalks);
    }

    private static void DrawPavement(Canvas canvas, double x, double y, double size)
    {
        AddRectangle(
            canvas,
            x,
            y,
            size,
            size,
            new SolidColorBrush(Color.FromRgb(137, 138, 136)));

        DrawPavementBricks(canvas, x, y, size);
        DrawPavementWear(canvas, x, y, size);
    }

    private static void DrawPavementBricks(Canvas canvas, double x, double y, double size)
    {
        const int rows = 6;
        double rowHeight = size / rows;
        double brickWidth = size / 3;
        Brush mortar = new SolidColorBrush(Color.FromRgb(92, 93, 91));
        double thickness = Math.Max(0.6, size / 120);

        for (int row = 1; row < rows; row++)
            AddLine(canvas, x, y + row * rowHeight, x + size, y + row * rowHeight, mortar, thickness, 0.8);

        for (int row = 0; row < rows; row++)
        {
            double rowTop = y + row * rowHeight;
            double offset = row % 2 == 0 ? 0 : brickWidth / 2;

            for (double position = offset; position < size; position += brickWidth)
            {
                if (position > 0 && position < size)
                {
                    AddLine(
                        canvas,
                        x + position,
                        rowTop,
                        x + position,
                        rowTop + rowHeight,
                        mortar,
                        thickness,
                        0.8);
                }
            }
        }
    }

    private static void DrawPavementWear(Canvas canvas, double x, double y, double size)
    {
        int count = Math.Max(8, (int)(size / 7));
        int area = Math.Max(1, (int)size - 8);
        Brush dark = new SolidColorBrush(Color.FromRgb(78, 79, 77));
        Brush light = new SolidColorBrush(Color.FromRgb(190, 190, 185));

        for (int index = 0; index < count; index++)
        {
            AddEllipse(
                canvas,
                x + 4 + PositiveModulo(index * 29 + (int)x * 3 + (int)y * 5, area),
                y + 4 + PositiveModulo(index * 17 + (int)x * 7 + (int)y * 2, area),
                1.2 + index % 3,
                0.8 + index % 2,
                index % 2 == 0 ? dark : light,
                0.22);
        }

        for (int index = 0; index < 3; index++)
        {
            double startX = x + size * (0.18 + index * 0.24);
            double startY = y + size * (0.22 + index * 0.18);
            AddLine(
                canvas,
                startX,
                startY,
                startX + size * 0.09,
                startY + size * 0.025,
                dark,
                Math.Max(0.5, size / 150),
                0.25);
        }
    }

    private static void DrawRoad(
        Canvas canvas,
        double x,
        double y,
        double size,
        bool left,
        bool right,
        bool up,
        bool down)
    {
        double width = size * 0.55;
        double center = size / 2;
        double start = center - width / 2;

        DrawAsphalt(canvas, x + start, y + start, width, width);
        if (left) DrawAsphalt(canvas, x, y + start, center, width);
        if (right) DrawAsphalt(canvas, x + center, y + start, center, width);
        if (up) DrawAsphalt(canvas, x + start, y, width, center);
        if (down) DrawAsphalt(canvas, x + start, y + center, width, center);

        DrawRoadMarkings(canvas, x, y, size, left, right, up, down);
    }

    private static void DrawAsphalt(Canvas canvas, double x, double y, double width, double height)
    {
        AddRectangle(
            canvas,
            x,
            y,
            width,
            height,
            new SolidColorBrush(Color.FromRgb(31, 32, 34)));

        DrawAsphaltGrain(canvas, x, y, width, height);
        DrawAsphaltCracks(canvas, x, y, width, height);
    }

    private static void DrawAsphaltGrain(Canvas canvas, double x, double y, double width, double height)
    {
        int count = Math.Max(8, (int)(width * height / 190));
        int availableWidth = Math.Max(1, (int)width - 4);
        int availableHeight = Math.Max(1, (int)height - 4);
        Brush light = new SolidColorBrush(Color.FromRgb(88, 89, 91));
        Brush dark = new SolidColorBrush(Color.FromRgb(8, 9, 10));

        for (int index = 0; index < count; index++)
        {
            double grainSize = 0.8 + index % 3 * 0.45;
            AddEllipse(
                canvas,
                x + 2 + PositiveModulo(index * 19 + (int)x * 5 + (int)y * 3, availableWidth),
                y + 2 + PositiveModulo(index * 31 + (int)x * 2 + (int)y * 7, availableHeight),
                grainSize,
                grainSize,
                index % 3 == 0 ? light : dark,
                index % 3 == 0 ? 0.35 : 0.25);
        }
    }

    private static void DrawAsphaltCracks(Canvas canvas, double x, double y, double width, double height)
    {
        if (width < 22 || height < 22)
            return;

        Brush brush = new SolidColorBrush(Color.FromRgb(5, 5, 6));
        double firstX = x + width * 0.28;
        double firstY = y + height * 0.32;
        double secondX = x + width * 0.7;
        double secondY = y + height * 0.62;

        AddPolyline(
            canvas,
            new Point(firstX, firstY),
            new Point(firstX + width * 0.08, firstY + height * 0.06),
            new Point(firstX + width * 0.05, firstY + height * 0.14),
            new Point(firstX + width * 0.13, firstY + height * 0.2),
            brush,
            Math.Max(0.45, width / 120),
            0.42);

        AddPolyline(
            canvas,
            new Point(secondX, secondY),
            new Point(secondX - width * 0.07, secondY + height * 0.04),
            new Point(secondX - width * 0.03, secondY + height * 0.11),
            brush,
            Math.Max(0.4, width / 130),
            0.32);
    }

    private static void DrawCrosswalks(
        Canvas canvas,
        double x,
        double y,
        double size,
        Crosswalk crosswalks)
    {
        foreach (Crosswalk side in new[]
                 {
                     Crosswalk.Top,
                     Crosswalk.Bottom,
                     Crosswalk.Left,
                     Crosswalk.Right
                 })
        {
            if (crosswalks.HasFlag(side))
                DrawCrosswalk(canvas, x, y, size, side);
        }
    }

    private static void DrawCrosswalk(Canvas canvas, double x, double y, double size, Crosswalk side)
    {
        double roadWidth = size * 0.55;
        bool verticalStripes = side is Crosswalk.Top or Crosswalk.Bottom;
        double fixedPosition = side switch
        {
            Crosswalk.Top or Crosswalk.Left => size * 0.055,
            _ => size * 0.775
        };
        double changingPosition = size / 2 - roadWidth / 2 + roadWidth * 0.15;
        double step = roadWidth * 0.13;

        for (int index = 0; index < 6; index++)
        {
            double left = verticalStripes ? x + changingPosition + index * step : x + fixedPosition;
            double top = verticalStripes ? y + fixedPosition : y + changingPosition + index * step;

            AddRectangle(
                canvas,
                left,
                top,
                verticalStripes ? size * 0.022 : size * 0.17,
                verticalStripes ? size * 0.17 : size * 0.022,
                Brushes.White,
                opacity: 0.88);
        }
    }

    private static void DrawRoadMarkings(
        Canvas canvas,
        double x,
        double y,
        double size,
        bool left,
        bool right,
        bool up,
        bool down)
    {
        double centerX = x + size / 2;
        double centerY = y + size / 2;
        double length = size * 0.14;
        double thickness = size * 0.02;

        if (left) DrawMark(canvas, x + size * 0.35, centerY, length, thickness, true);
        if (right) DrawMark(canvas, x + size * 0.65, centerY, length, thickness, true);
        if (up) DrawMark(canvas, centerX, y + size * 0.35, length, thickness, false);
        if (down) DrawMark(canvas, centerX, y + size * 0.65, length, thickness, false);
    }

    private static void DrawMark(
        Canvas canvas,
        double centerX,
        double centerY,
        double length,
        double thickness,
        bool horizontal)
    {
        double width = horizontal ? length : thickness;
        double height = horizontal ? thickness : length;
        AddRectangle(
            canvas,
            centerX - width / 2,
            centerY - height / 2,
            width,
            height,
            new SolidColorBrush(Color.FromRgb(235, 235, 225)),
            opacity: 0.86);
    }

    private static void AddRectangle(
        Canvas canvas,
        double x,
        double y,
        double width,
        double height,
        Brush fill,
        Brush? stroke = null,
        double strokeThickness = 0,
        double opacity = 1)
    {
        var rectangle = new Rectangle
        {
            Width = width,
            Height = height,
            Fill = fill,
            Stroke = stroke,
            StrokeThickness = strokeThickness,
            Opacity = opacity,
            IsHitTestVisible = false
        };
        Canvas.SetLeft(rectangle, x);
        Canvas.SetTop(rectangle, y);
        canvas.Children.Add(rectangle);
    }

    private static void AddEllipse(
        Canvas canvas,
        double x,
        double y,
        double width,
        double height,
        Brush fill,
        double opacity)
    {
        var ellipse = new Ellipse
        {
            Width = width,
            Height = height,
            Fill = fill,
            Opacity = opacity,
            IsHitTestVisible = false
        };
        Canvas.SetLeft(ellipse, x);
        Canvas.SetTop(ellipse, y);
        canvas.Children.Add(ellipse);
    }

    private static void AddLine(
        Canvas canvas,
        double x1,
        double y1,
        double x2,
        double y2,
        Brush stroke,
        double thickness,
        double opacity)
    {
        canvas.Children.Add(
            new Line
            {
                X1 = x1,
                Y1 = y1,
                X2 = x2,
                Y2 = y2,
                Stroke = stroke,
                StrokeThickness = thickness,
                Opacity = opacity,
                IsHitTestVisible = false
            });
    }

    private static void AddPolyline(
        Canvas canvas,
        Point point1,
        Point point2,
        Point point3,
        Brush stroke,
        double thickness,
        double opacity)
    {
        AddPolyline(canvas, new[] { point1, point2, point3 }, stroke, thickness, opacity);
    }

    private static void AddPolyline(
        Canvas canvas,
        Point point1,
        Point point2,
        Point point3,
        Point point4,
        Brush stroke,
        double thickness,
        double opacity)
    {
        AddPolyline(canvas, new[] { point1, point2, point3, point4 }, stroke, thickness, opacity);
    }

    private static void AddPolyline(
        Canvas canvas,
        IEnumerable<Point> points,
        Brush stroke,
        double thickness,
        double opacity)
    {
        canvas.Children.Add(
            new Polyline
            {
                Points = new PointCollection(points),
                Stroke = stroke,
                StrokeThickness = thickness,
                Opacity = opacity,
                IsHitTestVisible = false
            });
    }

    private static int PositiveModulo(int value, int modulus)
    {
        if (modulus <= 0)
            return 0;

        int result = value % modulus;
        return result < 0 ? result + modulus : result;
    }

    private void SaveStateForUndo()
    {
        undoHistory.Push(CreateSnapshot());
        redoHistory.Clear();
    }

    private MapSnapshot CreateSnapshot() =>
        new(roadMap.Width, roadMap.Height, roadMap.ToArray());

    private void RestoreSnapshot(MapSnapshot snapshot)
    {
        roadMap = new RoadMap(snapshot.Width, snapshot.Height);

        for (int y = 0; y < snapshot.Height; y++)
            for (int x = 0; x < snapshot.Width; x++)
                roadMap.SetTile(x, y, snapshot.Tiles[y][x]);

        MapWidthTextBox.Text = snapshot.Width.ToString();
        MapHeightTextBox.Text = snapshot.Height.ToString();
        DrawMap();
        Focus();
    }

    private void Undo()
    {
        if (undoHistory.Count == 0)
            return;

        redoHistory.Push(CreateSnapshot());
        RestoreSnapshot(undoHistory.Pop());
    }

    private void Redo()
    {
        if (redoHistory.Count == 0)
            return;

        undoHistory.Push(CreateSnapshot());
        RestoreSnapshot(redoHistory.Pop());
    }

    private string[][] GetTilesArray() => roadMap.ToArray();

    private void Help_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
            "Нажмите на дорожный элемент в палитре.\n" +
            "После этого нажмите на нужную ячейку карты.\n\n" +
            "Сохранить - сохранить карту в файл.\n" +
            "Загрузить - открыть сохраненную карту.\n" +
            "Экспорт PNG - сохранить карту как изображение.\n" +
            "Ctrl+Z - отменить последнее действие.\n" +
            "Ctrl+Y - повторить отмененное действие.\n" +
            "F1 - открыть справку.",
            "Справка",
            MessageBoxButton.OK,
            MessageBoxImage.Information);

        Focus();
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F1)
        {
            Help_Click(sender, e);
            e.Handled = true;
        }
        else if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control) && e.Key == Key.Z)
        {
            Undo();
            e.Handled = true;
        }
        else if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control) && e.Key == Key.Y)
        {
            Redo();
            e.Handled = true;
        }
    }

    [Flags]
    private enum Crosswalk
    {
        None = 0,
        Top = 1,
        Bottom = 2,
        Left = 4,
        Right = 8
    }

    private readonly record struct TileDefinition(
        bool Left,
        bool Right,
        bool Up,
        bool Down,
        Crosswalk Crosswalks = Crosswalk.None);

    private sealed record MapSnapshot(int Width, int Height, string[][] Tiles);
}
