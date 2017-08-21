using Microsoft.Kinect;
using Microsoft.Kinect.Fusion;
using System;
using System.Diagnostics.Contracts;
using System.Threading.Tasks;

namespace BodyScanner
{
    internal class ScanningEngine
    {
        private static readonly TimeSpan SCAN_DURATION = TimeSpan.FromSeconds(10);

        private readonly KinectSensor sensor;
        private readonly Func<ReconstructionController> controllerFactory;
        private DateTime scanEndTime;

        public ScanningEngine(KinectSensor sensor, Func<ReconstructionController> controllerFactory)
        {
            Contract.Requires(sensor != null);
            Contract.Requires(controllerFactory != null);

            this.sensor = sensor;
            this.controllerFactory = controllerFactory;
        }

        public ThreadSafeBitmap ScanBitmap { get; private set; }

        public int ScannedFramesCount { get; private set; }

        public float LastAlignmentEnergy { get; private set; }

        public Mesh ScannedMesh { get; private set; }

        public event EventHandler ScanUpdated;

        public event EventHandler ScanStarted;


        public async Task Run()
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
                RaiseScanUpdated();

                controller.SurfaceBitmapUpdated += (_, __) => RaiseScanUpdated();
                controller.FrameAligned += Controller_FrameAligned;
                controller.ReconstructionStarted += Controller_ReconstructionStarted;

                scanEndTime = DateTime.MaxValue;
                controller.Start();

                // TODO: Should check for various sides of scanning instead
                while (DateTime.UtcNow < scanEndTime)
                {
                    await Task.Delay(1000);
                }

                ScannedMesh = controller.GetBodyMesh();
            }
        }

        private void Controller_ReconstructionStarted(object sender, EventArgs e)
        {
            scanEndTime = DateTime.UtcNow.Add(SCAN_DURATION);

            ScanStarted?.Invoke(this, EventArgs.Empty);
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
