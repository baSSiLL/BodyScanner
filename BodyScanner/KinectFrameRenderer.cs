using Microsoft.Kinect;
using System;
using System.Diagnostics.Contracts;
using System.Threading;
using System.Threading.Tasks;

namespace BodyScanner
{
    class KinectFrameRenderer
    {
        private readonly DepthToColorConverter converter;
        private readonly DepthFrameReader reader;
        private readonly ushort[] frameData;
        private readonly SynchronizationContext syncContext;

        public KinectFrameRenderer(KinectSensor sensor, DepthToColorConverter converter)
        {
            Contract.Requires(sensor != null);
            Contract.Requires(converter != null);

            this.syncContext = SynchronizationContext.Current;
            this.converter = converter;

            var depthFrameDesc = sensor.DepthFrameSource.FrameDescription;
            BitmapWidth = depthFrameDesc.Width;
            BitmapHeight = depthFrameDesc.Height;
            var pixelCount = BitmapWidth * BitmapHeight;
            frameData = new ushort[pixelCount];
            Bitmap = new byte[pixelCount * 4];

            reader = sensor.DepthFrameSource.OpenReader();
            reader.FrameArrived += Reader_FrameArrived;
        }

        public byte[] Bitmap { get; }

        public int BitmapWidth { get; }

        public int BitmapHeight { get; }

        public event EventHandler BitmapUpdated;

        private void Reader_FrameArrived(object sender, DepthFrameArrivedEventArgs e)
        {
            var frame = e.FrameReference.AcquireFrame();
            if (frame != null)
            {
                using (frame)
                    frame.CopyFrameDataToArray(frameData);

                Task.Run(() => FillBitmap()).
                    ContinueWith(_ => syncContext.Post(__ => BitmapUpdated?.Invoke(this, EventArgs.Empty), null));
            }
        }

        private void FillBitmap()
        {
            var iBitmap = 0;
            for (var iDepth = 0; iDepth < frameData.Length; iDepth++)
            {
                var color = converter.Convert(frameData[iDepth]);
                Bitmap[iBitmap++] = color.B;
                Bitmap[iBitmap++] = color.G;
                Bitmap[iBitmap++] = color.R;
                Bitmap[iBitmap++] = color.A;
            }
        }
    }
}
