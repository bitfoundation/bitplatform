using Bit.Core.Contracts;
using Bit.Core.Models;
using Bit.Core.Models.Events;
using Bit.Signalr.Contracts;
using Microsoft.AspNet.SignalR.Client;
using Prism.Events;
using System;
using System.Threading;
using System.Threading.Tasks;

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

#if Xamarin
                Xamarin.Essentials.Connectivity.ConnectivityChanged += OnConnectivityChanged;
#elif Maui
                Microsoft.Maui.Essentials.Connectivity.ConnectivityChanged += OnConnectivityChanged;
#endif
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
#if Xamarin
                Xamarin.Essentials.MainThread.BeginInvokeOnMainThread(() =>
                {
                    IsConnected = isConnected;

                    EventAggregator
                        .GetEvent<ServerConnectivityChangedEvent>()
                        .Publish(new ServerConnectivityChangedEvent
                        {
                            IsConnected = IsConnected
                        });
                });
#elif Maui
                Microsoft.Maui.Essentials.MainThread.BeginInvokeOnMainThread(() =>
                {
                    IsConnected = isConnected;

                    EventAggregator
                        .GetEvent<ServerConnectivityChangedEvent>()
                        .Publish(new ServerConnectivityChangedEvent
                        {
                            IsConnected = IsConnected
                        });
                });
#else
                IsConnected = isConnected;

                EventAggregator
                    .GetEvent<ServerConnectivityChangedEvent>()
                    .Publish(new ServerConnectivityChangedEvent
                    {
                        IsConnected = IsConnected
                    });
#endif
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

#if Xamarin
        void OnConnectivityChanged(object? sender, Xamarin.Essentials.ConnectivityChangedEventArgs e)
        {
            if (e.NetworkAccess != Xamarin.Essentials.NetworkAccess.Internet)
                ReportStatus(isConnected: false);
        }
#elif Maui
        void OnConnectivityChanged(object? sender, Microsoft.Maui.Essentials.ConnectivityChangedEventArgs e)
        {
            if (e.NetworkAccess != Microsoft.Maui.Essentials.NetworkAccess.Internet)
                ReportStatus(isConnected: false);
        }
#endif

        public async Task Stop(CancellationToken cancellationToken)
        {
            ShouldBeConnected = false;
            if (_hubConnection != null)
                await Task.Run(_hubConnection.Stop, cancellationToken).ConfigureAwait(false);
        }

        private bool isDisposed;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (isDisposed) return;

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


#if Xamarin
                Xamarin.Essentials.Connectivity.ConnectivityChanged -= OnConnectivityChanged;
#elif Maui
                Microsoft.Maui.Essentials.Connectivity.ConnectivityChanged -= OnConnectivityChanged;
#endif

            _listener?.Dispose();
            _serverSentEventsTransport?.Dispose();
            _hubConnection?.Dispose();

            isDisposed = true;
        }

        ~SignalrMessageReceiver()
        {
            Dispose(false);
        }
    }
}
