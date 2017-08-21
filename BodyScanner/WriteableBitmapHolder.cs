using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BodyScanner
{
    class WriteableBitmapHolder
    {
        public WriteableBitmap Bitmap { get; private set; }

        public bool WritePixels(int width, int height, Array data)
        {
            var bitmapChanged = EnsureBitmapSize(width, height);

            var rect = new Int32Rect(0, 0, width, height);
            Bitmap.WritePixels(rect, data, width * Bitmap.Format.BitsPerPixel / 8, 0);

            return bitmapChanged;
        }

        private bool EnsureBitmapSize(int width, int height)
        {
            if (Bitmap != null && Bitmap.PixelWidth == width && Bitmap.PixelHeight == height)
                return false;

            Bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgr32, null);
            return true;
        }
    }
}
