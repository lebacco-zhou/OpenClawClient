using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace OpenClawClient.UI.Views;

/// <summary>
/// 图片全屏查看器
/// </summary>
public partial class ImageViewerWindow : Window
{
    private BitmapSource? _imageSource;
    private double _zoom = 1.0;
    private double _minZoom = 0.1;
    private double _maxZoom = 5.0;
    private Point _startPoint;
    private bool _isDragging;
    private TranslateTransform? _translateTransform;
    private ScaleTransform? _scaleTransform;
    private TransformGroup? _transformGroup;
    private string? _fileName;

    public ImageViewerWindow()
    {
        InitializeComponent();
        InitializeTransforms();
    }

    public ImageViewerWindow(BitmapSource image, string? fileName = null) : this()
    {
        LoadImage(image, fileName);
    }

    private void InitializeTransforms()
    {
        _transformGroup = new TransformGroup();
        _scaleTransform = new ScaleTransform();
        _translateTransform = new TranslateTransform();
        
        _transformGroup.Children.Add(_scaleTransform);
        _transformGroup.Children.Add(_translateTransform);
        
        MainImage.RenderTransform = _transformGroup;
        MainImage.RenderTransformOrigin = new Point(0.5, 0.5);
    }

    public void LoadImage(BitmapSource image, string? fileName = null)
    {
        _imageSource = image;
        MainImage.Source = image;
        _fileName = fileName;
        
        if (!string.IsNullOrEmpty(fileName))
        {
            FileNameText.Text = Path.GetFileName(fileName);
        }
        
        ResetZoom();
    }

    private void UpdateZoom()
    {
        if (_scaleTransform != null)
        {
            _scaleTransform.ScaleX = _zoom;
            _scaleTransform.ScaleY = _zoom;
            ZoomText.Text = $"{_zoom * 100:F0}%";
        }
    }

    private void ZoomIn()
    {
        _zoom = Math.Min(_zoom * 1.2, _maxZoom);
        UpdateZoom();
    }

    private void ZoomOut()
    {
        _zoom = Math.Max(_zoom / 1.2, _minZoom);
        UpdateZoom();
    }

    private void ResetZoom()
    {
        _zoom = 1.0;
        if (_translateTransform != null)
        {
            _translateTransform.X = 0;
            _translateTransform.Y = 0;
        }
        UpdateZoom();
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Escape:
                Close();
                break;
            case Key.Add:
            case Key.OemPlus:
                if (Keyboard.Modifiers == ModifierKeys.Control)
                {
                    ZoomIn();
                    e.Handled = true;
                }
                break;
            case Key.Subtract:
            case Key.OemMinus:
                if (Keyboard.Modifiers == ModifierKeys.Control)
                {
                    ZoomOut();
                    e.Handled = true;
                }
                break;
            case Key.D0:
                if (Keyboard.Modifiers == ModifierKeys.Control)
                {
                    ResetZoom();
                    e.Handled = true;
                }
                break;
            case Key.S:
                if (Keyboard.Modifiers == ModifierKeys.Control)
                {
                    Save();
                    e.Handled = true;
                }
                break;
        }
    }

    private void ImageBorder_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (Keyboard.Modifiers == ModifierKeys.Control)
        {
            if (e.Delta > 0)
                ZoomIn();
            else
                ZoomOut();
            e.Handled = true;
        }
    }

    private void ImageBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _isDragging = true;
        _startPoint = e.GetPosition(ImageBorder);
        ImageBorder.CaptureMouse();
        ImageBorder.Cursor = Cursors.Hand;
    }

    private void ImageBorder_MouseMove(object sender, MouseEventArgs e)
    {
        if (_isDragging && _translateTransform != null)
        {
            var currentPosition = e.GetPosition(ImageBorder);
            _translateTransform.X += currentPosition.X - _startPoint.X;
            _translateTransform.Y += currentPosition.Y - _startPoint.Y;
            _startPoint = currentPosition;
        }
    }

    private void ImageBorder_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _isDragging = false;
        ImageBorder.ReleaseMouseCapture();
        ImageBorder.Cursor = Cursors.Arrow;
    }

    private void ZoomIn_Click(object sender, RoutedEventArgs e) => ZoomIn();
    private void ZoomOut_Click(object sender, RoutedEventArgs e) => ZoomOut();
    private void ResetZoom_Click(object sender, RoutedEventArgs e) => ResetZoom();

    private void Save_Click(object sender, RoutedEventArgs e) => Save();

    private void Save()
    {
        if (_imageSource == null)
            return;

        var dialog = new SaveFileDialog
        {
            Filter = "PNG 图片 (*.png)|*.png|JPEG 图片 (*.jpg;*.jpeg)|*.jpg;*.jpeg|所有文件 (*.*)|*.*",
            FileName = !string.IsNullOrEmpty(_fileName) ? Path.GetFileName(_fileName) : "image"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                BitmapEncoder encoder = dialog.FilterIndex switch
                {
                    2 => new JpegBitmapEncoder(),
                    _ => new PngBitmapEncoder()
                };

                encoder.Frames.Add(BitmapFrame.Create(_imageSource));

                using (var stream = new FileStream(dialog.FileName, FileMode.Create))
                {
                    encoder.Save(stream);
                }

                MessageBox.Show($"图片已保存到：\n{dialog.FileName}", "保存成功",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存失败：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
