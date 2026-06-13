using Microsoft.Win32;
using RoadEditor.Core;
using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace RoadEditor.App;

public partial class MainWindow : Window
{
    private const int CellSize = 80;
    private const int PaletteCellSize = 72;
    private const int PaletteColumns = 4;

    private RoadMap roadMap = new(12, 7);
    private string selectedTile = "RoadHorizontal";

    private readonly string[] palette =
    {
        "Empty",
        "RoadHorizontal",
        "RoadVertical",
        "CornerRightDown",
        "CornerLeftDown",
        "CornerRightUp",
        "CornerLeftUp",
        "TUp",
        "TDown",
        "TLeft",
        "TRight",
        "Cross"
    };

    public MainWindow()
    {
        InitializeComponent();

        Loaded += (_, _) => Focus();

        BuildPalette();
        DrawMap();
        DrawSelectedPreview();
        UpdateSelectedTileName();
    }

    private void CreateMap_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(MapWidthTextBox.Text, out int width) ||
            !int.TryParse(MapHeightTextBox.Text, out int height))
        {
            MessageBox.Show(
                "Введите корректные размеры карты.",
                "Ошибка",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        if (width < 3 || height < 3 || width > 50 || height > 50)
        {
            MessageBox.Show(
                "Размер карты должен быть от 3 до 50 клеток.",
                "Ошибка",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        roadMap = new RoadMap(width, height);

        DrawMap();
        Focus();
    }

    private void ClearMap_Click(object sender, RoutedEventArgs e)
    {
        roadMap.Clear();

        DrawMap();
        Focus();
    }

    private void FillMap_Click(object sender, RoutedEventArgs e)
    {
        roadMap.Fill(selectedTile);

        DrawMap();
        Focus();
    }

    private void BuildPalette()
    {
        PaletteCanvas.Children.Clear();

        for (int index = 0; index < palette.Length; index++)
        {
            int column = index % PaletteColumns;
            int row = index / PaletteColumns;

            double left = column * PaletteCellSize;
            double top = row * PaletteCellSize;

            DrawCellBackground(
                PaletteCanvas,
                left,
                top,
                PaletteCellSize);

            DrawTile(
                PaletteCanvas,
                palette[index],
                left,
                top,
                PaletteCellSize);

            var border = new Rectangle
            {
                Width = PaletteCellSize,
                Height = PaletteCellSize,
                Fill = Brushes.Transparent,
                Stroke = new SolidColorBrush(
                    palette[index] == selectedTile
                        ? Color.FromRgb(0, 174, 255)
                        : Color.FromRgb(95, 95, 100)),
                StrokeThickness = palette[index] == selectedTile ? 3 : 1.2,
                IsHitTestVisible = false
            };

            Canvas.SetLeft(border, left);
            Canvas.SetTop(border, top);

            PaletteCanvas.Children.Add(border);
        }
    }

    private void PaletteCanvas_MouseLeftButtonDown(
        object sender,
        MouseButtonEventArgs e)
    {
        Point position = e.GetPosition(PaletteCanvas);

        int column = (int)(position.X / PaletteCellSize);
        int row = (int)(position.Y / PaletteCellSize);
        int index = row * PaletteColumns + column;

        if (index < 0 || index >= palette.Length)
            return;

        selectedTile = palette[index];

        BuildPalette();
        DrawSelectedPreview();
        UpdateSelectedTileName();

        Focus();
    }

    private void DrawSelectedPreview()
    {
        SelectedTilePreview.Children.Clear();

        DrawCellBackground(
            SelectedTilePreview,
            0,
            0,
            120);

        DrawTile(
            SelectedTilePreview,
            selectedTile,
            0,
            0,
            120);
    }

    private void UpdateSelectedTileName()
    {
        SelectedTileNameText.Text = selectedTile switch
        {
            "Empty" => "Пустая ячейка",
            "RoadHorizontal" => "Горизонтальная дорога",
            "RoadVertical" => "Вертикальная дорога",
            "CornerRightDown" => "Поворот вправо и вниз",
            "CornerLeftDown" => "Поворот влево и вниз",
            "CornerRightUp" => "Поворот вправо и вверх",
            "CornerLeftUp" => "Поворот влево и вверх",
            "TUp" => "Развилка вверх",
            "TDown" => "Развилка вниз",
            "TLeft" => "Развилка влево",
            "TRight" => "Развилка вправо",
            "Cross" => "Перекресток",
            _ => "Дорожный элемент"
        };
    }

    private void MapCanvas_MouseLeftButtonDown(
        object sender,
        MouseButtonEventArgs e)
    {
        Point point = e.GetPosition(MapCanvas);

        int x = (int)(point.X / CellSize);
        int y = (int)(point.Y / CellSize);

        if (x < 0 ||
            y < 0 ||
            x >= roadMap.Width ||
            y >= roadMap.Height)
        {
            return;
        }

        roadMap.SetTile(x, y, selectedTile);

        DrawMap();
        Focus();
    }

    private void MapCanvas_MouseMove(
        object sender,
        MouseEventArgs e)
    {
        Point point = e.GetPosition(MapCanvas);

        int x = (int)(point.X / CellSize);
        int y = (int)(point.Y / CellSize);

        if (x >= 0 &&
            y >= 0 &&
            x < roadMap.Width &&
            y < roadMap.Height)
        {
            CellInfoText.Text = $"Номер ячейки: {x}, {y}";
        }
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

                DrawCellBackground(
                    MapCanvas,
                    left,
                    top,
                    CellSize);

                DrawTile(
                    MapCanvas,
                    roadMap.GetTile(x, y),
                    left,
                    top,
                    CellSize);
            }
        }
    }

    private void DrawCellBackground(
        Canvas canvas,
        double x,
        double y,
        double size)
    {
        var rectangle = new Rectangle
        {
            Width = size,
            Height = size,
            Fill = new SolidColorBrush(Color.FromRgb(21, 21, 28)),
            Stroke = new SolidColorBrush(Color.FromRgb(78, 78, 82)),
            StrokeThickness = 1.2,
            IsHitTestVisible = false
        };

        Canvas.SetLeft(rectangle, x);
        Canvas.SetTop(rectangle, y);

        canvas.Children.Add(rectangle);
    }

    private void DrawTile(
        Canvas canvas,
        string tile,
        double x,
        double y,
        double size)
    {
        if (tile == "Empty")
            return;

        DrawPavement(canvas, x, y, size);

        switch (tile)
        {
            case "RoadHorizontal":
                DrawRoad(
                    canvas,
                    x,
                    y,
                    size,
                    left: true,
                    right: true,
                    up: false,
                    down: false);
                break;

            case "RoadVertical":
                DrawRoad(
                    canvas,
                    x,
                    y,
                    size,
                    left: false,
                    right: false,
                    up: true,
                    down: true);
                break;

            case "CornerRightDown":
                DrawRoad(
                    canvas,
                    x,
                    y,
                    size,
                    left: false,
                    right: true,
                    up: false,
                    down: true);
                break;

            case "CornerLeftDown":
                DrawRoad(
                    canvas,
                    x,
                    y,
                    size,
                    left: true,
                    right: false,
                    up: false,
                    down: true);
                break;

            case "CornerRightUp":
                DrawRoad(
                    canvas,
                    x,
                    y,
                    size,
                    left: false,
                    right: true,
                    up: true,
                    down: false);
                break;

            case "CornerLeftUp":
                DrawRoad(
                    canvas,
                    x,
                    y,
                    size,
                    left: true,
                    right: false,
                    up: true,
                    down: false);
                break;

            case "TUp":
                DrawRoad(
                    canvas,
                    x,
                    y,
                    size,
                    left: true,
                    right: true,
                    up: true,
                    down: false);
                break;

            case "TDown":
                DrawRoad(
                    canvas,
                    x,
                    y,
                    size,
                    left: true,
                    right: true,
                    up: false,
                    down: true);
                break;

            case "TLeft":
                DrawRoad(
                    canvas,
                    x,
                    y,
                    size,
                    left: true,
                    right: false,
                    up: true,
                    down: true);
                break;

            case "TRight":
                DrawRoad(
                    canvas,
                    x,
                    y,
                    size,
                    left: false,
                    right: true,
                    up: true,
                    down: true);
                break;

            case "Cross":
                DrawRoad(
                    canvas,
                    x,
                    y,
                    size,
                    left: true,
                    right: true,
                    up: true,
                    down: true);
                break;
        }
    }

    private void DrawPavement(
        Canvas canvas,
        double x,
        double y,
        double size)
    {
        var pavement = new Rectangle
        {
            Width = size,
            Height = size,
            Fill = new SolidColorBrush(Color.FromRgb(145, 145, 142)),
            IsHitTestVisible = false
        };

        Canvas.SetLeft(pavement, x);
        Canvas.SetTop(pavement, y);

        canvas.Children.Add(pavement);

        int count = 5;
        double cell = size / count;

        for (int index = 1; index < count; index++)
        {
            var verticalLine = new Line
            {
                X1 = x + index * cell,
                Y1 = y,
                X2 = x + index * cell,
                Y2 = y + size,
                Stroke = new SolidColorBrush(Color.FromRgb(90, 90, 90)),
                StrokeThickness = 0.7,
                IsHitTestVisible = false
            };

            var horizontalLine = new Line
            {
                X1 = x,
                Y1 = y + index * cell,
                X2 = x + size,
                Y2 = y + index * cell,
                Stroke = new SolidColorBrush(Color.FromRgb(90, 90, 90)),
                StrokeThickness = 0.7,
                IsHitTestVisible = false
            };

            canvas.Children.Add(verticalLine);
            canvas.Children.Add(horizontalLine);
        }
    }

    private void DrawRoad(
        Canvas canvas,
        double x,
        double y,
        double size,
        bool left,
        bool right,
        bool up,
        bool down)
    {
        double roadWidth = size * 0.55;
        double center = size / 2;
        double start = center - roadWidth / 2;

        DrawAsphalt(
            canvas,
            x + start,
            y + start,
            roadWidth,
            roadWidth);

        if (left)
        {
            DrawAsphalt(
                canvas,
                x,
                y + start,
                center,
                roadWidth);
        }

        if (right)
        {
            DrawAsphalt(
                canvas,
                x + center,
                y + start,
                center,
                roadWidth);
        }

        if (up)
        {
            DrawAsphalt(
                canvas,
                x + start,
                y,
                roadWidth,
                center);
        }

        if (down)
        {
            DrawAsphalt(
                canvas,
                x + start,
                y + center,
                roadWidth,
                center);
        }

        DrawRoadMarkings(
            canvas,
            x,
            y,
            size,
            left,
            right,
            up,
            down);
    }

    private void DrawAsphalt(
        Canvas canvas,
        double x,
        double y,
        double width,
        double height)
    {
        var asphalt = new Rectangle
        {
            Width = width,
            Height = height,
            Fill = new SolidColorBrush(Color.FromRgb(24, 24, 24)),
            IsHitTestVisible = false
        };

        Canvas.SetLeft(asphalt, x);
        Canvas.SetTop(asphalt, y);

        canvas.Children.Add(asphalt);
    }

    private void DrawRoadMarkings(
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
        double markLength = size * 0.14;
        double markThickness = size * 0.02;

        if (left)
        {
            DrawMark(
                canvas,
                x + size * 0.35,
                centerY,
                markLength,
                markThickness,
                horizontal: true);
        }

        if (right)
        {
            DrawMark(
                canvas,
                x + size * 0.65,
                centerY,
                markLength,
                markThickness,
                horizontal: true);
        }

        if (up)
        {
            DrawMark(
                canvas,
                centerX,
                y + size * 0.35,
                markLength,
                markThickness,
                horizontal: false);
        }

        if (down)
        {
            DrawMark(
                canvas,
                centerX,
                y + size * 0.65,
                markLength,
                markThickness,
                horizontal: false);
        }
    }

    private void DrawMark(
        Canvas canvas,
        double centerX,
        double centerY,
        double length,
        double thickness,
        bool horizontal)
    {
        var mark = new Rectangle
        {
            Width = horizontal ? length : thickness,
            Height = horizontal ? thickness : length,
            Fill = Brushes.White,
            Opacity = 0.85,
            IsHitTestVisible = false
        };

        Canvas.SetLeft(
            mark,
            centerX - mark.Width / 2);

        Canvas.SetTop(
            mark,
            centerY - mark.Height / 2);

        canvas.Children.Add(mark);
    }

    private void SaveMap_Click(
        object sender,
        RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Filter =
                "Road Editor map (*.roadmap)|*.roadmap|" +
                "JSON file (*.json)|*.json",

            FileName = "map.roadmap"
        };

        if (dialog.ShowDialog() != true)
            return;

        var data = new MapSaveData
        {
            Width = roadMap.Width,
            Height = roadMap.Height,
            Tiles = GetTilesArray()
        };

        string json = JsonSerializer.Serialize(
            data,
            new JsonSerializerOptions
            {
                WriteIndented = true
            });

        File.WriteAllText(dialog.FileName, json);
    }

    private void LoadMap_Click(
        object sender,
        RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter =
                "Road Editor map (*.roadmap)|*.roadmap|" +
                "JSON file (*.json)|*.json|" +
                "All files (*.*)|*.*"
        };

        if (dialog.ShowDialog() != true)
            return;

        try
        {
            string json = File.ReadAllText(dialog.FileName);

            MapSaveData? data =
                JsonSerializer.Deserialize<MapSaveData>(json);

            if (data == null ||
                data.Width < 3 ||
                data.Height < 3 ||
                data.Width > 50 ||
                data.Height > 50 ||
                data.Tiles == null)
            {
                MessageBox.Show(
                    "Файл карты имеет неправильный формат.",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return;
            }

            roadMap = new RoadMap(
                data.Width,
                data.Height);

            for (int y = 0; y < data.Height; y++)
            {
                for (int x = 0; x < data.Width; x++)
                {
                    string tile = "Empty";

                    if (y < data.Tiles.Length &&
                        data.Tiles[y] != null &&
                        x < data.Tiles[y].Length &&
                        !string.IsNullOrWhiteSpace(data.Tiles[y][x]))
                    {
                        tile = data.Tiles[y][x];
                    }

                    roadMap.SetTile(x, y, tile);
                }
            }

            MapWidthTextBox.Text =
                data.Width.ToString();

            MapHeightTextBox.Text =
                data.Height.ToString();

            DrawMap();
            Focus();
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                $"Не удалось загрузить карту.\n{exception.Message}",
                "Ошибка",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private string[][] GetTilesArray()
    {
        var result = new string[roadMap.Height][];

        for (int y = 0; y < roadMap.Height; y++)
        {
            result[y] = new string[roadMap.Width];

            for (int x = 0; x < roadMap.Width; x++)
            {
                result[y][x] = roadMap.GetTile(x, y);
            }
        }

        return result;
    }

    private void ExportPng_Click(
        object sender,
        RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "PNG image (*.png)|*.png",
            FileName = "road-map.png"
        };

        if (dialog.ShowDialog() != true)
            return;

        Size mapSize = new(
            MapCanvas.Width,
            MapCanvas.Height);

        MapCanvas.Measure(mapSize);
        MapCanvas.Arrange(new Rect(mapSize));
        MapCanvas.UpdateLayout();

        var bitmap = new RenderTargetBitmap(
            (int)MapCanvas.Width,
            (int)MapCanvas.Height,
            96,
            96,
            PixelFormats.Pbgra32);

        bitmap.Render(MapCanvas);

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(
            BitmapFrame.Create(bitmap));

        using FileStream stream =
            File.Create(dialog.FileName);

        encoder.Save(stream);
    }

    private void Help_Click(
        object sender,
        RoutedEventArgs e)
    {
        MessageBox.Show(
            "Нажми на дорожный элемент в палитре.\n" +
            "После этого нажми на нужную ячейку карты.\n\n" +
            "Сохранить — сохранить карту в файл.\n" +
            "Загрузить — открыть сохраненную карту.\n" +
            "Экспорт PNG — сохранить карту как изображение.\n" +
            "F1 — открыть справку.",
            "Справка",
            MessageBoxButton.OK,
            MessageBoxImage.Information);

        Focus();
    }

    private void Window_PreviewKeyDown(
        object sender,
        KeyEventArgs e)
    {
        if (e.Key == Key.F1)
        {
            Help_Click(sender, e);
            e.Handled = true;
        }
    }

    private sealed class MapSaveData
    {
        public int Width { get; set; }

        public int Height { get; set; }

        public string[][] Tiles { get; set; } =
            Array.Empty<string[]>();
    }
}
