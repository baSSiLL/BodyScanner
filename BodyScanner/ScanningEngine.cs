using Microsoft.Kinect;
using System;
using System.Diagnostics.Contracts;
using System.Threading.Tasks;

namespace BodyScanner
{
    internal class ScanningEngine
    {
        private readonly KinectSensor sensor;

        public ScanningEngine(KinectSensor sensor)
        {
            Contract.Requires(sensor != null);

            this.sensor = sensor;
        }

        public bool IsSensorAvailable => sensor.IsAvailable;

        public async Task Scan()
        {
            if (!IsSensorAvailable)
                throw new ApplicationException(Properties.Resources.KinectNotAvailable);
        }
    }
}
