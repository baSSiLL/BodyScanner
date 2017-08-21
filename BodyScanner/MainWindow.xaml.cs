using System;
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
                case nameof(AppViewModel.ScanBitmap):
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
            UpdateBitmap(ViewModel.ScanBitmap, scanBitmapHolder, scanImage);
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

            // TODO: Invert Y and align with floor normal in MeshConverter instead?
            var invertY = new ScaleTransform3D(1, -1, 1);
            var alignWithFloor = new RotateTransform3D(GetFloorAlignment());

            var rotation = new AxisAngleRotation3D(new Vector3D(0, 1, 0), 0);
            var animation = (AnimationTimeline)FindResource("AngleAnimation");
            rotation.BeginAnimation(AxisAngleRotation3D.AngleProperty, animation);
            var rotate = new RotateTransform3D(rotation);
            model.Transform = new Transform3DGroup
            {
                Children = { translate, invertY, alignWithFloor, rotate }
            };
        }

        private Rotation3D GetFloorAlignment()
        {
            if (Math.Abs(ViewModel.FloorNormal.Y - 1) < 1e-4)
                return Rotation3D.Identity;

            var axis = Vector3D.CrossProduct(ViewModel.FloorNormal, new Vector3D(0, 1, 0));
            var angle = Math.Asin(axis.Length);
            return new AxisAngleRotation3D(axis, angle * 180 / Math.PI);
        }
    }
}
