using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

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
                case nameof(AppViewModel.DepthBitmapBgra):
                    UpdateDepthBitmap();
                    break;
                case nameof(AppViewModel.ScanBitmapBgra):
                    UpdateScanBitmap();
                    break;
            }
        }

        private void UpdateDepthBitmap()
        {
            if (ViewModel.DepthBitmapBgra != null)
            {
                var changed = depthBitmapHolder.WritePixels(ViewModel.DepthBitmapWidth, ViewModel.DepthBitmapHeight, ViewModel.DepthBitmapBgra);
                if (changed)
                {
                    depthImage.Source = depthBitmapHolder.Bitmap;
                }
            }
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
    }
}
