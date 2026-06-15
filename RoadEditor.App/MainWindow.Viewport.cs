using System;
using System.Windows;
using System.Windows.Input;

namespace RoadEditor.App;

public partial class MainWindow
{
    private double zoom = 1.0;

    private bool isPanning;

    private Point panStartPoint;

    private double panStartHorizontalOffset;

    private double panStartVerticalOffset;

    private void Undo_Click(
        object sender,
        RoutedEventArgs e)
    {
        Undo();
    }

    private void Redo_Click(
        object sender,
        RoutedEventArgs e)
    {
        Redo();
    }

    private void ZoomIn_Click(
        object sender,
        RoutedEventArgs e)
    {
        ApplyZoom(zoom + 0.1);
    }

    private void ZoomOut_Click(
        object sender,
        RoutedEventArgs e)
    {
        ApplyZoom(zoom - 0.1);
    }

    private void ResetZoom_Click(
        object sender,
        RoutedEventArgs e)
    {
        ApplyZoom(1.0);
    }

    private void ApplyZoom(double value)
    {
        zoom = Math.Clamp(
            value,
            0.5,
            2.5);

        MapScaleTransform.ScaleX = zoom;
        MapScaleTransform.ScaleY = zoom;

        ZoomInfoText.Text =
            $"Масштаб: {Math.Round(zoom * 100)}%";

        Focus();
    }

    private void MapScrollViewer_PreviewMouseWheel(
        object sender,
        MouseWheelEventArgs e)
    {
        double step =
            e.Delta > 0
                ? 0.1
                : -0.1;

        ApplyZoom(zoom + step);

        e.Handled = true;
    }

    private void MapScrollViewer_PreviewMouseRightButtonDown(
        object sender,
        MouseButtonEventArgs e)
    {
        isPanning = true;

        panStartPoint =
            e.GetPosition(MapScrollViewer);

        panStartHorizontalOffset =
            MapScrollViewer.HorizontalOffset;

        panStartVerticalOffset =
            MapScrollViewer.VerticalOffset;

        MapScrollViewer.CaptureMouse();

        MapScrollViewer.Cursor =
            Cursors.Hand;

        e.Handled = true;
    }

    private void MapScrollViewer_PreviewMouseRightButtonUp(
        object sender,
        MouseButtonEventArgs e)
    {
        StopPanning();

        e.Handled = true;
    }

    private void MapScrollViewer_PreviewMouseMove(
        object sender,
        MouseEventArgs e)
    {
        if (!isPanning ||
            e.RightButton != MouseButtonState.Pressed)
        {
            return;
        }

        Point currentPoint =
            e.GetPosition(MapScrollViewer);

        Vector shift =
            currentPoint - panStartPoint;

        MapScrollViewer.ScrollToHorizontalOffset(
            panStartHorizontalOffset - shift.X);

        MapScrollViewer.ScrollToVerticalOffset(
            panStartVerticalOffset - shift.Y);

        e.Handled = true;
    }

    private void StopPanning()
    {
        if (!isPanning)
            return;

        isPanning = false;

        MapScrollViewer.ReleaseMouseCapture();

        MapScrollViewer.Cursor =
            Cursors.Arrow;
    }
}
