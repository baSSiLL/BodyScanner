using System;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;

namespace BodyScanner
{
    internal class AppViewModel : ViewModelBase
    {
        private readonly ScanningEngine engine;
        private readonly KinectFrameRenderer renderer;
        private readonly UserInteractionService uis;

        public AppViewModel(ScanningEngine engine, KinectFrameRenderer renderer, UserInteractionService uis)
        {
            Contract.Requires(engine != null);
            Contract.Requires(renderer != null);

            this.engine = engine;
            this.renderer = renderer;
            this.uis = uis;

            startScanningCommand = new DelegateCommand(DoScanning, CanStartScanning);

            Prompt = Properties.Resources.PromptEnterName;

            renderer.BitmapUpdated += Renderer_BitmapUpdated;
        }

        public string WindowTitle => Properties.Resources.ApplicationName;

        public string Prompt
        {
            get { return prompt; }
            private set { SetPropertyValue(value, ref prompt); }
        }
        private string prompt;

        public string PersonName
        {
            get { return personName; }
            set { SetPropertyValue(value, ref personName); }
        }
        private string personName;

        public ICommand StartScanningCommand => startScanningCommand;
        private DelegateCommand startScanningCommand;


        public bool IsScanning
        {
            get { return isScanning; }
            private set { SetPropertyValue(value, ref isScanning); }
        }
        private bool isScanning;

        private async void DoScanning()
        {
            if (!CanStartScanning())
                throw new InvalidOperationException("Cannot start scanning");

            IsScanning = true;

            await AwaitForStart(5);

            try
            {
                await engine.Scan();
                Prompt = Properties.Resources.PromptScanCompleted;
            }
            catch (ApplicationException ex)
            {
                uis.ShowError(ex.Message);
                Prompt = Properties.Resources.PromptScanAborted;
            }
            IsScanning = false;

            await Task.Delay(TimeSpan.FromSeconds(5));

            if (!IsScanning)
            {
                Prompt = Properties.Resources.PromptEnterName;
            }
        }

        private bool CanStartScanning()
        {
            return !isScanning && !string.IsNullOrWhiteSpace(PersonName);
        }


        private async Task AwaitForStart(int startDelayInSeconds)
        {
            do
            {
                Prompt = string.Format(Properties.Resources.PromptScanBeginsIn, startDelayInSeconds);
                await Task.Delay(1000);
            } while (--startDelayInSeconds > 0);
        }


        public byte[] DepthBitmap => renderer.Bitmap;

        public int DepthBitmapWidth => renderer.BitmapWidth;

        public int DepthBitmapHeight => renderer.BitmapHeight;

        private void Renderer_BitmapUpdated(object sender, EventArgs e)
        {
            OnPropertyChanged(nameof(DepthBitmap));
        }

        protected override void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            base.OnPropertyChanged(propertyName);

            switch (propertyName)
            {
                case nameof(PersonName):
                case nameof(IsScanning):
                    startScanningCommand.InvalidateCanExecute();
                    break;
            }
        }
    }
}
