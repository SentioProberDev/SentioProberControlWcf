using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.Windows.Input;

using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Threading;

using Sentio.Contracts;
using Sentio.Contracts.Enumerations;
using Sentio.Contracts.Helper;

// ReSharper disable IdentifierTypo
// ReSharper disable SuspiciousTypeConversion.Global


namespace Sentio.WcfTest.ViewModel
{
    public abstract class SentioWcfClientBase : ViewModelBase, ISentioWcfClient
    {
        private readonly List<ClientControl> _listClientUi = new List<ClientControl>();

        private SentioModules _activeModule;

        private bool _isConnected;

        private bool _isInRemoteMode;

        private string _sentioVersion;

        private DuplexChannelFactory<ISentioService> _factory;

        private InstanceContext _communicationObject;

        private SentioSessionInfo _session;

        private SentioCompatibilityLevel _sentioCompateLevel = SentioCompatibilityLevel.Default;

        public SentioCompatibilityLevel SentioCompatLevel => _sentioCompateLevel;

        public SentioModules ActiveModule
        {
            get => _activeModule;
            set => Set(ref _activeModule, value);
        }

        public SentioSessionInfo Session
        {
            get => _session;
            set => Set(ref _session, value);
        }

        public List<ClientControl> ClientUi
        {
            get => _listClientUi;

            set
            {
                _listClientUi.Clear();

                foreach (ClientControl item in value)
                {
                    _listClientUi.Add(item);
                }
            }
        }

        public bool IsConnected
        {
            get => _isConnected;
            set => Set(ref _isConnected, value);
        }

        public bool IsInRemoteMode
        {
            get => _isInRemoteMode;

            set
            {
                Set(ref _isInRemoteMode, value);
                Sentio?.SetRemoteMode(value);
            }
        }

        public ISentioService Sentio { get; protected set; }

        public string SentioVersion
        {
            get => _sentioVersion;
            set => Set(ref _sentioVersion, value);
        }

        public void Connect(string clientName, string remotePrefix = "")
        {
            try
            {
                var callback = new SentioCallbackImpl(this);

                bool tcpService = true;

                _communicationObject = new InstanceContext(callback);

                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                if (tcpService)
                {
                    int MaxWcfMessageSize = 100 * 1024 * 1024;
                    var netTcpBinding = new NetTcpBinding
                    {
                        MaxBufferPoolSize = MaxWcfMessageSize,
                        MaxBufferSize = MaxWcfMessageSize,
                        MaxReceivedMessageSize = MaxWcfMessageSize,
                    };

                    _factory = new DuplexChannelFactory<ISentioService>(
                        _communicationObject,
                        netTcpBinding,
                        new EndpointAddress("net.tcp://localhost:35556/Sentio/SentioService"));
                }
                else
                // ReSharper disable once HeuristicUnreachableCode
                {
                    _factory = new DuplexChannelFactory<ISentioService>(
                        _communicationObject, 
                        new NetNamedPipeBinding(NetNamedPipeSecurityMode.None),
                        new EndpointAddress("net.pipe://localhost/Sentio/SentioService"));

                }

                Sentio = _factory.CreateChannel();
                Sentio.AsClientChannel().Faulted += SentioChannelFaulted;
                Sentio.AsClientChannel().Closed += SentioChannelClosed;

                // Set the compatibility level 
//                _sentioCompateLevel = SentioCompatibilityLevel.Sentio_3_6;
                
                Sentio.OpenWcfSession(clientName, _sentioCompateLevel);

                SentioVersion = Sentio.Version;
                ActiveModule = Sentio.ActiveModule;
                IsInRemoteMode = Sentio.IsInRemoteMode;

                // Session info was added in SENTIO 3.6
                if ((int)_sentioCompateLevel >= (int)SentioCompatibilityLevel.Sentio_3_6)
                {
                    Session = Sentio.Session;
                }

                Sentio.SetupClientPanel(ClientUi);
                if (ClientUi.Count >= 0)
                {
                    Sentio.ShowClientPanel(true);
                }
                
                if (!string.IsNullOrEmpty(remotePrefix))
                {
                    Sentio.EnableRemoteCommandCallbacks(remotePrefix);
                }

            }
            finally
            {
                IsConnected = Sentio != null;
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public abstract void OnChannelClosed();

        private void SentioChannelClosed(object sender, EventArgs e)
        {
            OnChannelClosed();
        }

        public abstract void OnChannelFaulted();

        private void SentioChannelFaulted(object sender, EventArgs e)
        {
            OnChannelFaulted();
        }

        public virtual void DisconnectClient(bool closeWcfConnection = true)
        {
            try
            {
                SentioVersion = "";
                ActiveModule = SentioModules.Unknown;

                try
                {
                    if (closeWcfConnection && Sentio.AsClientChannel()?.State != CommunicationState.Faulted)
                        Sentio?.CloseWcfSession();
                }
                catch(Exception exc)
                {
                    Console.WriteLine(exc.Message);
                }
                finally
                {
                    // Channel must be closed explicitely
                    // (https://docs.microsoft.com/de-de/dotnet/framework/wcf/feature-details/how-to-use-the-channelfactory)
                    // ReSharper disable once SuspiciousTypeConversion.Global
                    Sentio.AsClientChannel()?.Close();

                    Sentio = null;
                }

                RaisePropertyChanged(string.Empty);
                CommandManager.InvalidateRequerySuggested();
            }
            finally
            {
                IsConnected = Sentio != null;
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public virtual RemoteCommandResponse ExecuteExternalRemoteCommand(string cmd, string param)
        {
            return new RemoteCommandResponse
            {
                Message = $"Remote command {cmd} is not supported!",
                ErrorCode = (int)SentioErrorCodes.InvalidCommand
            };
        }

        public virtual void NotifyActiveModuleChanged(SentioModules module)
        {
            ActiveModule = module;
        }

        public virtual void NotifyButtonPressed(string btnId)
        {
            DispatcherHelper.CheckBeginInvokeOnUI(
                () =>
                {
                    var btn = ClientUi.Find(i => i.Id.Equals(btnId)) as ClientButton;
                    btn?.OnClick();
                });
        }

        public virtual void NotifyProjectSave(string project)
        { }

        public virtual void NotifyProjectLoad(string project)
        { }

        public virtual void NotifySessionChanged(SentioSessionInfo session)
        { }

        public virtual void NotifyWafermapViewportChange(double x, double y, double z)
        { }

        public virtual void NotifyRemoteModeChanged(bool state)
        {
            _isInRemoteMode = state;
            RaisePropertyChanged(nameof(IsInRemoteMode));

        }

        public virtual void NotifySentioShutdown()
        {
            DisconnectClient(false);
        }

        public virtual void NotifyWafermapDoubleClickOnDie(int col, int row, int n)
        { }

        public virtual void NotifyTextBoxUpdate(string tbId, string text)
        {
            DispatcherHelper.CheckBeginInvokeOnUI(
                () =>
                {
                    var tb = ClientUi.Find(i => i.Id.Equals(tbId)) as ClientTextBox;
                    tb?.OnTextChanged(text);
                });
        }

        public virtual void NotifyStepToCell(int col, int row, int site)
        { }
    }
}