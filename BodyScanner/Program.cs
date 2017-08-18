using Microsoft.Kinect;
using System;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace BodyScanner
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            AppViewModel viewModel;
            try
            {
                viewModel = CreateViewModel();
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(Properties.Resources.InitializationError, ex.Message),
                    Properties.Resources.ApplicationName,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            RunApplication(viewModel);
        }

        private static void RunApplication(AppViewModel viewModel)
        {

            var app = new App();
            var mainWindow = new MainWindow
            {
                DataContext = viewModel
            };
            app.Run(mainWindow);
        }

        private static AppViewModel CreateViewModel()
        {
            SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext());

            var sensor = CreateAndOpenSensor();
            var engine = new ScanningEngine(sensor, () => new ReconstructionController(sensor));
            var renderer = new KinectFrameRenderer(sensor, new DepthToColorConverter());
            var uis = new UserInteractionService();
            return new AppViewModel(engine, renderer, uis);
        }

        private static KinectSensor CreateAndOpenSensor()
        {
            var sensor = KinectSensor.GetDefault();
            if (sensor == null)
            {
                throw new ApplicationException(Properties.Resources.KinectNotAvailable);
            }

            sensor.Open();
            if (!sensor.IsOpen)
            {
                throw new ApplicationException(Properties.Resources.KinectNotAvailable);
            }

            return sensor;
        }
    }
}
