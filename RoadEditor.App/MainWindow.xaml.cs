using Microsoft.Win32;
using RoadEditor.Core;
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

    private RoadMap roadMap = new(12, 7);
    private string selectedTile = "RoadHorizontal";

    public MainWindow()
    {
        InitializeComponent();

        Loaded += (_, _) => Focus();

        DrawMap();
        DrawSelectedPreview();
    }

    private void CreateMap_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(MapWidthTextBox.Text, out int width) ||
            !int.TryParse(MapHeightTextBox.Text, out int height))
        {
            MessageBox.Show("Введите корректные размеры карты.", "Ошибка");
            return;
        }

        if (width < 3 || height < 3 || width > 50 || height > 50)
        {
            MessageBox.Show("Размер карты должен быть от 3 до 50 клеток.", "Ошибка");
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

    private void SaveMap_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "Road editor map (*.roadmap)|*.roadmap|JSON file (*.json)|*.json",
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

        string json = JsonSerializer.Serialize(data, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        File.WriteAllText(dialog.FileName, json);
    }

    private void LoadMap_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Road editor map (*.roadmap)|*.roadmap|JSON file (*.json)|*.json|All files (*.*)|*.*"
        };

        if (dialog.ShowDialog() != true)
            return;

        try
        {
            string json = File.ReadAllText(dialog.FileName);
            MapSaveData? data = JsonSerializer.Deserialize<MapSaveData>(json);

            if (data == null || data.Width < 3 || data.Height < 3 || data.Tiles == null)
            {
                MessageBox.Show("Файл карты поврежден или имеет неправильный формат.", "Ошибка");
                return;
            }

            roadMap = new RoadMap(data.Width, data.Height);

            for (int y = 0; y < data.Height; y++)
            {
                for (int x = 0; x < data.Width; x++)
                {
                    string tile = "Empty";

                    if (y < data.Tiles.Length && x < data.Tiles[y].Length)
                        tile = data.Tiles[y][x] ?? "Empty";

                    roadMap.SetTile(x, y, tile);
                }
            }

            MapWidthTextBox.Text = data.Width.ToString();
            MapHeightTextBox.Text = data.Height.ToString();

            DrawMap();
            Focus();
        }
        catch
        {
            MessageBox.Show("Не удалось загрузить карту.", "Ошибка");
        }
    }

    private void ExportPng_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "PNG image (*.png)|*.png",
            FileName = "road-map.png"
        };

        if (dialog.ShowDialog() != true)
            return;

        Size size = new(MapCanvas.Width, MapCanvas.Height);

        MapCanvas.Measure(size);
        MapCanvas.Arrange(new Rect(size));
        MapCanvas.UpdateLayout();

        var bitmap = new RenderTargetBitmap(
            (int)MapCanvas.Width,
            (int)MapCanvas.Height,
            96,
            96,
            PixelFormats.Pbgra32);

        bitmap.Render(MapCanvas);

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));

        using FileStream stream = File.Create(dialog.FileName);
        encoder.Save(stream);
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

    private void TileButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string tile)
        {
            selectedTile = tile;
            DrawSelectedPreview();
            Focus();
        }
    }

    private void MapCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        Point point = e.GetPosition(MapCanvas);

        int x = (int)(point.X / CellSize);
        int y = (int)(point.Y / CellSize);

        if (x >= 0 && y >= 0 && x < roadMap.Width && y < roadMap.Height)
        {
            roadMap.SetTile(x, y, selectedTile);
            DrawMap();
        }

        Focus();
    }

    private void MapCanvas_MouseMove(object sender, MouseEventArgs e)
    {
        Point point = e.GetPosition(MapCanvas);

        int x = (int)(point.X / CellSize);
        int y = (int)(point.Y / CellSize);

        if (x >= 0 && y >= 0 && x < roadMap.Width && y < roadMap.Height)
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

                DrawCellBackground(MapCanvas, left, top, CellSize);
                DrawTile(MapCanvas, roadMap.GetTile(x, y), left, top, CellSize);
            }
        }
    }

    private void DrawSelectedPreview()
    {
        SelectedTilePreview.Children.Clear();
        DrawCellBackground(SelectedTilePreview, 0, 0, 120);
        DrawTile(SelectedTilePreview, selectedTile, 0, 0, 120);
    }

    private void DrawCellBackground(Canvas canvas, double x, double y, double size)
    {
        var rectangle = new Rectangle
        {
            Width = size,
            Height = size,
            Fill = new SolidColorBrush(Color.FromRgb(21, 21, 28)),
            Stroke = new SolidColorBrush(Color.FromRgb(78, 78, 82)),
            StrokeThickness = 1.2
        };

        Canvas.SetLeft(rectangle, x);
        Canvas.SetTop(rectangle, y);
        canvas.Children.Add(rectangle);
    }

    private void DrawTile(Canvas canvas, string tile, double x, double y, double size)
    {
        if (tile == "Empty")
            return;

        DrawPavement(canvas, x, y, size);

        switch (tile)
        {
            case "RoadHorizontal":
                DrawRoad(canvas, x, y, size, left: true, right: true, up: false, down: false);
                break;

            case "RoadVertical":
                DrawRoad(canvas, x, y, size, left: false, right: false, up: true, down: true);
                break;

            case "CornerRightDown":
                DrawRoad(canvas, x, y, size, left: false, right: true, up: false, down: true);
                break;

            case "CornerLeftDown":
                DrawRoad(canvas, x, y, size, left: true, right: false, up: false, down: true);
                break;

            case "CornerRightUp":
                DrawRoad(canvas, x, y, size, left: false, right: true, up: true, down: false);
                break;

            case "CornerLeftUp":
                DrawRoad(canvas, x, y, size, left: true, right: false, up: true, down: false);
                break;

            case "TUp":
                DrawRoad(canvas, x, y, size, left: true, right: true, up: true, down: false);
                break;

            case "TDown":
                DrawRoad(canvas, x, y, size, left: true, right: true, up: false, down: true);
                break;

            case "TLeft":
                DrawRoad(canvas, x, y, size, left: true, right: false, up: true, down: true);
                break;

            case "TRight":
                DrawRoad(canvas, x, y, size, left: false, right: true, up: true, down: true);
                break;

            case "Cross":
                DrawRoad(canvas, x, y, size, left: true, right: true, up: true, down: true);
                break;
        }
    }

    private void DrawPavement(Canvas canvas, double x, double y, double size)
    {
        var pavement = new Rectangle
        {
            Width = size,
            Height = size,
            Fill = new SolidColorBrush(Color.FromRgb(145, 145, 142))
        };

        Canvas.SetLeft(pavement, x);
        Canvas.SetTop(pavement, y);
        canvas.Children.Add(pavement);

        int count = 5;
        double cell = size / count;

        for (int i = 1; i < count; i++)
        {
            var verticalLine = new Line
            {
                X1 = x + i * cell,
                Y1 = y,
                X2 = x + i * cell,
                Y2 = y + size,
                Stroke = new SolidColorBrush(Color.FromRgb(90, 90, 90)),
                StrokeThickness = 0.7
            };

            var horizontalLine = new Line
            {
                X1 = x,
                Y1 = y + i * cell,
                X2 = x + size,
                Y2 = y + i * cell,
                Stroke = new SolidColorBrush(Color.FromRgb(90, 90, 90)),
                StrokeThickness = 0.7
            };

            canvas.Children.Add(verticalLine);
            canvas.Children.Add(horizontalLine);
        }
    }

    private void DrawRoad(Canvas canvas, double x, double y, double size, bool left, bool right, bool up, bool down)
    {
        double roadWidth = size * 0.55;
        double center = size / 2;
        double start = center - roadWidth / 2;

        DrawAsphalt(canvas, x + start, y + start, roadWidth, roadWidth);

        if (left)
            DrawAsphalt(canvas, x, y + start, center, roadWidth);

        if (right)
            DrawAsphalt(canvas, x + center, y + start, center, roadWidth);

        if (up)
            DrawAsphalt(canvas, x + start, y, roadWidth, center);

        if (down)
            DrawAsphalt(canvas, x + start, y + center, roadWidth, center);

        DrawRoadMarkings(canvas, x, y, size, left, right, up, down);
    }

    private void DrawAsphalt(Canvas canvas, double x, double y, double width, double height)
    {
        var asphalt = new Rectangle
        {
            Width = width,
            Height = height,
            Fill = new SolidColorBrush(Color.FromRgb(24, 24, 24))
        };

        Canvas.SetLeft(asphalt, x);
        Canvas.SetTop(asphalt, y);
        canvas.Children.Add(asphalt);
    }

    private void DrawRoadMarkings(Canvas canvas, double x, double y, double size, bool left, bool right, bool up, bool down)
    {
        double centerX = x + size / 2;
        double centerY = y + size / 2;
        double markLength = size * 0.14;
        double markThickness = size * 0.02;

        if (left)
            DrawMark(canvas, x + size * 0.35, centerY, markLength, markThickness, true);

        if (right)
            DrawMark(canvas, x + size * 0.65, centerY, markLength, markThickness, true);

        if (up)
            DrawMark(canvas, centerX, y + size * 0.35, markLength, markThickness, false);

        if (down)
            DrawMark(canvas, centerX, y + size * 0.65, markLength, markThickness, false);
    }

    private void DrawMark(Canvas canvas, double centerX, double centerY, double length, double thickness, bool horizontal)
    {
        var mark = new Rectangle
        {
            Width = horizontal ? length : thickness,
            Height = horizontal ? thickness : length,
            Fill = Brushes.White,
            Opacity = 0.85
        };

        Canvas.SetLeft(mark, centerX - mark.Width / 2);
        Canvas.SetTop(mark, centerY - mark.Height / 2);
        canvas.Children.Add(mark);
    }

    private void Help_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
            "ЛКМ по карте - поставить выбранный дорожный элемент.\n" +
            "«Сохранить» сохраняет карту в файл .roadmap.\n" +
            "«Загрузить» открывает ранее сохраненную карту.\n" +
            "«Экспорт PNG» сохраняет карту как изображение.\n" +
            "F1 - справка.",
            "Справка");
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
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

        public string[][] Tiles { get; set; } = [];
    }
}
