using Bit.Core.Contracts;
using Bit.Core.Models;
using Bit.Core.Models.Events;
using Bit.Signalr.Contracts;
using Microsoft.AspNet.SignalR.Client;
using Prism.Events;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Essentials.Interfaces;

namespace Bit.Signalr.Implementations
{
    public class SignalrMessageReceiver : Bindable, IMessageReceiver
    {
        public IExceptionHandler ExceptionHandler { get; set; } = default!;

        public SignalRHttpClient SignalRHttpClient { get; set; } = default!;

        public IEventAggregator EventAggregator { get; set; } = default!;

        public IClientAppProfile ClientAppProfile { get; set; } = default!;

        public IHubConnectionFactory HubConnectionFactory { get; set; } = default!;

        public IClientTransportFactory ClientTransportFactory { get; set; } = default!;

        public IConnectivity Connectivity { get; set; } = default!;

        public IMainThread MainThread { get; set; } = default!;

        private bool _IsConnected;
        public virtual bool IsConnected
        {
            get => _IsConnected;
            set => SetProperty(ref _IsConnected, value);
        }

        private bool _ShouldBeConnected;

        /// <summary>
        /// <see cref="IMessageReceiver.ShouldBeConnected"/>
        /// </summary>
        public virtual bool ShouldBeConnected
        {
            get => _ShouldBeConnected;
            set => SetProperty(ref _ShouldBeConnected, value);
        }

        HubConnection? _hubConnection;
        IDisposable? _listener;
        bool _continueWork = true;
        DefaultServerSentEventsTransport? _serverSentEventsTransport;

        public virtual async Task Start(CancellationToken cancellationToken)
        {
            ShouldBeConnected = true;

            if (_hubConnection == null)
            {
                _hubConnection = HubConnectionFactory(ClientAppProfile);

                IHubProxy hubProxy = _hubConnection.CreateHubProxy("MessagesHub");

                _listener = hubProxy.On<string, string>("OnMessageReceived", (key, dataAsJson) =>
                {
                    MessageReceivedEvent @event = new MessageReceivedEvent
                    {
                        Key = key,
                        Body = dataAsJson
                    };

                    EventAggregator.GetEvent<MessageReceivedEvent>().Publish(@event);
                });

                _hubConnection.ConnectionSlow += OnConnectionSlow;
                _hubConnection.Reconnected += OnReconnected;
                _hubConnection.Closed += OnClosed;
                _hubConnection.Reconnecting += OnReconnecting;
                _hubConnection.StateChanged += OnStateChanged;

                DependencyDelegates.Current.StartTimer?.Invoke(TimeSpan.FromSeconds(5), OnSchedule);

                Connectivity.ConnectivityChanged += OnConnectivityChanged;
            }

            EnsureReNewTransport();

            await _hubConnection.Start(_serverSentEventsTransport).ConfigureAwait(false);
        }

        void EnsureReNewTransport()
        {
            try
            {
                if (_serverSentEventsTransport != null)
                {
                    _serverSentEventsTransport.OnDisconnected -= OnDisconnected;
                    _serverSentEventsTransport.Dispose();
                }
            }
            finally
            {
                _serverSentEventsTransport = ClientTransportFactory(SignalRHttpClient);

                _serverSentEventsTransport.OnDisconnected += OnDisconnected;
            }
        }

        void ReportStatus(bool isConnected)
        {
            if (IsConnected != isConnected)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    IsConnected = isConnected;

                    EventAggregator
                        .GetEvent<ServerConnectivityChangedEvent>()
                        .Publish(new ServerConnectivityChangedEvent
                        {
                            IsConnected = IsConnected
                        });
                });
            }
        }

        bool _isDoingOnSchedule;

        bool OnSchedule()
        {
            if (ShouldBeConnected && IsConnected == false && _continueWork && _isDoingOnSchedule == false)
            {
                _isDoingOnSchedule = true;

                Task.Run(async () =>
                {
                    try
                    {
                        await Stop(CancellationToken.None).ConfigureAwait(false);
                    }
                    catch (Exception exp)
                    {
                        ExceptionHandler.OnExceptionReceived(exp);
                    }

                    try
                    {
                        await Start(CancellationToken.None).ConfigureAwait(false);
                    }
                    catch (Exception exp)
                    {
                        ExceptionHandler.OnExceptionReceived(exp);
                    }
                    finally
                    {
                        _isDoingOnSchedule = false;
                    }
                });
            }

            return _continueWork;
        }

        void OnDisconnected()
        {
            ReportStatus(isConnected: false);
        }

        void OnConnectionSlow()
        {
            ReportStatus(isConnected: false);
        }

        void OnStateChanged(StateChange e)
        {
            ReportStatus(isConnected: e.NewState == ConnectionState.Connected);
        }

        void OnReconnecting()
        {
            ReportStatus(isConnected: false);
        }

        void OnReconnected()
        {
            ReportStatus(isConnected: true);
        }

        void OnClosed()
        {
            ReportStatus(isConnected: false);
        }

        void OnConnectivityChanged(object? sender, Xamarin.Essentials.ConnectivityChangedEventArgs e)
        {
            if (e.NetworkAccess != Xamarin.Essentials.NetworkAccess.Internet)
                ReportStatus(isConnected: false);
        }

        public async Task Stop(CancellationToken cancellationToken)
        {
            ShouldBeConnected = false;
            if (_hubConnection != null)
                await Task.Run(_hubConnection.Stop, cancellationToken).ConfigureAwait(false);
        }

        public void Dispose()
        {
            _continueWork = false;

            if (_hubConnection != null)
            {
                _hubConnection.ConnectionSlow -= OnConnectionSlow;
                _hubConnection.Reconnected -= OnReconnected;
                _hubConnection.Closed -= OnClosed;
                _hubConnection.Reconnected -= OnReconnected;
                _hubConnection.Reconnecting -= OnReconnecting;
                _hubConnection.StateChanged -= OnStateChanged;
            }

            if (_serverSentEventsTransport != null)
            {
                _serverSentEventsTransport.OnDisconnected -= OnDisconnected;
            }


            Connectivity.ConnectivityChanged -= OnConnectivityChanged;

            _listener?.Dispose();
            _serverSentEventsTransport?.Dispose();
            _hubConnection?.Dispose();
        }
    }
}
