using Bit.Model;
using Bit.Model.Events;
using Bit.ViewModel.Contracts;
using Microsoft.AspNet.SignalR.Client;
using Prism.Events;
using Prism.Services;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Essentials;

namespace Bit.ViewModel.Implementations
{
    public class SignalrMessageReceiver : Bindable, IMessageReceiver, IDisposable
    {
        public virtual SignalRHttpClient SignalRHttpClient { get; set; }

        public virtual IEventAggregator EventAggregator { get; set; }

        public virtual IDeviceService DeviceService { get; set; }

        public virtual bool IsConnected { get; set; }

        public virtual IClientAppProfile ClientAppProfile { get; set; }

        /// <summary>
        /// <see cref="IMessageReceiver.ShouldBeConnected"/>
        /// </summary>
        public virtual bool ShouldBeConnected { get; set; }

        HubConnection _hubConnection;
        IDisposable _listener;
        bool _continueWork = true;
        DefaultServerSentEventsTransport _serverSentEventsTransport;

        public virtual async Task Start(CancellationToken cancellationToken)
        {
            ShouldBeConnected = true;

            if (_hubConnection == null)
            {
                _hubConnection = new HubConnection($"{ClientAppProfile.HostUri}{ClientAppProfile.SignalrEndpint}")
                {
                    TransportConnectTimeout = TimeSpan.FromSeconds(3)
                };

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

                DeviceService.StartTimer(TimeSpan.FromSeconds(5), OnSchedule);

                Connectivity.ConnectivityChanged += OnConnectivityChanged;
            }

            EnsureReNewTransport();

            await _hubConnection.Start(_serverSentEventsTransport);
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
                _serverSentEventsTransport = new DefaultServerSentEventsTransport(SignalRHttpClient)
                {
                    ReconnectDelay = TimeSpan.FromSeconds(3)
                };

                _serverSentEventsTransport.OnDisconnected += OnDisconnected;
            }
        }

        void ReportStatus(bool isConnected)
        {
            if (IsConnected != isConnected)
            {
                IsConnected = isConnected;

                EventAggregator
                    .GetEvent<ServerConnectivityChangedEvent>()
                    .Publish(new ServerConnectivityChangedEvent
                    {
                        IsConnected = IsConnected
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
                        await Stop(CancellationToken.None);
                    }
                    catch (Exception exp)
                    {
                        BitExceptionHandler.Current.OnExceptionReceived(exp);
                    }

                    try
                    {
                        await Start(CancellationToken.None);
                    }
                    catch (Exception exp)
                    {
                        BitExceptionHandler.Current.OnExceptionReceived(exp);
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

        void OnConnectivityChanged(object sender, ConnectivityChangedEventArgs e)
        {
            if (e.NetworkAccess != NetworkAccess.Internet)
                ReportStatus(isConnected: false);
        }

        public Task Stop(CancellationToken cancellationToken)
        {
            ShouldBeConnected = false;
            _hubConnection?.Stop();
            return Task.CompletedTask;
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
