using Microsoft.Kinect;
using System;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;

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
            var pixelCount = depthFrameDesc.Width * depthFrameDesc.Height;
            frameData = new ushort[pixelCount];
            Bitmap = new ThreadSafeBitmap(depthFrameDesc.Width, depthFrameDesc.Height);

            reader = sensor.DepthFrameSource.OpenReader();
            reader.FrameArrived += Reader_FrameArrived;
        }

        public bool Mirror
        {
            get { return mirror; }
            set { mirror = value; }
        }
        private volatile bool mirror;

        public ThreadSafeBitmap Bitmap { get; }

        public event EventHandler BitmapUpdated;

        private void RaiseBitmapUpdated()
        {
            BitmapUpdated?.Invoke(this, EventArgs.Empty);
        }

        private void Reader_FrameArrived(object sender, DepthFrameArrivedEventArgs e)
        {
            var frame = e.FrameReference.AcquireFrame();
            if (frame != null)
            {
                using (frame)
                {
                    frame.CopyFrameDataToArray(frameData);
                }

                var fillAction = mirror ? new Action<byte[]>(FillBitmap) : new Action<byte[]>(UnmirrorAndFillBitmap);
                Task.Run(() => Bitmap.Access(fillAction)).
                    ContinueWith(_ => AfterRender());
            }
        }

        private void FillBitmap(byte[] bitmapData)
        {
            var iBitmap = 0;
            for (var iDepth = 0; iDepth < frameData.Length; iDepth++)
            {
                var color = converter.Convert(frameData[iDepth]);
                WritePixel(bitmapData, ref iBitmap, ref color);
            }
        }

        private void UnmirrorAndFillBitmap(byte[] bitmapData)
        {
            var iBitmap = 0;
            while (iBitmap < bitmapData.Length)
            {
                var iDepth = iBitmap / 4 + Bitmap.Width - 1;
                for (var x = 0; x < Bitmap.Width; x++)
                {
                    var color = converter.Convert(frameData[iDepth--]);
                    WritePixel(bitmapData, ref iBitmap, ref color);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WritePixel(byte[] bitmapData, ref int iBitmap, ref Color color)
        {
            bitmapData[iBitmap++] = color.B;
            bitmapData[iBitmap++] = color.G;
            bitmapData[iBitmap++] = color.R;
            bitmapData[iBitmap++] = color.A;
        }

        private void AfterRender()
        {
            syncContext.Post(RaiseBitmapUpdated);
        }
    }
}
