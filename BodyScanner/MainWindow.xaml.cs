using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Media.Media3D;

namespace BodyScanner
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly WriteableBitmapHolder depthBitmapHolder = new WriteableBitmapHolder();
        private readonly WriteableBitmapHolder scanBitmapHolder = new WriteableBitmapHolder();

        public MainWindow()
        {
            InitializeComponent();

            Loaded += MainWindow_Loaded;
        }

        private AppViewModel ViewModel => (AppViewModel)DataContext;

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            personName.Focus();

            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(AppViewModel.DepthBitmap):
                    UpdateDepthBitmap();
                    break;
                case nameof(AppViewModel.ScanBitmapBgra):
                    UpdateScanBitmap();
                    break;
                case nameof(AppViewModel.Body3DModel):
                    SetupViewportTransforms();
                    break;
            }
        }

        private void UpdateDepthBitmap()
        {
            UpdateBitmap(ViewModel.DepthBitmap, depthBitmapHolder, depthImage);
        }

        private void UpdateScanBitmap()
        {
            if (ViewModel.ScanBitmapBgra != null)
            {
                var changed = scanBitmapHolder.WritePixels(ViewModel.ScanBitmapWidth, ViewModel.ScanBitmapHeight, ViewModel.ScanBitmapBgra);
                if (changed)
                {
                    scanImage.Source = scanBitmapHolder.Bitmap;
                }
            }
        }

        private void UpdateBitmap(ThreadSafeBitmap bitmap, WriteableBitmapHolder holder, Image image)
        {
            if (holder == null || bitmap == null)
                return;

            var changed = false;
            bitmap.Access(bitmapData =>
                changed = holder.WritePixels(bitmap.Width, bitmap.Height, bitmapData));

            if (changed)
            {
                image.Source = holder.Bitmap;
            }
        }

        private void SetupViewportTransforms()
        {
            var geometry = ViewModel.Body3DModel;
            if (geometry == null)
                return;

            var center = new Vector3D(
                (geometry.Bounds.X + geometry.Bounds.SizeX) / 2,
                0,
                (geometry.Bounds.Z + geometry.Bounds.SizeZ) / 2);
            var translate = new TranslateTransform3D(-center);

            var rotation = new AxisAngleRotation3D(new Vector3D(0, 1, 0), 0);
            var animation = (AnimationTimeline)FindResource("AngleAnimation");
            rotation.BeginAnimation(AxisAngleRotation3D.AngleProperty, animation);
            var rotate = new RotateTransform3D(rotation);
            model.Transform = new Transform3DGroup
            {
                Children = { translate, rotate }
            };
        }
    }
}
