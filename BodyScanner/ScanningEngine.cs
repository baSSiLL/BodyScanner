using Microsoft.Kinect;
using System;
using System.Diagnostics.Contracts;
using System.Threading.Tasks;

namespace BodyScanner
{
    internal class ScanningEngine
    {
        private readonly KinectSensor sensor;
        private readonly Func<ReconstructionController> controllerFactory;

        public ScanningEngine(KinectSensor sensor, Func<ReconstructionController> controllerFactory)
        {
            Contract.Requires(sensor != null);
            Contract.Requires(controllerFactory != null);

            this.sensor = sensor;
            this.controllerFactory = controllerFactory;
        }

        public int[] ScanBitmap { get; private set; }

        public int ScanBitmapWidth { get; private set; }

        public int ScanBitmapHeight { get; private set; }

        public int ScannedFramesCount { get; private set; }

        public float LastAlignmentEnergy { get; private set; }

        public event EventHandler ScanUpdated;


        public async Task Scan()
        {
            if (!sensor.IsAvailable)
                throw new ApplicationException(Properties.Resources.KinectNotAvailable);

            ReconstructionController controller;
            try
            {
                controller = controllerFactory.Invoke();
            }
            catch (Exception ex)
            {
                throw new ApplicationException(string.Format(Properties.Resources.InitScanError, ex.Message), ex);
            }

            using (controller)
            {
                ScannedFramesCount = 0;
                LastAlignmentEnergy = 0;
                ScanBitmap = controller.SurfaceBitmap;
                ScanBitmapWidth = controller.SurfaceBitmapWidth;
                ScanBitmapHeight = controller.SurfaceBitmapHeight;
                RaiseScanUpdated();

                controller.SurfaceBitmapUpdated += (_, __) => RaiseScanUpdated();
                controller.FrameAligned += Controller_FrameAligned;

                await Task.Delay(5000);
            }
        }

        private void Controller_FrameAligned(object sender, EventArgs e)
        {
            var controller = (ReconstructionController)sender;
            ScannedFramesCount++;
            LastAlignmentEnergy = controller.LastFrameAlignmentEnergy;

            RaiseScanUpdated();
        }

        private void RaiseScanUpdated()
        {
            ScanUpdated?.Invoke(this, EventArgs.Empty);
        }
    }
}
