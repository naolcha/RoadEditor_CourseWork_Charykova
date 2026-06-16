using System;
using Microsoft.Win32;
using RoadEditor.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Controls;

namespace RoadEditor.App;

public partial class MainWindow
{
    private const int MaximumMapSize = 80;

    private void ResizeMap_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (!int.TryParse(
                MapWidthTextBox.Text,
                out int newWidth) ||
            !int.TryParse(
                MapHeightTextBox.Text,
                out int newHeight))
        {
            MessageBox.Show(
                "Введите корректные размеры карты.",
                "Ошибка",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        if (newWidth < 3 ||
            newHeight < 3 ||
            newWidth > MaximumMapSize ||
            newHeight > MaximumMapSize)
        {
            MessageBox.Show(
                $"Размер карты должен быть от 3 до {MaximumMapSize} клеток.",
                "Ошибка",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        if (newWidth == roadMap.Width &&
            newHeight == roadMap.Height)
        {
            return;
        }

        SaveStateForUndo();

        RoadMap resizedMap =
            new(newWidth, newHeight);

        int copiedWidth =
            Math.Min(
                roadMap.Width,
                newWidth);

        int copiedHeight =
            Math.Min(
                roadMap.Height,
                newHeight);

        for (int y = 0; y < copiedHeight; y++)
        {
            for (int x = 0; x < copiedWidth; x++)
            {
                resizedMap.SetTile(
                    x,
                    y,
                    roadMap.GetTile(x, y));
            }
        }

        roadMap = resizedMap;

        MapWidthTextBox.Text =
            newWidth.ToString();

        MapHeightTextBox.Text =
            newHeight.ToString();

        DrawMap();
        Focus();
    }

    private void SaveProject_Click(
        object sender,
        RoutedEventArgs e)
    {
        var dialog =
            new SaveFileDialog
            {
                Filter =
                    "Road Editor map (*.roadmap)|*.roadmap|" +
                    "JSON file (*.json)|*.json",

                FileName =
                    "map.roadmap"
            };

        if (dialog.ShowDialog() != true)
            return;

        var data =
            new RoadEditorSaveData
            {
                FormatVersion = 2,
                Width = roadMap.Width,
                Height = roadMap.Height,
                Tiles = GetTilesArray(),
                Shapes = CaptureOverlayShapes()
            };

        string json =
            JsonSerializer.Serialize(
                data,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                });

        File.WriteAllText(
            dialog.FileName,
            json);
    }

    private void LoadProject_Click(
        object sender,
        RoutedEventArgs e)
    {
        var dialog =
            new OpenFileDialog
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
            string json =
                File.ReadAllText(
                    dialog.FileName);

            RoadEditorSaveData? data =
                JsonSerializer.Deserialize<RoadEditorSaveData>(
                    json,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

            if (!IsValidSaveData(data))
            {
                MessageBox.Show(
                    "Файл карты имеет неправильный формат.",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return;
            }

            SaveStateForUndo();

            RoadMap loadedMap =
                new(
                    data!.Width,
                    data.Height);

            for (int y = 0; y < data.Height; y++)
            {
                for (int x = 0; x < data.Width; x++)
                {
                    string tileName =
                        GetSavedTileName(
                            data.Tiles,
                            x,
                            y);

                    try
                    {
                        loadedMap.SetTile(
                            x,
                            y,
                            tileName);
                    }
                    catch
                    {
                        loadedMap.SetTile(
                            x,
                            y,
                            RoadTileType.Empty);
                    }
                }
            }

            roadMap = loadedMap;

            MapWidthTextBox.Text =
                data.Width.ToString();

            MapHeightTextBox.Text =
                data.Height.ToString();

            DeselectShape();
            DrawingCanvas.Children.Clear();

            DrawMap();

            RestoreOverlayShapes(
                data.Shapes ??
                new List<OverlayShapeSaveData>());

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

    private static bool IsValidSaveData(
        RoadEditorSaveData? data)
    {
        return data != null &&
               data.Width >= 3 &&
               data.Height >= 3 &&
               data.Width <= MaximumMapSize &&
               data.Height <= MaximumMapSize &&
               data.Tiles != null;
    }

    private static string GetSavedTileName(
        string[][] tiles,
        int x,
        int y)
    {
        if (y < 0 ||
            y >= tiles.Length ||
            tiles[y] == null ||
            x < 0 ||
            x >= tiles[y].Length ||
            string.IsNullOrWhiteSpace(
                tiles[y][x]))
        {
            return RoadTileType.Empty.ToString();
        }

        return tiles[y][x];
    }

    private List<OverlayShapeSaveData>
        CaptureOverlayShapes()
    {
        var result =
            new List<OverlayShapeSaveData>();

        foreach (Shape shape in
                 DrawingCanvas.Children
                     .OfType<Shape>())
        {
            var item =
                new OverlayShapeSaveData
                {
                    Tool =
                        shape.Tag?.ToString() ??
                        GetToolNameByShape(shape),

                    Stroke =
                        BrushToText(
                            shape.Stroke),

                    Fill =
                        BrushToText(
                            shape.Fill),

                    StrokeThickness =
                        shape.StrokeThickness
                };

            switch (shape)
            {
                case Line line:
                    item.X1 = line.X1;
                    item.Y1 = line.Y1;
                    item.X2 = line.X2;
                    item.Y2 = line.Y2;
                    break;

                case Rectangle rectangle:
                    item.X1 =
                        GetCanvasLeft(
                            rectangle);

                    item.Y1 =
                        GetCanvasTop(
                            rectangle);

                    item.X2 =
                        item.X1 +
                        rectangle.Width;

                    item.Y2 =
                        item.Y1 +
                        rectangle.Height;
                    break;

                case Ellipse ellipse:
                    item.X1 =
                        GetCanvasLeft(
                            ellipse);

                    item.Y1 =
                        GetCanvasTop(
                            ellipse);

                    item.X2 =
                        item.X1 +
                        ellipse.Width;

                    item.Y2 =
                        item.Y1 +
                        ellipse.Height;
                    break;

                case Polygon polygon:
                    foreach (Point point in
                             polygon.Points)
                    {
                        item.Points.Add(
                            new ShapePointSaveData
                            {
                                X = point.X,
                                Y = point.Y
                            });
                    }

                    SetBoundsFromPoints(
                        item,
                        polygon.Points);
                    break;
            }

            result.Add(item);
        }

        return result;
    }

    private void RestoreOverlayShapes(
        IEnumerable<OverlayShapeSaveData> shapes)
    {
        foreach (OverlayShapeSaveData item in shapes)
        {
            if (!Enum.TryParse(
                    item.Tool,
                    ignoreCase: true,
                    out DrawingTool tool) ||
                tool is DrawingTool.Road or
                    DrawingTool.Select)
            {
                continue;
            }

            Shape? shape =
                CreateShape(tool);

            if (shape == null)
                continue;

            shape.Tag = tool;

            shape.Stroke =
                BrushFromText(
                    item.Stroke,
                    Brushes.White);

            shape.Fill =
                shape is Line
                    ? Brushes.Transparent
                    : BrushFromText(
                        item.Fill,
                        Brushes.Transparent);

            shape.StrokeThickness =
                item.StrokeThickness > 0
                    ? item.StrokeThickness
                    : 3;

            switch (shape)
            {
                case Line line:
                    line.X1 = item.X1;
                    line.Y1 = item.Y1;
                    line.X2 = item.X2;
                    line.Y2 = item.Y2;
                    break;

                case Rectangle rectangle:
                    rectangle.Width =
                        Math.Abs(
                            item.X2 -
                            item.X1);

                    rectangle.Height =
                        Math.Abs(
                            item.Y2 -
                            item.Y1);

                    Canvas.SetLeft(
                        rectangle,
                        Math.Min(
                            item.X1,
                            item.X2));

                    Canvas.SetTop(
                        rectangle,
                        Math.Min(
                            item.Y1,
                            item.Y2));
                    break;

                case Ellipse ellipse:
                    ellipse.Width =
                        Math.Abs(
                            item.X2 -
                            item.X1);

                    ellipse.Height =
                        Math.Abs(
                            item.Y2 -
                            item.Y1);

                    Canvas.SetLeft(
                        ellipse,
                        Math.Min(
                            item.X1,
                            item.X2));

                    Canvas.SetTop(
                        ellipse,
                        Math.Min(
                            item.Y1,
                            item.Y2));
                    break;

                case Polygon polygon:
                    if (item.Points.Count > 0)
                    {
                        polygon.Points =
                            new PointCollection(
                                item.Points.Select(
                                    point =>
                                        new Point(
                                            point.X,
                                            point.Y)));
                    }
                    else
                    {
                        UpdateShapeGeometry(
                            polygon,
                            new Point(
                                item.X1,
                                item.Y1),
                            new Point(
                                item.X2,
                                item.Y2));
                    }
                    break;
            }

            DrawingCanvas.Children.Add(
                shape);
        }
    }

    private static void SetBoundsFromPoints(
        OverlayShapeSaveData item,
        PointCollection points)
    {
        if (points.Count == 0)
            return;

        item.X1 =
            points.Min(
                point =>
                    point.X);

        item.Y1 =
            points.Min(
                point =>
                    point.Y);

        item.X2 =
            points.Max(
                point =>
                    point.X);

        item.Y2 =
            points.Max(
                point =>
                    point.Y);
    }

    private static string GetToolNameByShape(
        Shape shape)
    {
        return shape switch
        {
            Line =>
                DrawingTool.Line.ToString(),

            Rectangle =>
                DrawingTool.Rectangle.ToString(),

            Ellipse =>
                DrawingTool.Ellipse.ToString(),

            Polygon =>
                DrawingTool.Triangle.ToString(),

            _ =>
                DrawingTool.Rectangle.ToString()
        };
    }

    private static string BrushToText(
        Brush? brush)
    {
        if (brush is not SolidColorBrush solidBrush)
            return "Transparent";

        return solidBrush.Color.ToString();
    }

    private static Brush BrushFromText(
        string? colorText,
        Brush fallback)
    {
        if (string.IsNullOrWhiteSpace(colorText) ||
            string.Equals(
                colorText,
                "Transparent",
                StringComparison.OrdinalIgnoreCase))
        {
            return Brushes.Transparent;
        }

        try
        {
            Color color =
                (Color)ColorConverter.ConvertFromString(
                    colorText);

            return new SolidColorBrush(
                color);
        }
        catch
        {
            return fallback;
        }
    }

    private void ExportProjectPng_Click(
        object sender,
        RoutedEventArgs e)
    {
        var dialog =
            new SaveFileDialog
            {
                Filter =
                    "PNG image (*.png)|*.png",

                FileName =
                    "road-map.png"
            };

        if (dialog.ShowDialog() != true)
            return;

        Effect? selectedEffect =
            selectedShape?.Effect;

        if (selectedShape != null)
        {
            selectedShape.Effect = null;
        }

        double previousScaleX =
            MapScaleTransform.ScaleX;

        double previousScaleY =
            MapScaleTransform.ScaleY;

        try
        {
            MapScaleTransform.ScaleX = 1;
            MapScaleTransform.ScaleY = 1;

            Size mapSize =
                new(
                    roadMap.Width *
                    CellSize,

                    roadMap.Height *
                    CellSize);

            MapHost.Measure(
                mapSize);

            MapHost.Arrange(
                new Rect(
                    mapSize));

            MapHost.UpdateLayout();

            var bitmap =
                new RenderTargetBitmap(
                    (int)mapSize.Width,
                    (int)mapSize.Height,
                    96,
                    96,
                    PixelFormats.Pbgra32);

            bitmap.Render(
                MapHost);

            var encoder =
                new PngBitmapEncoder();

            encoder.Frames.Add(
                BitmapFrame.Create(
                    bitmap));

            using FileStream stream =
                File.Create(
                    dialog.FileName);

            encoder.Save(
                stream);
        }
        finally
        {
            MapScaleTransform.ScaleX =
                previousScaleX;

            MapScaleTransform.ScaleY =
                previousScaleY;

            if (selectedShape != null)
            {
                selectedShape.Effect =
                    selectedEffect;
            }

            MapHost.UpdateLayout();
        }
    }
}

public sealed class RoadEditorSaveData
{
    public int FormatVersion { get; set; } = 2;

    public int Width { get; set; }

    public int Height { get; set; }

    public string[][] Tiles { get; set; } =
        Array.Empty<string[]>();

    public List<OverlayShapeSaveData> Shapes { get; set; } =
        new();
}

public sealed class OverlayShapeSaveData
{
    public string Tool { get; set; } =
        string.Empty;

    public string Stroke { get; set; } =
        "#FFFFFFFF";

    public string Fill { get; set; } =
        "Transparent";

    public double StrokeThickness { get; set; } =
        3;

    public double X1 { get; set; }

    public double Y1 { get; set; }

    public double X2 { get; set; }

    public double Y2 { get; set; }

    public List<ShapePointSaveData> Points { get; set; } =
        new();
}

public sealed class ShapePointSaveData
{
    public double X { get; set; }

    public double Y { get; set; }
}