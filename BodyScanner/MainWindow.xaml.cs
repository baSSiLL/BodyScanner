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
        private WriteableBitmap depthBitmap;

        public MainWindow()
        {
            InitializeComponent();

            Loaded += MainWindow_Loaded;
        }

        private AppViewModel ViewModel => (AppViewModel)DataContext;

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            personName.Focus();

            depthBitmap = new WriteableBitmap(ViewModel.DepthBitmapWidth, ViewModel.DepthBitmapHeight, 96, 96, PixelFormats.Bgr32, null);
            depthImage.Source = depthBitmap;

            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(AppViewModel.DepthBitmap):
                    UpdateDepthBitmap();
                    break;
            }
        }

        private void UpdateDepthBitmap()
        {
            if (ViewModel.DepthBitmap != null)
            {
                var rect = new Int32Rect(0, 0, depthBitmap.PixelWidth, depthBitmap.PixelHeight);
                depthBitmap.Lock();
                depthBitmap.WritePixels(rect, ViewModel.DepthBitmap, depthBitmap.PixelWidth * depthBitmap.Format.BitsPerPixel / 8, 0);
                depthBitmap.Unlock();
            }
        }
    }
}
