﻿using Microsoft.Win32;
using System;
using System.Diagnostics.Contracts;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media.Media3D;

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
            saveModelCommand = new DelegateCommand(SaveBodyModel, () => engine.ScannedMesh != null);

            Prompt = Properties.Resources.PromptEnterName;
            ShowDepthBitmap = true;

            renderer.BitmapUpdated += Renderer_BitmapUpdated;
            engine.ScanStarted += Engine_ScanStarted;
            engine.ScanUpdated += Engine_ScanUpdated;
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
            Body3DModel = null;
            ShowDepthBitmap = true;
            Prompt = Properties.Resources.PromptToBeginScan;

            try
            {
                await engine.Run();
                Prompt = Properties.Resources.PromptScanCompleted;
                FloorNormal = new Vector3D(engine.FloorNormal.X, engine.FloorNormal.Y, engine.FloorNormal.Z);
                Body3DModel = engine.ScannedMesh == null
                    ? null
                    : MeshConverter.Convert(engine.ScannedMesh);
            }
            catch (ApplicationException ex)
            {
                uis.ShowError(ex.Message);
                Prompt = Properties.Resources.PromptScanAborted;
            }

            ShowScanBitmap = false;
            if (Body3DModel == null)
            {
                ShowDepthBitmap = true;
            }
            IsScanning = false;

            await Task.Delay(TimeSpan.FromSeconds(5));

            if (!IsScanning)
            {
                ScanningStatus = null;
                Prompt = Properties.Resources.PromptEnterName;
            }
        }

        private bool CanStartScanning()
        {
            return !isScanning && !string.IsNullOrWhiteSpace(PersonName);
        }


        public bool ShowDepthBitmap
        {
            get { return showDepthBitmap; }
            private set { SetPropertyValue(value, ref showDepthBitmap); }
        }
        private bool showDepthBitmap;

        public ThreadSafeBitmap DepthBitmap
        {
            get { return renderer.Bitmap; }
        }

        private void Renderer_BitmapUpdated(object sender, EventArgs e)
        {
            OnPropertyChanged(nameof(DepthBitmap));
        }


        public bool ShowScanBitmap
        {
            get { return showScanBitmap; }
            private set { SetPropertyValue(value, ref showScanBitmap); }
        }
        private bool showScanBitmap;

        public ThreadSafeBitmap ScanBitmap => engine.ScanBitmap;

        public string ScanningStatus
        {
            get { return scanningStatus; }
            private set { SetPropertyValue(value, ref scanningStatus); }
        }
        private string scanningStatus;

        public MeshGeometry3D Body3DModel
        {
            get { return body3DModel; }
            private set { SetPropertyValue(value, ref body3DModel); }
        }
        private MeshGeometry3D body3DModel;

        public Vector3D FloorNormal
        {
            get { return floorNormal; }
            private set { SetPropertyValue(value, ref floorNormal); }
        }
        private Vector3D floorNormal;

        public bool ShowBodyModel
        {
            get { return Body3DModel != null; }
        }

        public ICommand SaveModelCommand => saveModelCommand;
        private readonly DelegateCommand saveModelCommand;

        private void SaveBodyModel()
        {
            var dialog = new SaveFileDialog
            {
                Title = "Save Model",
                Filter = "Wavefront OBJ|*.obj|STL (binary)|*.stl|PLY (text)|*.ply",
                FilterIndex = 0,
                DefaultExt = ".obj"
            };
            if (dialog.ShowDialog() == true)
            {
                var flipAxes = true;
                switch (Path.GetExtension(dialog.FileName).ToLowerInvariant())
                {
                    case ".obj":
                        using (var writer = File.CreateText(dialog.FileName))
                        {
                            ModelIO.SaveAsciiObjMesh(engine.ScannedMesh, writer, flipAxes);
                        }
                        break;
                    case ".stl":
                        using (var file = File.Create(dialog.FileName))
                        using (var writer = new BinaryWriter(file))
                        {
                            ModelIO.SaveBinaryStlMesh(engine.ScannedMesh, writer, flipAxes);
                        }
                        break;
                    case ".ply":
                        using (var writer = File.CreateText(dialog.FileName))
                        {
                            ModelIO.SaveAsciiPlyMesh(engine.ScannedMesh, writer, flipAxes);
                        }
                        break;
                    default:
                        uis.ShowError("Unsupported file format");
                        break;
                }
            }
        }

        private void Engine_ScanStarted(object sender, EventArgs e)
        {
            ShowDepthBitmap = false;
            ShowScanBitmap = true;
            Prompt = Properties.Resources.PromptScanning;

            OnScanUpdated();
        }

        private void Engine_ScanUpdated(object sender, EventArgs e)
        {
            OnScanUpdated();
        }

        private void OnScanUpdated()
        {
            ScanningStatus = string.Format(Properties.Resources.ScanningStatus, engine.ScannedFramesCount, engine.LastAlignmentEnergy);
            OnPropertyChanged(nameof(ScanBitmap));
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
                case nameof(Body3DModel):
                    OnPropertyChanged(nameof(ShowBodyModel));
                    saveModelCommand.InvalidateCanExecute();
                    break;
            }
        }
    }
}
