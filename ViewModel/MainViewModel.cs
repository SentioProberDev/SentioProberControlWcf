using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using GalaSoft.MvvmLight.CommandWpf;
using GalaSoft.MvvmLight.Threading;

using Sentio.Contracts;
using Sentio.Contracts.Enumerations;
using Sentio.Contracts.Helper;
// ReSharper disable StringLiteralTypo

namespace Sentio.WcfTest.ViewModel
{
    public class MainViewModel : SentioWcfClientBase
    {
        private ImageSource _activeModuleSnapshot;
        private string _hint = "Hello World";
        private ImageSource _imageSource;

        private bool _showClientPanel;

        private bool _autoConnect = false;

        /// <summary>
        ///     Initializes a new instance of the MainViewModel class.
        /// </summary>
        public MainViewModel()
        {

            CmdConnect = new RelayCommand(CmdConnectImpl, () => !IsConnected);
            CmdDisconnect = new RelayCommand(() => { DisconnectClient(); }, () => IsConnected);

            CmdShowHint = new RelayCommand(ShowHintImpl, () => IsConnected);
            CmdSelectModule = new RelayCommand<string>(SelectModuleImpl, param => IsConnected);
            CmdSwitchCamera = new RelayCommand<string>(SwitchCameraImpl, param => IsConnected);
            CmdStepFirstDie = new RelayCommand(StepFirstDieImpl, () => IsConnected);
            CmdStepNextDie = new RelayCommand(StepNextDieImpl, () => IsConnected);
            CmdGrabImage = new RelayCommand(GrabImageImpl, () => IsConnected);
            CmdGrabActiveModule = new RelayCommand(GrabActiveModuleImpl, () => IsConnected);
            CmdListModuleProperties = new RelayCommand(ListModulePropertiesImpl, () => IsConnected);
            CmdSetLight = new RelayCommand(SetModuleProperties, () => IsConnected);
            LogLines = new ObservableCollection<string>();

            ClientUi = new List<ClientControl>
            {
                new ClientButton("btnExit", "Exit", OnExit, "Icon_Cancel"),
                new ClientButton("btnRun", "Run", OnRun),
                new ClientButton("btnClearBins", "Clear Bins", OnClearBins),
                new ClientButton("btnStepNext", "StepNextDie", OnStepNextDie),
                new ClientTextBox("edName", "DUT", OnDutChanged),
                new ClientIcon("icInfo", "Icon_OpenProject"),
                new ClientLabel("lbLog", "This is a label")
            };

            var args = Environment.GetCommandLineArgs();
            _autoConnect = args.FirstOrDefault(opt => opt == "-c") != null;
        }

        private void CmdConnectImpl()
        {
            try
            {
                Connect("Sentio WCF Client", "wcf_test");
                LogLines.Add($"Session: User={Session.User}; AccessLevel={Session.AccessLevel}");
                LogLines.Add($"ActiveModule: {ActiveModule}");
            }
            catch (Exception exc)
            {
                LogLines.Add(exc.Message);
            }
        }

        private void SetModuleProperties()
        {
            try
            {
                if ((int)SentioCompatLevel <= (int)SentioCompatibilityLevel.Sentio_3_6)
                {
                    throw new NotSupportedException("SENTIO Version 3.6 or higher needed! (Set Compatibility level accordingly)");
                }

                LogLines.Clear();

                LogLines.Add("The following examples demonstrate how to set module properties");

                LogLines.Add("Setting light");
                Sentio.SetModuleProperty(SentioModules.Vision, "light", new SentioVariantData(Camera.Scope.ToString()), new SentioVariantData(100));

                LogLines.Add("Setting gain");
                Sentio.SetModuleProperty(SentioModules.Vision, "gain", new SentioVariantData(Camera.Scope.ToString()), new SentioVariantData(0.3));

                LogLines.Add("Setting jpeg_quality");
                Sentio.SetModuleProperty(SentioModules.Vision, "jpeg_quality", new SentioVariantData(100));

                LogLines.Add("Done");
            }
            catch (FaultException<SentioErrorDetails> exc)
            {
                // This exception should be the standard for transmitting
                // non sentio runtime errors
                LogLines.Add(exc.Detail.Message);
                LogLines.Add(exc.Detail.Details);
            }
            catch (FaultException exc)
            {
                // This may be related to the wcf underpinnings and have nothing to
                // do with SENTIO but with the connection as a whole.
                LogLines.Add(exc.Message);
            }
            catch (Exception exc)
            {
                // ANY error from the wcf client goes here
                LogLines.Add(exc.Message);
            }
        }

        private void ListModulePropertiesImpl()
        {
            try
            {
                if ((int)SentioCompatLevel <= (int)SentioCompatibilityLevel.Sentio_3_6)
                {
                    throw new NotSupportedException("SENTIO Version 3.6 or higher needed! (Set Compatibility level accordingly)");
                }

                LogLines.Clear();

                var visionProp = new Dictionary<string, string>
                {
                    // Jpeg Quality in percent when saving images
                    { "jpeg_quality", "" },

                    // Camera Parameters, The parameter refers to the camera.
                    // available cameras are: scope, offaxis, chuck, vce01, scope2
                    { "image_size", "scope" },
                    { "light", "scope" },
                    { "gain", "scope" },
                    { "gain_min", "scope" },
                    { "gain_max", "scope" },
                    { "exposure", "scope" },
                    { "exposure_min", "scope" },
                    { "exposure_max", "scope" },
                    { "calib", "scope" },

                    // size of the camera's region of interest in µm
                    { "roi_size", "scope" }
                };

                foreach (var it in visionProp)
                {
                    try
                    {
                        var propName = it.Key;
                        var propArg = it.Value;

                        var prop = Sentio.GetModuleProperty(SentioModules.Vision, propName, propArg);
                        LogLines.Add($"{it.Key} - {prop} ({prop.Type})");
                    }
                    catch (FaultException<SentioErrorDetails> exc)
                    {
                        // This exception should be the standard for transmitting
                        // non sentio runtime errors
                        LogLines.Add(exc.Detail.Message);
                        LogLines.Add(exc.Detail.Details);
                    }
                    catch (FaultException exc)
                    {
                        // This may be related to the wcf underpinnings and have nothing to
                        // do with SENTIO but with the connection as a whole.
                        LogLines.Add(exc.Message);
                    }
                }
            }
            catch (Exception exc)
            {
                LogLines.Add(exc.Message);
            }
        }

        public bool AutoConnect => _autoConnect;

        public void TryAutoConnect()
        {
            if (!AutoConnect)
                return;

            if (!SentioInteropHelper.IsSentioRunning)
                return;

            try
            {
                CmdConnect.Execute(null);
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message, "Automatic Connection to Sentio failed!");
            }
        }

        public ImageSource ActiveModuleSnapshot
        {
            get => _activeModuleSnapshot;
            set => Set(ref _activeModuleSnapshot, value);
        }

        public ICommand CmdConnect { get; }

        public ICommand CmdDisconnect { get; }

        public ICommand CmdGrabActiveModule { get; }

        public ICommand CmdGrabImage { get; }

        public ICommand CmdSelectModule { get; }

        public ICommand CmdShowHint { get; }

        public ICommand CmdStepFirstDie { get; }

        public ICommand CmdStepNextDie { get; }

        public ICommand CmdSwitchCamera { get; }

        public ICommand CmdListModuleProperties { get; }

        public ICommand CmdSetLight { get; }

        public string Hint
        {
            get => _hint;
            set => Set(ref _hint, value);
        }

        public ImageSource ImageSource
        {
            get => _imageSource;
            set => Set(ref _imageSource, value);
        }

        public ObservableCollection<string> LogLines { get; set; }

        public bool ShowClientPanel
        {
            get => _showClientPanel;

            set
            {
                Set(ref _showClientPanel, value);
                Sentio?.ShowClientPanel(value);
            }
        }

        public override RemoteCommandResponse ExecuteExternalRemoteCommand(string cmd, string param)
        {
            var resp = new RemoteCommandResponse();
            DispatcherHelper.UIDispatcher.Invoke(
                () =>
                {
                    LogLines.Add($"Remote Command Request: {cmd} {param}");
                });

            switch (cmd)
            {
                case "command1":
                    resp.Message = "Hello World!";
                    resp.ErrorCode = 0;
                    break;

                case "command2":
                    resp.Message = "You found the second remote command!";
                    resp.ErrorCode = 0;
                    break;

                default:
                    resp.Message = $"Invalid Command: {cmd}";
                    resp.ErrorCode = (int)SentioErrorCodes.InvalidCommand;
                    break;
            }

            return resp;
        }

        public override void OnChannelClosed()
        {
            DispatcherHelper.UIDispatcher.Invoke(
                () =>
                {
                    LogLines.Add("Sentio channel closed!");
                });
        }

        public override void OnChannelFaulted()
        {
            DispatcherHelper.UIDispatcher.Invoke(
                () =>
                {
                    LogLines.Add("Sentio channel faulted!");
                });
        }

        public override void NotifyActiveModuleChanged(SentioModules module)
        {
            base.NotifyActiveModuleChanged(module);
            DispatcherHelper.UIDispatcher.Invoke(
                () =>
                {
                    LogLines.Add($"Active module changed to {module}");

                    if (module == SentioModules.Wafermap)
                    {
                        Thread.Sleep(500);
                    }

                    GrabActiveModuleImpl();
                });
        }

        public override void NotifyButtonPressed(string btnId)
        {
            base.NotifyButtonPressed(btnId);
            DispatcherHelper.UIDispatcher.Invoke(
                () =>
                {
                    LogLines.Add($"Button Pressed: {btnId}");
                    GrabActiveModuleImpl();
                });
        }

        public override void NotifyWafermapViewportChange(double x, double y, double z)
        {
            DispatcherHelper.UIDispatcher.Invoke(
                () =>
                {
                    LogLines.Add($"Wafermap viewport changed: {x}, {y}, {z}");
                    GrabActiveModuleImpl();
                });
        }

        public override void NotifyRemoteModeChanged(bool state)
        {
            base.NotifyRemoteModeChanged(state);
            DispatcherHelper.UIDispatcher.Invoke(
                () =>
                {
                    LogLines.Add($"Remote mode changed: {state}");
                    GrabActiveModuleImpl();
                });
        }

        public override void NotifySentioShutdown()
        {
            base.NotifySentioShutdown();
            DispatcherHelper.UIDispatcher.Invoke(() => { LogLines.Add("Sentio was shut down!"); });
        }

        public override void NotifyStepToCell(int col, int row, int site)
        {
            DispatcherHelper.UIDispatcher.Invoke(
                () =>
                {
                    LogLines.Add($"Step to Cell Notification received Col:{col}; Row: {row}; Index: {site}");
                    GrabActiveModuleImpl();
                });
        }

        public override void NotifyWafermapDoubleClickOnDie(int col, int row, int n)
        {
            DispatcherHelper.CheckBeginInvokeOnUI(() => { LogLines.Add($"Die Clicked at Col:{col}; Row: {row}; Index: {n}"); });
        }

        public override void NotifyProjectSave(string project)
        {
            DispatcherHelper.CheckBeginInvokeOnUI(() => { LogLines.Add($"Project saved: {project}"); });
        }

        public override void NotifyProjectLoad(string project)
        {
            DispatcherHelper.CheckBeginInvokeOnUI(() => { LogLines.Add($"Project load: {project}"); });
        }

        public override void NotifySessionChanged(SentioSessionInfo session)
        {
            DispatcherHelper.CheckBeginInvokeOnUI(() => { LogLines.Add($"New Sentio Session: {session.User}, {session.AccessLevel}"); });
        }

        private void GrabActiveModuleImpl()
        {
            byte[] imageData = Sentio?.SnapShotActiveModule(ImageFormat.Jpeg);
            if (imageData == null)
            {
                return;
            }

            var memoryStream = new MemoryStream(imageData);
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.StreamSource = memoryStream;
            bmp.EndInit();

            // Assign the Source property of your image
            ActiveModuleSnapshot = bmp;
        }

        private void GrabImageImpl()
        {
            try
            {
                byte[] imageData = Sentio?.GrabCameraImage(Camera.Scope, ImageFormat.Jpeg);
                if (imageData == null)
                {
                    return;
                }

                var memoryStream = new MemoryStream(imageData);
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.StreamSource = memoryStream;
                bmp.EndInit();

                // Assign the Source property of your image
                ImageSource = bmp;
            }
            catch (Exception exc)
            {
                LogLines.Add(exc.Message);
            }
        }

        /// <summary>
        ///     Clear all wafermap bins by executing the clearallbins command from the wafermap command handler
        /// </summary>
        private void OnClearBins()
        {
            try
            {
                Sentio?.ExecuteModuleCommand(Defaults.WafermapModuleName, "ClearAllBins", null);
            }
            catch (Exception exc)
            {
                LogLines.Add(exc.Message);
            }
        }

        private void OnDutChanged(string text)
        {
            LogLines.Add($"Dut Textbox updated: \"{text}\"");
        }

        private void OnExit()
        {
            LogLines.Add("OnExit Button pressed");

            Sentio.CloseWcfSession();

            Application.Current.Shutdown();
        }

        private void OnRun()
        {
            LogLines.Add("OnRun Button pressed");
            TestAll();
        }

        private void OnStepNextDie()
        {
            StepNextDieImpl();
        }

        private void SelectModuleImpl(string moduleName)
        {
            if (Sentio == null)
            {
                return;
            }

            try
            {
                var module = (SentioModules)Enum.Parse(typeof(SentioModules), moduleName);
                if (module == SentioModules.Vision)
                // You can also activate tab pages:
                {
                    Sentio.SetActiveModule(module, "Automation");
                }
                else
                {
                    Sentio.SetActiveModule(module);
                }

                Thread.Sleep(500);
                GrabActiveModuleImpl();
            }
            catch (CommunicationObjectFaultedException exc)
            {
                LogLines.Add(exc.Message);
                MessageBox.Show(exc.Message);
            }
            catch (Exception exc)
            {
                LogLines.Add(exc.Message);
                MessageBox.Show(exc.Message);
            }
        }

        private void ShowHintImpl()
        {
            Sentio?.ShowHint(Hint);
        }

        private async void StepFirstDieImpl()
        {
            try
            {
                if (Sentio == null)
                {
                    return;
                }

                // Step to first die in route, subsite 0
                RemoteCommandResponse resp = await Sentio.ExecuteRemoteCommand("map:step_first_die", 0);
                if (resp.ErrorCode != 0)
                {
                    LogLines.Add(resp.Message);
                }
            }
            catch (Exception exc)
            {
                LogLines.Add(exc.Message);
            }
        }

        private async void StepNextDieImpl()
        {
            try
            {
                if (Sentio == null)
                {
                    return;
                }

                // Step to first die in route, subsite 0
                RemoteCommandResponse resp = await Sentio.ExecuteRemoteCommand("map:step_next_die", null);
                if (resp.ErrorCode != 0)
                {
                    LogLines.Add(resp.Message);
                }
            }
            catch (Exception exc)
            {
                LogLines.Add(exc.Message);
                Sentio?.ShowHint(exc.Message);
            }
        }

        private void SwitchCameraImpl(string camera)
        {
            if (string.IsNullOrEmpty(camera))
            {
                return;
            }

            Sentio?.ExecuteModuleCommand(Defaults.VisionModuleName, "SwitchToCamera", camera);
        }

        private async void TestAll()
        {
            try
            {
                Sentio.SetRemoteMode(true);

                RemoteCommandResponse resp = await Sentio.ExecuteRemoteCommand("map:step_first_die", 0);
                if (resp.ErrorCode != 0)
                {
                    throw new Exception(resp.Message);
                }

                var rnd = new Random();
                while (resp.ErrorCode == 0)
                {
                    resp = await Sentio.ExecuteRemoteCommand("map:bin_step_next_die", rnd.Next(0, 5));
                    if (resp.ErrorCode != 0)
                    {
                        throw new Exception(resp.Message);
                    }
                }
            }
            catch (Exception exc)
            {
                LogLines.Add(exc.Message);
                Sentio.ShowHint(exc.Message);
            }
            finally
            {
                Sentio.SetRemoteMode(false);
            }
        }
    }
}