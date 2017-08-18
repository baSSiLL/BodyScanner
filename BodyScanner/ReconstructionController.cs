using Microsoft.Kinect;
using Microsoft.Kinect.Fusion;
using System;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Threading;
using System.Threading.Tasks;

namespace BodyScanner
{
    class ReconstructionController : IDisposable
    {
        const float MIN_DEPTH = 1.5f;
        const float MAX_DEPTH = 3.5f;

        private readonly SynchronizationContext syncContext;
        private readonly SharedCriticalSection syncProcessing = new SharedCriticalSection();

        private readonly DepthFrameReader reader;
        private readonly Reconstruction reconstruction;
        private readonly ushort[] rawFrameData;
        private readonly FusionFloatImageFrame floatFrame;
        private readonly FusionPointCloudImageFrame pointCloudFrame;
        private readonly FusionColorImageFrame surfaceFrame;
        private Matrix4 worldToCameraTransform = Matrix4.Identity;
        private Matrix4 worldToVolumeTransform;

        public ReconstructionController(KinectSensor sensor)
        {
            Contract.Requires(sensor != null);

            this.syncContext = SynchronizationContext.Current;

            var rparams = new ReconstructionParameters(128, 256, 256, 256);
            reconstruction = Reconstruction.FusionCreateReconstruction(rparams, ReconstructionProcessor.Amp, -1, worldToCameraTransform);
            worldToVolumeTransform = reconstruction.GetCurrentWorldToVolumeTransform();
            worldToVolumeTransform.M43 -= MIN_DEPTH * rparams.VoxelsPerMeter;
            reconstruction.ResetReconstruction(worldToCameraTransform, worldToVolumeTransform);

            var depthFrameDesc = sensor.DepthFrameSource.FrameDescription;

            var totalPixels = depthFrameDesc.Width * depthFrameDesc.Height;
            rawFrameData = new ushort[totalPixels];
            SurfaceBitmap = new int[totalPixels];
            SurfaceBitmapWidth = depthFrameDesc.Width;
            SurfaceBitmapHeight = depthFrameDesc.Height;

            var intrinsics = sensor.CoordinateMapper.GetDepthCameraIntrinsics();
            var cparams = new CameraParameters(
                intrinsics.FocalLengthX / SurfaceBitmapWidth, 
                intrinsics.FocalLengthY / SurfaceBitmapHeight, 
                intrinsics.PrincipalPointX / SurfaceBitmapWidth, 
                intrinsics.PrincipalPointY / SurfaceBitmapHeight);
            floatFrame = new FusionFloatImageFrame(depthFrameDesc.Width, depthFrameDesc.Height, cparams);
            pointCloudFrame = new FusionPointCloudImageFrame(depthFrameDesc.Width, depthFrameDesc.Height, cparams);
            surfaceFrame = new FusionColorImageFrame(depthFrameDesc.Width, depthFrameDesc.Height, cparams);

            reader = sensor.DepthFrameSource.OpenReader();
            reader.FrameArrived += Reader_FrameArrived;
        }

        public void Dispose()
        {
            syncProcessing.Enter();

            reader?.Dispose();

            floatFrame?.Dispose();
            pointCloudFrame?.Dispose();
            surfaceFrame?.Dispose();

            reconstruction?.Dispose();
        }

        public int[] SurfaceBitmap { get; }

        public int SurfaceBitmapWidth { get; }

        public int SurfaceBitmapHeight { get; }

        public event EventHandler SurfaceBitmapUpdated;

        private void Reader_FrameArrived(object sender, DepthFrameArrivedEventArgs e)
        {
            if (!syncProcessing.TryEnter())
                return;

            var frame = e.FrameReference.AcquireFrame();
            if (frame == null)
            {
                syncProcessing.Exit();
                return;
            }

            using (frame) frame.CopyFrameDataToArray(rawFrameData);

            Task.Run(new Action(ProcessFrame)).
                ContinueWith(_ => syncProcessing.Exit());
        }

        public float LastFrameAlignmentEnergy => alignmentEnergy;
        private float alignmentEnergy;

        public event EventHandler FrameAligned;

        private void ProcessFrame()
        {
            try
            {
                reconstruction.DepthToDepthFloatFrame(rawFrameData, floatFrame,
                    MIN_DEPTH, MAX_DEPTH,
                    false);

                var aligned = reconstruction.ProcessFrame(
                    floatFrame,
                    FusionDepthProcessor.DefaultAlignIterationCount,
                    FusionDepthProcessor.DefaultIntegrationWeight,
                    out alignmentEnergy,
                    worldToCameraTransform);
                if (aligned)
                {
                    syncContext.Post(() => FrameAligned?.Invoke(this, EventArgs.Empty));
                    worldToCameraTransform = reconstruction.GetCurrentWorldToCameraTransform();
                }
            }
            catch (InvalidOperationException)
            {
            }

            try
            {
                //reconstruction.CalculatePointCloud(pointCloudFrame, worldToCameraTransform);
                reconstruction.CalculatePointCloudAndDepth(pointCloudFrame, floatFrame, worldToCameraTransform);
                FusionDepthProcessor.DepthFloatFrameToPointCloud(floatFrame, pointCloudFrame);

                FusionDepthProcessor.ShadePointCloud(pointCloudFrame, worldToCameraTransform, surfaceFrame, null);
                surfaceFrame.CopyPixelDataTo(SurfaceBitmap);

                syncContext.Post(() => SurfaceBitmapUpdated?.Invoke(this, EventArgs.Empty));
            }
            catch (InvalidOperationException)
            {
            }
        }
    }
}
