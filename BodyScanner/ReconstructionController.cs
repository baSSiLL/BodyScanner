using Microsoft.Kinect;
using Microsoft.Kinect.Fusion;
using System;
using System.Diagnostics.Contracts;
using System.Linq;
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
        private bool isDisposed;

        private readonly KinectSensor sensor;
        private MultiSourceFrameReader reader;
        private readonly Reconstruction reconstruction;
        private readonly ushort[] rawDepthData;
        private readonly byte[] bodyIndexData;
        private readonly FusionFloatImageFrame floatDepthFrame;
        private readonly FusionPointCloudImageFrame pointCloudFrame;
        private readonly FusionColorImageFrame surfaceFrame;
        private Matrix4 worldToCameraTransform = Matrix4.Identity;
        private Matrix4 worldToVolumeTransform;
        private ulong reconstructedBodyTrackingId = ulong.MaxValue;

        public ReconstructionController(KinectSensor sensor)
        {
            Contract.Requires(sensor != null);

            this.syncContext = SynchronizationContext.Current;
            this.sensor = sensor;

            var rparams = new ReconstructionParameters(128, 256, 256, 256);
            reconstruction = Reconstruction.FusionCreateReconstruction(rparams, ReconstructionProcessor.Amp, -1, worldToCameraTransform);
            worldToVolumeTransform = reconstruction.GetCurrentWorldToVolumeTransform();
            worldToVolumeTransform.M43 -= MIN_DEPTH * rparams.VoxelsPerMeter;
            reconstruction.ResetReconstruction(worldToCameraTransform, worldToVolumeTransform);

            var depthFrameDesc = sensor.DepthFrameSource.FrameDescription;

            var totalPixels = depthFrameDesc.Width * depthFrameDesc.Height;
            rawDepthData = new ushort[totalPixels];
            bodyIndexData = new byte[totalPixels];
            SurfaceBitmap = new ThreadSafeBitmap(depthFrameDesc.Width, depthFrameDesc.Height);

            var intrinsics = sensor.CoordinateMapper.GetDepthCameraIntrinsics();
            var cparams = new CameraParameters(
                intrinsics.FocalLengthX / depthFrameDesc.Width, 
                intrinsics.FocalLengthY / depthFrameDesc.Height, 
                intrinsics.PrincipalPointX / depthFrameDesc.Width, 
                intrinsics.PrincipalPointY / depthFrameDesc.Height);
            floatDepthFrame = new FusionFloatImageFrame(depthFrameDesc.Width, depthFrameDesc.Height, cparams);
            pointCloudFrame = new FusionPointCloudImageFrame(depthFrameDesc.Width, depthFrameDesc.Height, cparams);
            surfaceFrame = new FusionColorImageFrame(depthFrameDesc.Width, depthFrameDesc.Height, cparams);
        }

        public void Dispose()
        {
            if (!isDisposed)
            {
                syncProcessing.Enter();

                isDisposed = true;

                reader?.Dispose();

                floatDepthFrame?.Dispose();
                pointCloudFrame?.Dispose();
                surfaceFrame?.Dispose();

                reconstruction?.Dispose();
            }
        }

        public event EventHandler ReconstructionStarted;

        public ThreadSafeBitmap SurfaceBitmap { get; }

        public event EventHandler SurfaceBitmapUpdated;

        public void Start()
        {
            reader = sensor.OpenMultiSourceFrameReader(FrameSourceTypes.Depth | FrameSourceTypes.Body | FrameSourceTypes.BodyIndex);
            reader.MultiSourceFrameArrived += Reader_MultiSourceFrameArrived;
        }

        public Mesh GetBodyMesh()
        {
            return reconstruction.CalculateMesh(1);
        }

        private void Reader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            if (!syncProcessing.TryEnter())
                return;

            byte bodyIndex = 255;
            var frame = e.FrameReference.AcquireFrame();
            var isValidFrame = frame != null;
            if (isValidFrame)
            {
                using (var bodyFrame = frame.BodyFrameReference.AcquireFrame())
                {
                    isValidFrame = bodyFrame != null;
                    if (isValidFrame)
                    {
                        if (!IsReconstructing)
                        {
                            SelectBodyToReconstruct(bodyFrame);
                            if (IsReconstructing)
                                syncContext.Post(() => ReconstructionStarted?.Invoke(this, EventArgs.Empty));
                        }

                        if (IsReconstructing)
                        {
                            bodyIndex = GetReconstructedBodyIndex(bodyFrame);
                            isValidFrame = bodyIndex != byte.MaxValue;
                        }
                    }
                }

                if (isValidFrame && IsReconstructing)
                {
                    using (var depthFrame = frame.DepthFrameReference.AcquireFrame())
                    using (var bodyIndexFrame = frame.BodyIndexFrameReference.AcquireFrame())
                    {
                        isValidFrame = depthFrame != null && bodyIndexFrame != null;
                        if (isValidFrame)
                        {
                            depthFrame.CopyFrameDataToArray(rawDepthData);
                            bodyIndexFrame.CopyFrameDataToArray(bodyIndexData);
                        }
                    }
                }
            }

            if (isValidFrame && IsReconstructing)
            {
                Task.Run(() => ProcessFrame(bodyIndex)).
                    ContinueWith(_ => syncProcessing.Exit());
            }
            else
            {
                syncProcessing.Exit();
            }
        }

        public float LastFrameAlignmentEnergy => alignmentEnergy;
        private float alignmentEnergy;

        public event EventHandler FrameAligned;

        public bool IsReconstructing => reconstructedBodyTrackingId != ulong.MaxValue;

        private void ProcessFrame(byte bodyIndex)
        {
            try
            {
                RemoveNonBodyPixels(bodyIndex);

                reconstruction.DepthToDepthFloatFrame(rawDepthData, floatDepthFrame,
                    MIN_DEPTH, MAX_DEPTH,
                    false);

                var aligned = reconstruction.ProcessFrame(
                    floatDepthFrame,
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
                reconstruction.CalculatePointCloud(pointCloudFrame, worldToCameraTransform);

                FusionDepthProcessor.ShadePointCloud(pointCloudFrame, worldToCameraTransform, surfaceFrame, null);
                SurfaceBitmap.Access(data => surfaceFrame.CopyPixelDataTo(data));

                syncContext.Post(() => SurfaceBitmapUpdated?.Invoke(this, EventArgs.Empty));
            }
            catch (InvalidOperationException)
            {
            }
        }

        private void SelectBodyToReconstruct(BodyFrame bodyFrame)
        {
            if (bodyFrame.BodyCount == 0)
                return;

            var bodies = new Body[bodyFrame.BodyCount];
            bodyFrame.GetAndRefreshBodyData(bodies);

            var minBodyZ = float.PositiveInfinity;
            foreach (var body in bodies.Where(IsBodySuitableForReconstruction))
            {
                var z = body.Joints[JointType.SpineBase].Position.Z;
                if (z < minBodyZ)
                {
                    minBodyZ = z;
                    reconstructedBodyTrackingId = body.TrackingId;
                }
            }
        }

        private static bool IsBodySuitableForReconstruction(Body body)
        {
            if (!body.IsTracked) return false;

            var spineBase = body.Joints[JointType.SpineBase];
            if (spineBase.TrackingState != TrackingState.Tracked) return false;

            var middleZ = (MIN_DEPTH + MAX_DEPTH) / 2;
            return middleZ - 0.5f < spineBase.Position.Z && spineBase.Position.Z < middleZ + 0.5f &&
                -0.5f < spineBase.Position.X && spineBase.Position.X < 0.5f;
        }

        private byte GetReconstructedBodyIndex(BodyFrame bodyFrame)
        {
            if (bodyFrame.BodyCount == 0)
                return byte.MaxValue;

            var bodies = new Body[bodyFrame.BodyCount];
            bodyFrame.GetAndRefreshBodyData(bodies);

            for (var i = 0; i < bodies.Length; i++)
            {
                if (bodies[i].IsTracked && bodies[i].TrackingId == reconstructedBodyTrackingId)
                    return (byte)i;
            }

            return byte.MaxValue;
        }

        private void RemoveNonBodyPixels(int bodyIndex)
        {
            for (var i = 0; i < rawDepthData.Length; i++)
            {
                if (bodyIndexData[i] != bodyIndex)
                {
                    rawDepthData[i] = 0;
                }
            }
        }
    }
}
