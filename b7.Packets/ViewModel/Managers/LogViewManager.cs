﻿using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;

using Xabbo.Common;
using Xabbo.Interceptor;
using Xabbo.Messages;

using b7.Packets.Composer;
using b7.Packets.Services;
using b7.Packets.Util;

namespace b7.Packets.ViewModel
{
    public class LogViewManager : ObservableObject
    {
        private readonly IContext _context;
        private readonly IRemoteInterceptor _interceptor;

        protected Incoming In => _interceptor.Messages.In;
        protected Outgoing Out => _interceptor.Messages.Out;

        private readonly PacketComposer _composer;

        private readonly ObservableCollection<PacketLogViewModel> _logs;
        public ICollectionView Logs { get; }

        private bool _isConnected;
        public bool IsConnected
        {
            get => _isConnected;
            set => Set(ref _isConnected, value);
        }

        private bool _isLogging;
        public bool IsLogging
        {
            get => _isLogging;
            set => Set(ref _isLogging, value);
        }

        private Direction _direction = Direction.Both;
        public Direction Direction
        {
            get => _direction;
            set => Set(ref _direction, value);
        }

        private string _filterText = string.Empty;
        public string FilterText
        {
            get => _filterText;
            set
            {
                if (Set(ref _filterText, value))
                    UpdateFilter();
            }
        }

        private PacketLogViewModel? _selectedItem;
        public PacketLogViewModel? SelectedItem
        {
            get => _selectedItem;
            set => Set(ref _selectedItem, value);
        }

        private StringBuilder _packetInfo;
        public string PacketInfo
        {
            get => _packetInfo?.ToString() ?? string.Empty;
            set
            {
                _packetInfo = new StringBuilder(value);
                RaisePropertyChanged(nameof(PacketInfo));
            }
        }

        private bool _blockPackets;
        public bool BlockPackets
        {
            get => _blockPackets;
            set => Set(ref _blockPackets, value);
        }

        private bool _blockIncoming;
        public bool BlockIncoming
        {
            get => _blockIncoming;
            set => Set(ref _blockIncoming, value);
        }

        private bool _blockOutgoing = true;
        public bool BlockOutgoing
        {
            get => _blockOutgoing;
            set => Set(ref _blockOutgoing, value);
        }

        private bool _blockBoth;
        public bool BlockBoth
        {
            get => _blockBoth;
            set => Set(ref _blockBoth, value);
        }

        private string _composerText = string.Empty;
        public string ComposerText
        {
            get => _composerText;
            set => Set(ref _composerText, value);
        }

        public ICommand ToggleLoggingCommand { get; }
        public ICommand ClearLogsCommand { get; }
        public ICommand LoadInStructuralizerCommand { get; }

        public ICommand SendToClientCommand { get; }
        public ICommand SendToServerCommand { get; }

        public LogViewManager(IContext context, IRemoteInterceptor interceptor)
        {
            _context = context;
            _interceptor = interceptor;

            _interceptor.Connected += OnConnectionStart;
            _interceptor.Disconnected += OnConnectionEnd;
            _interceptor.Intercepted += OnIntercepted;

            _composer = new PacketComposer(_interceptor);

            _logs = new ObservableCollection<PacketLogViewModel>();
            Logs = CollectionViewSource.GetDefaultView(_logs);
            Logs.Filter = FilterLog;

            _packetInfo = new StringBuilder();

            ToggleLoggingCommand = new RelayCommand(ToggleLogging);
            ClearLogsCommand = new RelayCommand(ClearLogs);
            LoadInStructuralizerCommand = new RelayCommand(LoadInStructuralizer);

            SendToClientCommand = new RelayCommand(SendToClient);
            SendToServerCommand = new RelayCommand(SendToServer);
        }

        private void UpdateFilter()
        {
            if (!_context.IsSynchronized)
            {
                _context.Invoke(UpdateFilter);
                return;
            }

            Logs.Refresh();
        }

        private bool FilterLog(object o)
        {
            if (o is not PacketLogViewModel log ||
                string.IsNullOrWhiteSpace(_filterText))
            {
                return true;
            }

            return log.Name.Contains(_filterText.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        private void ToggleLogging() => IsLogging = !IsLogging;

        private void AddLog(InterceptArgs e)
        {
            string? name = e.Packet.Header.GetName(e.Packet.Protocol);
            if (name is null)
                name = e.Packet.Header.GetValue(e.Packet.Protocol).ToString();

            AddLog(new PacketLogViewModel
            {
                Packet = e.Packet.Copy(),
                DirectionPointer = e.IsOutgoing ? ">>" : "<<",
                Id = e.Packet.Header.GetValue(_interceptor.Client),
                IsFlashName = _interceptor.Client == ClientType.Flash,
                IsUnityName = _interceptor.Client == ClientType.Unity,
                IsOutgoing = e.Packet.Header.IsOutgoing,
                Length = e.Packet.Length,
                Name = name,
                Timestamp = DateTime.Now,
                IsBlocked = e.IsBlocked
            });
        }

        private void AddLog(PacketLogViewModel log)
        {
            if (!_context.IsSynchronized)
            {
                _context.Invoke(() => AddLog(log));
                return;
            }

            _logs.Add(log);
        }

        private void ClearLogs()
        {
            if (!_context.IsSynchronized)
            {
                _context.Invoke(() => ClearLogs());
                return;
            }

            _logs.Clear();
        }

        private void LoadInStructuralizer()
        {
            if (SelectedItem == null) return;

            Messenger.Default.Send(new GenericMessage<PacketLogViewModel>(this, SelectedItem));
        }

        private void SendToClient()
        {
            try
            {
                IPacket packet = _composer.ComposePacket(Destination.Client, ComposerText);
                _interceptor.Send(packet);

                AddLog(new InterceptArgs(_interceptor, Destination.Client, packet));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Compose error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SendToServer()
        {
            try
            {
                IPacket packet = _composer.ComposePacket(Destination.Server, ComposerText);
                _interceptor.Send(packet);

                AddLog(new InterceptArgs(_interceptor, Destination.Server, packet));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Compose error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnConnectionStart(object? sender, EventArgs e)
        {
            IsConnected = true;
        }

        private void OnConnectionEnd(object? sender, EventArgs e)
        {
            IsConnected = false;
            IsLogging = false;
        }

        private void OnIntercepted(object? sender, InterceptArgs e)
        {
            bool isEssentialPacket =
                e.Packet.Header == In.Ping ||
                e.Packet.Header == Out.Pong ||
                e.Packet.Header == In.ClientLatencyPingResponse ||
                e.Packet.Header == Out.ClientLatencyPingRequest;

            if (BlockPackets)
            {
                if (!isEssentialPacket &&
                    (BlockBoth ||
                    (e.Destination == Destination.Server && BlockOutgoing) ||
                    (e.Destination == Destination.Client && BlockIncoming)))
                {
                    e.Block();
                }
            }

            if (!IsLogging) return;
            if (e.IsIncoming && !Direction.HasFlag(Direction.Incoming)) return;
            if (e.IsOutgoing && !Direction.HasFlag(Direction.Outgoing)) return;

            // TODO add a hidden flag to messages and ignore them here, hard-coded for now

            if (e.Packet.Header == In.Ping ||
                e.Packet.Header == Out.Pong ||
                e.Packet.Header == In.ClientLatencyPingResponse ||
                e.Packet.Header == Out.ClientLatencyPingRequest)
            {
                return;
            }

            AddLog(e);
        }

        public void UpdateSelection(IList selection)
        {
            _packetInfo.Clear();

            foreach (var packetLog in selection.OfType<PacketLogViewModel>().OrderBy(x => x.Timestamp))
            {
                if (_packetInfo.Length > 0)
                {
                    _packetInfo.AppendLine();
                    _packetInfo.AppendLine();
                }

                AppendPacketInfo(_packetInfo, packetLog);
            }

            RaisePropertyChanged(nameof(PacketInfo));
        }

        private void AppendPacketInfo(StringBuilder sb, PacketLogViewModel packetLog)
        {
            var packet = packetLog.Packet;

            sb.Append(packetLog.DirectionPointer);
            sb.Append(" [");
            sb.Append(packetLog.Timestamp.ToString("HH:mm:ss.fff"));
            sb.Append("] ");
            sb.Append(packetLog.Name);
            if (packetLog.Packet.Header.Name != null)
            {
                sb.Append(" (");
                sb.Append(packetLog.Packet.Header.GetValue(_interceptor.Client));
                sb.Append(')');
            }

            if (packetLog.Length > 0)
            {
                sb.AppendLine();
                sb.AppendLine();

                ReadOnlySpan<byte> data = packet.Buffer;

                int rows = (data.Length - 1) / 16 + 1;
                for (int i = 0; i < rows; i++)
                {
                    if (i > 0)
                        sb.AppendLine();

                    for (int j = 0; j < 16; j++)
                    {
                        int k = i * 16 + j;
                        if (k < data.Length)
                            sb.Append($"{data[k],-3:x2}");
                        else
                            sb.Append($"{"",3}");
                    }

                    sb.Append("   ");

                    for (int j = 0; j < 16; j++)
                    {
                        int k = i * 16 + j;
                        if (k < data.Length)
                            sb.Append(StringUtil.GetAsciiChar(data[k]));
                    }
                }
            }
        }
    }
}
