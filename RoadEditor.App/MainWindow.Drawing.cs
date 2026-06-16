using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;

namespace RoadEditor.App;

public partial class MainWindow
{
    private enum DrawingTool
    {
        Road,
        Select,
        Line,
        Rectangle,
        Ellipse,
        Triangle,
        Arrow,
        Star
    }

    private DrawingTool currentDrawingTool =
        DrawingTool.Road;

    private Point drawingStartPoint;
    private Point lastMovePoint;

    private Shape? previewShape;
    private Shape? selectedShape;

    private bool isDrawingShape;
    private bool isMovingShape;

    private void DrawingTool_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (sender is not Button button ||
            button.Tag is not string toolName ||
            !Enum.TryParse(
                toolName,
                out DrawingTool tool))
        {
            return;
        }

        currentDrawingTool = tool;

        DrawingCanvas.IsHitTestVisible =
            currentDrawingTool != DrawingTool.Road;

        DeselectShape();

        DrawingModeText.Text =
            currentDrawingTool switch
            {
                DrawingTool.Road =>
                    "Режим: размещение дорог",

                DrawingTool.Select =>
                    "Режим: выбор и перемещение фигур",

                DrawingTool.Line =>
                    "Режим: линия",

                DrawingTool.Rectangle =>
                    "Режим: прямоугольник",

                DrawingTool.Ellipse =>
                    "Режим: эллипс",

                DrawingTool.Triangle =>
                    "Режим: треугольник",

                DrawingTool.Arrow =>
                    "Режим: стрелка",

                DrawingTool.Star =>
                    "Режим: звезда",

                _ =>
                    "Режим рисования"
            };

        Focus();
    }

    private void DrawingCanvas_MouseLeftButtonDown(
        object sender,
        MouseButtonEventArgs e)
    {
        Point position =
            e.GetPosition(DrawingCanvas);

        if (currentDrawingTool == DrawingTool.Select)
        {
            Shape? clickedShape =
                FindShape(e.OriginalSource as DependencyObject);

            if (clickedShape == null)
            {
                DeselectShape();
                return;
            }

            SelectShape(clickedShape);

            isMovingShape = true;
            lastMovePoint = position;

            DrawingCanvas.CaptureMouse();

            e.Handled = true;
            return;
        }

        if (currentDrawingTool == DrawingTool.Road)
            return;

        DeselectShape();

        drawingStartPoint = position;

        previewShape =
            CreateShape(currentDrawingTool);

        if (previewShape == null)
            return;

        DrawingCanvas.Children.Add(previewShape);

        isDrawingShape = true;

        DrawingCanvas.CaptureMouse();

        UpdateShapeGeometry(
            previewShape,
            drawingStartPoint,
            drawingStartPoint);

        e.Handled = true;
    }

    private void DrawingCanvas_MouseMove(
        object sender,
        MouseEventArgs e)
    {
        Point currentPoint =
            e.GetPosition(DrawingCanvas);

        if (isDrawingShape &&
            previewShape != null &&
            e.LeftButton == MouseButtonState.Pressed)
        {
            UpdateShapeGeometry(
                previewShape,
                drawingStartPoint,
                currentPoint);

            e.Handled = true;
            return;
        }

        if (isMovingShape &&
            selectedShape != null &&
            e.LeftButton == MouseButtonState.Pressed)
        {
            Vector shift =
                currentPoint - lastMovePoint;

            MoveShape(
                selectedShape,
                shift.X,
                shift.Y);

            lastMovePoint = currentPoint;

            e.Handled = true;
        }
    }

    private void DrawingCanvas_MouseLeftButtonUp(
        object sender,
        MouseButtonEventArgs e)
    {
        if (isDrawingShape)
        {
            Point currentPoint =
                e.GetPosition(DrawingCanvas);

            if (previewShape != null)
            {
                UpdateShapeGeometry(
                    previewShape,
                    drawingStartPoint,
                    currentPoint);

                double width =
                    Math.Abs(
                        currentPoint.X -
                        drawingStartPoint.X);

                double height =
                    Math.Abs(
                        currentPoint.Y -
                        drawingStartPoint.Y);

                if (width < 4 && height < 4)
                {
                    DrawingCanvas.Children.Remove(
                        previewShape);
                }
                else
                {
                    SelectShape(previewShape);
                }
            }

            previewShape = null;
            isDrawingShape = false;

            DrawingCanvas.ReleaseMouseCapture();

            e.Handled = true;
            return;
        }

        if (isMovingShape)
        {
            isMovingShape = false;

            DrawingCanvas.ReleaseMouseCapture();

            e.Handled = true;
        }
    }

    private Shape? CreateShape(
        DrawingTool tool)
    {
        Brush strokeBrush =
            GetSelectedBrush(
                StrokeColorComboBox,
                Brushes.White);

        Brush fillBrush =
            GetFillBrush();

        Shape? shape =
            tool switch
            {
                DrawingTool.Line =>
                    new Line(),

                DrawingTool.Rectangle =>
                    new Rectangle(),

                DrawingTool.Ellipse =>
                    new Ellipse(),

                DrawingTool.Triangle =>
                    new Polygon(),

                DrawingTool.Arrow =>
                    new Polygon(),

                DrawingTool.Star =>
                    new Polygon(),

                _ =>
                    null
            };

        if (shape == null)
            return null;

        shape.Stroke = strokeBrush;
        shape.StrokeThickness = 3;
        shape.Tag = tool;

        if (shape is Line)
        {
            shape.Fill = Brushes.Transparent;
        }
        else
        {
            shape.Fill = fillBrush;
        }

        return shape;
    }

    private Brush GetFillBrush()
    {
        if (UseFillCheckBox.IsChecked != true)
        {
            return Brushes.Transparent;
        }

        return GetSelectedBrush(
            FillColorComboBox,
            Brushes.Red);
    }

    private static Brush GetSelectedBrush(
        ComboBox comboBox,
        Brush fallback)
    {
        if (comboBox.SelectedItem is not ComboBoxItem item ||
            item.Tag is not string colorText)
        {
            return fallback;
        }

        try
        {
            return new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString(
                    colorText));
        }
        catch
        {
            return fallback;
        }
    }

    private void UpdateShapeGeometry(
        Shape shape,
        Point start,
        Point end)
    {
        if (shape is Line line)
        {
            line.X1 = start.X;
            line.Y1 = start.Y;
            line.X2 = end.X;
            line.Y2 = end.Y;

            return;
        }

        double left =
            Math.Min(start.X, end.X);

        double top =
            Math.Min(start.Y, end.Y);

        double width =
            Math.Abs(end.X - start.X);

        double height =
            Math.Abs(end.Y - start.Y);

        if (shape is Rectangle rectangle)
        {
            rectangle.Width = width;
            rectangle.Height = height;

            Canvas.SetLeft(rectangle, left);
            Canvas.SetTop(rectangle, top);

            return;
        }

        if (shape is Ellipse ellipse)
        {
            ellipse.Width = width;
            ellipse.Height = height;

            Canvas.SetLeft(ellipse, left);
            Canvas.SetTop(ellipse, top);

            return;
        }

        if (shape is not Polygon polygon ||
            shape.Tag is not DrawingTool tool)
        {
            return;
        }

        polygon.Points =
            tool switch
            {
                DrawingTool.Triangle =>
                    CreateTrianglePoints(
                        left,
                        top,
                        width,
                        height),

                DrawingTool.Arrow =>
                    CreateArrowPoints(
                        start,
                        end),

                DrawingTool.Star =>
                    CreateStarPoints(
                        left,
                        top,
                        width,
                        height),

                _ =>
                    new PointCollection()
            };
    }

    private static PointCollection CreateTrianglePoints(
        double left,
        double top,
        double width,
        double height)
    {
        return new PointCollection
        {
            new Point(
                left + width / 2,
                top),

            new Point(
                left + width,
                top + height),

            new Point(
                left,
                top + height)
        };
    }

    private static PointCollection CreateArrowPoints(
        Point start,
        Point end)
    {
        Vector direction =
            end - start;

        double length =
            direction.Length;

        if (length < 1)
        {
            return new PointCollection
            {
                start,
                end
            };
        }

        direction.Normalize();

        Vector perpendicular =
            new(
                -direction.Y,
                direction.X);

        double bodyHalfWidth =
            Math.Clamp(
                length * 0.08,
                4,
                14);

        double headHalfWidth =
            Math.Clamp(
                length * 0.18,
                8,
                28);

        double headLength =
            Math.Clamp(
                length * 0.3,
                12,
                45);

        Point headStart =
            end - direction * headLength;

        return new PointCollection
        {
            start + perpendicular * bodyHalfWidth,

            headStart +
            perpendicular * bodyHalfWidth,

            headStart +
            perpendicular * headHalfWidth,

            end,

            headStart -
            perpendicular * headHalfWidth,

            headStart -
            perpendicular * bodyHalfWidth,

            start -
            perpendicular * bodyHalfWidth
        };
    }

    private static PointCollection CreateStarPoints(
        double left,
        double top,
        double width,
        double height)
    {
        var points =
            new PointCollection();

        double centerX =
            left + width / 2;

        double centerY =
            top + height / 2;

        double outerRadius =
            Math.Min(width, height) / 2;

        double innerRadius =
            outerRadius * 0.45;

        for (int index = 0;
             index < 10;
             index++)
        {
            double angle =
                -Math.PI / 2 +
                index * Math.PI / 5;

            double radius =
                index % 2 == 0
                    ? outerRadius
                    : innerRadius;

            points.Add(
                new Point(
                    centerX +
                    Math.Cos(angle) * radius,

                    centerY +
                    Math.Sin(angle) * radius));
        }

        return points;
    }

    private Shape? FindShape(
        DependencyObject? source)
    {
        DependencyObject? current =
            source;

        while (current != null &&
               current != DrawingCanvas)
        {
            if (current is Shape shape &&
                DrawingCanvas.Children.Contains(shape))
            {
                return shape;
            }

            current =
                VisualTreeHelper.GetParent(current);
        }

        return null;
    }

    private void SelectShape(
        Shape shape)
    {
        DeselectShape();

        selectedShape = shape;

        selectedShape.Effect =
            new DropShadowEffect
            {
                Color = Colors.DeepSkyBlue,
                BlurRadius = 14,
                ShadowDepth = 0,
                Opacity = 1
            };

        Panel.SetZIndex(
            selectedShape,
            100);

        SelectedShapeText.Text =
            $"Выбрана фигура: {GetShapeName(shape)}";
    }

    private void DeselectShape()
    {
        if (selectedShape != null)
        {
            selectedShape.Effect = null;

            Panel.SetZIndex(
                selectedShape,
                0);
        }

        selectedShape = null;

        SelectedShapeText.Text =
            "Фигура не выбрана";
    }

    private static string GetShapeName(
        Shape shape)
    {
        if (shape.Tag is not DrawingTool tool)
            return "фигура";

        return tool switch
        {
            DrawingTool.Line =>
                "линия",

            DrawingTool.Rectangle =>
                "прямоугольник",

            DrawingTool.Ellipse =>
                "эллипс",

            DrawingTool.Triangle =>
                "треугольник",

            DrawingTool.Arrow =>
                "стрелка",

            DrawingTool.Star =>
                "звезда",

            _ =>
                "фигура"
        };
    }

    private static void MoveShape(
        Shape shape,
        double shiftX,
        double shiftY)
    {
        switch (shape)
        {
            case Line line:
                line.X1 += shiftX;
                line.Y1 += shiftY;
                line.X2 += shiftX;
                line.Y2 += shiftY;
                break;

            case Rectangle rectangle:
                Canvas.SetLeft(
                    rectangle,
                    GetCanvasLeft(rectangle) + shiftX);

                Canvas.SetTop(
                    rectangle,
                    GetCanvasTop(rectangle) + shiftY);
                break;

            case Ellipse ellipse:
                Canvas.SetLeft(
                    ellipse,
                    GetCanvasLeft(ellipse) + shiftX);

                Canvas.SetTop(
                    ellipse,
                    GetCanvasTop(ellipse) + shiftY);
                break;

            case Polygon polygon:
                for (int index = 0;
                     index < polygon.Points.Count;
                     index++)
                {
                    Point point =
                        polygon.Points[index];

                    polygon.Points[index] =
                        new Point(
                            point.X + shiftX,
                            point.Y + shiftY);
                }
                break;

            case Polyline polyline:
                for (int index = 0;
                     index < polyline.Points.Count;
                     index++)
                {
                    Point point =
                        polyline.Points[index];

                    polyline.Points[index] =
                        new Point(
                            point.X + shiftX,
                            point.Y + shiftY);
                }
                break;
        }
    }

    private static double GetCanvasLeft(
        FrameworkElement element)
    {
        double value =
            Canvas.GetLeft(element);

        return double.IsNaN(value)
            ? 0
            : value;
    }

    private static double GetCanvasTop(
        FrameworkElement element)
    {
        double value =
            Canvas.GetTop(element);

        return double.IsNaN(value)
            ? 0
            : value;
    }

    private void DeleteSelectedShape_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (selectedShape == null)
        {
            MessageBox.Show(
                "Сначала выберите фигуру.",
                "Удаление фигуры",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            return;
        }

        Shape shapeToDelete =
            selectedShape;

        DeselectShape();

        DrawingCanvas.Children.Remove(
            shapeToDelete);

        Focus();
    }

    private void ClearShapes_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (DrawingCanvas.Children.Count == 0)
            return;

        MessageBoxResult result =
            MessageBox.Show(
                "Удалить все фигуры с карты?",
                "Очистка фигур",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes)
            return;

        DeselectShape();

        DrawingCanvas.Children.Clear();

        Focus();
    }
}