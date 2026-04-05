using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.Interactivity;
using Avalonia.Input;
using Avalonia.Styling;
using CircleIntersectionApp.ViewModels;
using ReactiveUI;
using Avalonia.ReactiveUI;
using System.ComponentModel;
using CircleIntersectionApp.Models;

namespace CircleIntersectionApp.Views;

public partial class MainWindow : ReactiveWindow<MainWindowViewModel>
{
    private readonly Dictionary<string, Control> _animatedElements = new();
    private readonly Random _random = new();
    private double _zoomLevel = 1.0;
    private double _panOffsetX = 0;
    private double _panOffsetY = 0;
    private bool _isPanning = false;
    private Point _lastMousePosition;
    private bool _autoFitEnabled = true;

    public MainWindow()
    {
        InitializeComponent();

        this.WhenActivated(_ =>
        {
            if (DataContext is MainWindowViewModel vm)
            {
                vm.PropertyChanged += ViewModel_PropertyChanged;
            }
        });

        this.Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            DrawGraph(vm);
        }
    }

    private bool _isAnimating = false;

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is MainWindowViewModel vm &&
            (e.PropertyName == nameof(MainWindowViewModel.IsValidData) ||
             e.PropertyName == nameof(MainWindowViewModel.OutputResult) ||
             e.PropertyName == nameof(MainWindowViewModel.CurrentCircleData)))
        {
            // Reset auto-fit when data changes
            if (e.PropertyName == nameof(MainWindowViewModel.CurrentCircleData))
            {
                _autoFitEnabled = true;
            }

            if (!_isAnimating)
            {
                _isAnimating = true;
                Dispatcher.UIThread.Post(async () =>
                {
                    try
                    {
                        await DrawGraphAnimated(vm);
                    }
                    finally
                    {
                        _isAnimating = false;
                    }
                });
            }
        }
    }

    private void GraphCanvas_SizeChanged(object? sender, SizeChangedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            DrawGraph(vm);
        }
    }

    private void GraphCanvas_PointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        // Zoom with mouse wheel
        double zoomFactor = e.Delta.Y > 0 ? 1.1 : 0.9;
        _zoomLevel *= zoomFactor;

        // Limit zoom levels
        _zoomLevel = Math.Max(0.1, Math.Min(5.0, _zoomLevel));

        if (DataContext is MainWindowViewModel vm)
        {
            DrawGraph(vm);
        }

        e.Handled = true;
    }

    private void GraphCanvas_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            _isPanning = true;
            _lastMousePosition = e.GetPosition(GraphCanvas);
            e.Handled = true;
        }
    }

    private void GraphCanvas_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (_isPanning)
        {
            var currentPosition = e.GetPosition(GraphCanvas);
            var deltaX = currentPosition.X - _lastMousePosition.X;
            var deltaY = currentPosition.Y - _lastMousePosition.Y;

            _panOffsetX += deltaX / _zoomLevel;
            _panOffsetY += deltaY / _zoomLevel;

            _lastMousePosition = currentPosition;

            if (DataContext is MainWindowViewModel vm)
            {
                DrawGraph(vm);
            }

            e.Handled = true;
        }
    }

    private void GraphCanvas_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _isPanning = false;
        e.Handled = true;
    }

    private void AutoFitGraph(CircleData c)
    {
        // Calculate the bounds of the circles
        double xMin = Math.Min(c.X1 - c.R1, c.X2 - c.R2);
        double xMax = Math.Max(c.X1 + c.R1, c.X2 + c.R2);
        double yMin = Math.Min(c.Y1 - c.R1, c.Y2 - c.R2);
        double yMax = Math.Max(c.Y1 + c.R1, c.Y2 + c.R2);

        // Add some padding
        double padding = Math.Max(Math.Max((xMax - xMin) * 0.1, (yMax - yMin) * 0.1), 2.0);
        xMin -= padding;
        xMax += padding;
        yMin -= padding;
        yMax += padding;

        // Calculate center of the graph
        double centerX = (xMin + xMax) / 2;
        double centerY = (yMin + yMax) / 2;

        // Set pan offset to center the graph
        _panOffsetX = -centerX;
        _panOffsetY = -centerY;

        // Calculate zoom level to fit the graph in the canvas
        double graphWidth = xMax - xMin;
        double graphHeight = yMax - yMin;
        double canvasWidth = GraphCanvas.Bounds.Width;
        double canvasHeight = GraphCanvas.Bounds.Height;

        if (canvasWidth > 0 && canvasHeight > 0)
        {
            double scaleX = canvasWidth / graphWidth;
            double scaleY = canvasHeight / graphHeight;
            double optimalScale = Math.Min(scaleX, scaleY) * 0.8; // 80% to leave some margin

            _zoomLevel = Math.Max(0.1, Math.Min(2.0, optimalScale)); // Limit zoom between 0.1 and 2.0
        }
    }    private async Task DrawGraphAnimated(MainWindowViewModel vm)
    {
        if (GraphCanvas is null || vm is null)
            return;

        try
        {
            // Fade out existing elements
            foreach (var element in GraphCanvas.Children.ToList())
            {
                if (element is Control control)
                {
                    await AnimateElement(control, 0, 0.3);
                }
            }

            // Clear and redraw
            GraphCanvas.Children.Clear();
            DrawGraph(vm);

            // Fade in new elements
            foreach (var element in GraphCanvas.Children.ToList())
            {
                if (element is Control control)
                {
                    control.Opacity = 0;
                    await AnimateElement(control, 1, 0.5);
                }
            }
        }
        catch (Exception ex)
        {
            // Log error or handle gracefully
            System.Diagnostics.Debug.WriteLine($"Error in DrawGraphAnimated: {ex.Message}");
        }
    }

    private async System.Threading.Tasks.Task AnimateElement(Control element, double targetOpacity, double duration)
    {
        var animation = new Animation
        {
            Duration = TimeSpan.FromSeconds(duration),
            Easing = new CubicEaseOut(),
            Children =
            {
                new KeyFrame
                {
                    KeyTime = TimeSpan.FromSeconds(duration),
                    Setters = { new Setter(Visual.OpacityProperty, targetOpacity) }
                }
            }
        };

        await animation.RunAsync(element);
    }

    private void DrawGraph(MainWindowViewModel vm)
    {
        if (GraphCanvas is null)
            return;

        GraphCanvas.Children.Clear();

        if (!vm.IsValidData)
        {
            DrawEmptyState();
            return;
        }

        var c = vm.CurrentCircleData;
        if (c.R1 <= 0 || c.R2 <= 0)
        {
            DrawEmptyState();
            return;
        }

        // Auto-center and fit graph on first load or when data changes significantly
        if (_autoFitEnabled)
        {
            AutoFitGraph(c);
            _autoFitEnabled = false; // Disable auto-fit after first positioning
        }

        // Calculate adaptive bounds based on zoom and circles
        double basePadding = 3.0 / _zoomLevel; // Adaptive padding
        double xMin = Math.Min(c.X1 - c.R1, c.X2 - c.R2) - basePadding;
        double xMax = Math.Max(c.X1 + c.R1, c.X2 + c.R2) + basePadding;
        double yMin = Math.Min(c.Y1 - c.R1, c.Y2 - c.R2) - basePadding;
        double yMax = Math.Max(c.Y1 + c.R1, c.Y2 + c.R2) + basePadding;

        // Apply pan offset
        xMin += _panOffsetX;
        xMax += _panOffsetX;
        yMin += _panOffsetY;
        yMax += _panOffsetY;

        double width = GraphCanvas.Bounds.Width;
        double height = GraphCanvas.Bounds.Height;
        if (width <= 0 || height <= 0)
            return;

        const double margin = 40;
        double dx = Math.Max(1.0, xMax - xMin);
        double dy = Math.Max(1.0, yMax - yMin);
        double scale = Math.Min((width - 2 * margin) / dx, (height - 2 * margin) / dy) * _zoomLevel;
        double ConvX(double x) => margin + (x - xMin) * scale;
        double ConvY(double y) => height - margin - (y - yMin) * scale;

        // Draw grid
        DrawGrid(width, height, ConvX, ConvY, xMin, xMax, yMin, yMax);

        // Draw circles with gradients and shadows
        DrawCircleWithEffects(c.X1, c.Y1, c.R1, ConvX, ConvY, scale, "#3498db", "#2980b9", "Circle1");
        DrawCircleWithEffects(c.X2, c.Y2, c.R2, ConvX, ConvY, scale, "#e74c3c", "#c0392b", "Circle2");

        // Draw intersection points with glow effect
        if (c.CirclesIntersect())
        {
            var ip = c.GetIntersectionPoints();
            if (ip.HasValue)
            {
                DrawIntersectionPoint(ip.Value.x1, ip.Value.y1, ConvX, ConvY, scale, "Point1");
                DrawIntersectionPoint(ip.Value.x2, ip.Value.y2, ConvX, ConvY, scale, "Point2");
            }
        }

        // Draw connecting line between centers
        DrawCenterLine(c.X1, c.Y1, c.X2, c.Y2, ConvX, ConvY);
    }

    private void DrawEmptyState()
    {
        var text = new TextBlock
        {
            Text = "Введите данные для визуализации",
            FontSize = 16,
            Foreground = new SolidColorBrush(Color.Parse("#888888")),
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };

        Canvas.SetLeft(text, GraphCanvas.Bounds.Width / 2 - 100);
        Canvas.SetTop(text, GraphCanvas.Bounds.Height / 2 - 10);
        GraphCanvas.Children.Add(text);
    }

    private void DrawGrid(double width, double height, Func<double, double> convX, Func<double, double> convY,
                         double xMin, double xMax, double yMin, double yMax)
    {
        var gridBrush = new SolidColorBrush(Color.Parse("#404040"), 0.4);

        // Calculate adaptive step based on zoom level
        double range = Math.Max(xMax - xMin, yMax - yMin);
        double baseStep = Math.Pow(10, Math.Floor(Math.Log10(range / 10)));
        double step = baseStep;

        // Adjust step based on zoom
        if (_zoomLevel > 2) step = baseStep / 2;
        else if (_zoomLevel < 0.5) step = baseStep * 2;

        // Vertical grid lines with labels
        for (double x = Math.Ceiling(xMin / step) * step; x <= xMax; x += step)
        {
            var line = new Line
            {
                StartPoint = new Point(convX(x), 0),
                EndPoint = new Point(convX(x), height),
                Stroke = gridBrush,
                StrokeThickness = 1
            };
            GraphCanvas.Children.Add(line);

            // X-axis labels
            if (Math.Abs(x) > 0.01) // Don't label the origin
            {
                var label = new TextBlock
                {
                    Text = x.ToString(_zoomLevel > 1 ? "F1" : "F0"),
                    FontSize = Math.Max(8, Math.Min(12, 10 * _zoomLevel)),
                    Foreground = new SolidColorBrush(Color.Parse("#b0b0b0"))
                };
                Canvas.SetLeft(label, convX(x) - 15);
                Canvas.SetTop(label, convY(0) + 5);
                GraphCanvas.Children.Add(label);
            }
        }

        // Horizontal grid lines with labels
        for (double y = Math.Ceiling(yMin / step) * step; y <= yMax; y += step)
        {
            var line = new Line
            {
                StartPoint = new Point(0, convY(y)),
                EndPoint = new Point(width, convY(y)),
                Stroke = gridBrush,
                StrokeThickness = 1
            };
            GraphCanvas.Children.Add(line);

            // Y-axis labels
            if (Math.Abs(y) > 0.01) // Don't label the origin
            {
                var label = new TextBlock
                {
                    Text = y.ToString(_zoomLevel > 1 ? "F1" : "F0"),
                    FontSize = Math.Max(8, Math.Min(12, 10 * _zoomLevel)),
                    Foreground = new SolidColorBrush(Color.Parse("#b0b0b0"))
                };
                Canvas.SetLeft(label, convX(0) - 35);
                Canvas.SetTop(label, convY(y) - 8);
                GraphCanvas.Children.Add(label);
            }
        }

        // Main axes
        var xAxis = new Line
        {
            StartPoint = new Point(0, convY(0)),
            EndPoint = new Point(width, convY(0)),
            Stroke = new SolidColorBrush(Color.Parse("#888888")),
            StrokeThickness = 2
        };
        var yAxis = new Line
        {
            StartPoint = new Point(convX(0), 0),
            EndPoint = new Point(convX(0), height),
            Stroke = new SolidColorBrush(Color.Parse("#888888")),
            StrokeThickness = 2
        };
        GraphCanvas.Children.Add(xAxis);
        GraphCanvas.Children.Add(yAxis);

        // Axis labels
        var xLabel = new TextBlock
        {
            Text = "X",
            FontSize = 14,
            Foreground = new SolidColorBrush(Color.Parse("#cccccc")),
            FontWeight = FontWeight.Bold
        };
        Canvas.SetLeft(xLabel, width - 20);
        Canvas.SetTop(xLabel, convY(0) - 25);
        GraphCanvas.Children.Add(xLabel);

        var yLabel = new TextBlock
        {
            Text = "Y",
            FontSize = 14,
            Foreground = new SolidColorBrush(Color.Parse("#cccccc")),
            FontWeight = FontWeight.Bold
        };
        Canvas.SetLeft(yLabel, convX(0) + 5);
        Canvas.SetTop(yLabel, 10);
        GraphCanvas.Children.Add(yLabel);

        // Origin label
        var originLabel = new TextBlock
        {
            Text = "(0,0)",
            FontSize = 10,
            Foreground = Brushes.Black,
            FontWeight = FontWeight.Bold
        };
        Canvas.SetLeft(originLabel, convX(0) + 5);
        Canvas.SetTop(originLabel, convY(0) + 5);
        GraphCanvas.Children.Add(originLabel);
    }

    private void DrawCircleWithEffects(double cx, double cy, double r, Func<double, double> convX, Func<double, double> convY,
                                      double scale, string color1, string color2, string elementId)
    {
        // Draw circle as mathematical curve (outline only)
        var circle = new Ellipse
        {
            Width = 2 * r * scale,
            Height = 2 * r * scale,
            Stroke = new SolidColorBrush(Color.Parse(color1)),
            StrokeThickness = 3,
            Fill = Brushes.Transparent
        };

        Canvas.SetLeft(circle, convX(cx) - r * scale);
        Canvas.SetTop(circle, convY(cy) - r * scale);
        GraphCanvas.Children.Add(circle);

        // Center point
        var center = new Ellipse
        {
            Width = 6,
            Height = 6,
            Fill = new SolidColorBrush(Color.Parse(color1)),
            Stroke = new SolidColorBrush(Color.Parse("#333333")),
            StrokeThickness = 1
        };

        Canvas.SetLeft(center, convX(cx) - 3);
        Canvas.SetTop(center, convY(cy) - 3);
        GraphCanvas.Children.Add(center);

        // Center coordinates label
        var centerLabel = new TextBlock
        {
            Text = $"({cx:F1}, {cy:F1})",
            FontSize = 10,
            Foreground = new SolidColorBrush(Color.Parse(color1)),
            FontWeight = FontWeight.Bold
        };

        Canvas.SetLeft(centerLabel, convX(cx) + 5);
        Canvas.SetTop(centerLabel, convY(cy) - 15);
        GraphCanvas.Children.Add(centerLabel);

        // Radius label
        var radiusLabel = new TextBlock
        {
            Text = $"r = {r:F1}",
            FontSize = 12,
            Foreground = new SolidColorBrush(Color.Parse(color1)),
            FontWeight = FontWeight.Bold
        };

        Canvas.SetLeft(radiusLabel, convX(cx) + r * scale * 0.7);
        Canvas.SetTop(radiusLabel, convY(cy) - r * scale * 0.3);
        GraphCanvas.Children.Add(radiusLabel);

        _animatedElements[elementId] = circle;
    }

    private void DrawIntersectionPoint(double x, double y, Func<double, double> convX, Func<double, double> convY,
                                      double scale, string elementId)
    {
        // Mathematical intersection point
        var point = new Ellipse
        {
            Width = 8,
            Height = 8,
            Fill = new SolidColorBrush(Color.Parse("#f39c12")),
            Stroke = new SolidColorBrush(Color.Parse("#333333")),
            StrokeThickness = 2
        };

        Canvas.SetLeft(point, convX(x) - 4);
        Canvas.SetTop(point, convY(y) - 4);
        GraphCanvas.Children.Add(point);

        // Point coordinates label
        var pointLabel = new TextBlock
        {
            Text = $"({x:F2}, {y:F2})",
            FontSize = 10,
            Foreground = new SolidColorBrush(Color.Parse("#f39c12")),
            FontWeight = FontWeight.Bold,
            Background = new SolidColorBrush(Color.Parse("#1a1a1a"))
        };

        Canvas.SetLeft(pointLabel, convX(x) + 8);
        Canvas.SetTop(pointLabel, convY(y) - 20);
        GraphCanvas.Children.Add(pointLabel);

        _animatedElements[elementId] = point;
    }

    private void DrawCenterLine(double x1, double y1, double x2, double y2, Func<double, double> convX, Func<double, double> convY)
    {
        // Distance between centers
        double distance = Math.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1));

        var line = new Line
        {
            StartPoint = new Point(convX(x1), convY(y1)),
            EndPoint = new Point(convX(x2), convY(y2)),
            Stroke = new SolidColorBrush(Color.Parse("#95a5a6")),
            StrokeThickness = 2,
            StrokeDashArray = new Avalonia.Collections.AvaloniaList<double> { 5, 5 }
        };
        GraphCanvas.Children.Add(line);

        // Distance label
        var midX = (x1 + x2) / 2;
        var midY = (y1 + y2) / 2;
        var distanceLabel = new TextBlock
        {
            Text = $"d = {distance:F2}",
            FontSize = 10,
            Foreground = new SolidColorBrush(Color.Parse("#95a5a6")),
            FontWeight = FontWeight.Bold,
            Background = Brushes.White
        };

        Canvas.SetLeft(distanceLabel, convX(midX) - 25);
        Canvas.SetTop(distanceLabel, convY(midY) - 15);
        GraphCanvas.Children.Add(distanceLabel);
    }

    private void ResetViewButton_Click(object? sender, RoutedEventArgs e)
    {
        _zoomLevel = 1.0;
        _panOffsetX = 0;
        _panOffsetY = 0;
        _autoFitEnabled = true; // Re-enable auto-fit

        if (DataContext is MainWindowViewModel vm)
        {
            DrawGraph(vm);
        }
    }    private void ZoomInButton_Click(object? sender, RoutedEventArgs e)
    {
        _zoomLevel *= 1.2;
        _zoomLevel = Math.Min(5.0, _zoomLevel);

        if (DataContext is MainWindowViewModel vm)
        {
            DrawGraph(vm);
        }
    }

    private void ZoomOutButton_Click(object? sender, RoutedEventArgs e)
    {
        _zoomLevel /= 1.2;
        _zoomLevel = Math.Max(0.1, _zoomLevel);

        if (DataContext is MainWindowViewModel vm)
        {
            DrawGraph(vm);
        }
    }
}
