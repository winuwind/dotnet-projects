using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using Google.Protobuf;
using Snake.Config;
using Snake.Game;
using Snakes;

namespace Snake.Control;

public class Controller
{
    private readonly UdpClient _multicastClient;
    private readonly UdpClient _mainClient;

    private readonly
        ConcurrentDictionary<(IPAddress ip, int port), (long seq, List<GameAnnouncement> games, DateTime lastTime)>
        _announcements = new();

    private readonly ConcurrentDictionary<(IPAddress ip, int port), long> _lastSteerMsg = new();

    private readonly ConcurrentDictionary<long, (GameMessage msg, DateTime timeSent, IPAddress ip, int port)>
        _sentMessages = new();

    private readonly ConcurrentDictionary<int, DateTime> _lastMsgs = new();
    private readonly GamePlayer _currentPlayer;

    private SnakesGame _game;
    private GamePlayer _masterGamePlayer;
    private GamePlayer? _deputyGamePlayer;

    public event Action<string>? OnError;
    public event Action? OnGameOver;

    public event Action<ConcurrentDictionary<(IPAddress ip, int port), (long seq, List<GameAnnouncement> games, DateTime
        lastTime)>>? OnGamesList;

    public event Action<GameState>? Game;

    private long _numberMessage = 0;
    private bool _isRunning = true;
    private bool _gameInProcess = false;
    private bool _isGameOver = false;

    public Controller()
    {
        _multicastClient = new UdpClient();
        _multicastClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        _multicastClient.Client.Bind(new IPEndPoint(IPAddress.Any, AppConstant.MulticastPort));
        _multicastClient.JoinMulticastGroup(AppConstant.MulticastAddress, IPAddress.Any);

        _mainClient = new UdpClient();

        _currentPlayer = new GamePlayer
        {
            Role = NodeRole.Normal,
            Score = 0,
            Name = "Anonym"
        };
        _masterGamePlayer = _currentPlayer;
        _deputyGamePlayer = null;

        _game = new SnakesGame(AppConstant.Size, _currentPlayer, "");

        var bytes = new GameMessage
        {
            Discover = new GameMessage.Types.DiscoverMsg()
        }.ToByteArray();
        _mainClient.Send(bytes, bytes.Length, new IPEndPoint(AppConstant.MulticastAddress, AppConstant.MulticastPort));

        new Thread(ListenMulticastGroup).Start();
        new Thread(ListenMain).Start();
    }

    public int GetId()
    {
        return _currentPlayer.Id;
    }

    public void SetPlayerName(string name)
    {
        _currentPlayer.Name = name;
    }

    public void ChangeDirection(Direction direction)
    {
        if (_gameInProcess && !_isGameOver && _currentPlayer.Role != NodeRole.Viewer)
        {
            _game.ChangeDirectionSnake(_currentPlayer.Id, direction);
            if (_currentPlayer.Role != NodeRole.Master)
            {
                SendSteerMsg(direction);
            }
        }
    }

    private void UpdateUserInterface()
    {
        Game?.Invoke(_game.GetState());
    }

    private void GameOver()
    {
        _isGameOver = true;
        OnGameOver?.Invoke();
    }

    public void Close()
    {
        _isRunning = false;
        _gameInProcess = false;
        _multicastClient.Close();
        _mainClient.Close();
    }

    public void RequestGamesList()
    {
        OnGamesList?.Invoke(_announcements);
    }

    public void ExitGame()
    {
        _gameInProcess = false;
        if (_currentPlayer.Role == NodeRole.Master)
        {
            if (_deputyGamePlayer != null)
            {
                SendRoleChangeMsg(_deputyGamePlayer, NodeRole.Viewer, NodeRole.Master);
                _masterGamePlayer = _deputyGamePlayer;
                _deputyGamePlayer = null;
            }
            else
            {
                foreach (var playerNewMaster in _game.GetState().Players.Players)
                {
                    if (playerNewMaster.Role == NodeRole.Normal)
                    {
                        SendRoleChangeMsg(playerNewMaster, NodeRole.Viewer, NodeRole.Master);
                        _masterGamePlayer = playerNewMaster;
                        break;
                    }
                }
            }

            _currentPlayer.Role = NodeRole.Viewer;

            _sentMessages.Clear();
            _lastMsgs.Clear();
            _lastSteerMsg.Clear();
        }
        else if (_currentPlayer.Role != NodeRole.Viewer)
        {
            SendRoleChangeMsg(_masterGamePlayer, NodeRole.Viewer, NodeRole.Master);
            _currentPlayer.Role = NodeRole.Viewer;
        }
    }

    public void StartNewGame(string gameName)
    {
        _gameInProcess = true;
        _isGameOver = false;

        _currentPlayer.Role = NodeRole.Master;
        _currentPlayer.Score = 0;
        _masterGamePlayer = _currentPlayer;
        _deputyGamePlayer = null;

        _game = new SnakesGame(AppConstant.Size, _currentPlayer, gameName);

        _gameInProcess = true;

        new Thread(GameProcess).Start();
        new Thread(WorkWithSentMessages).Start();
        new Thread(CheckPlayers).Start();
        new Thread(SenderAnnouncementMsgs).Start();
    }

    private void SenderAnnouncementMsgs()
    {
        while (_gameInProcess)
        {
            Thread.Sleep(AppConstant.DelayAnnouncementMsg);

            if (_currentPlayer.Role == NodeRole.Master)
            {
                SendAnnouncementMsg(AppConstant.MulticastAddress, AppConstant.MulticastPort);
            }

            if (_currentPlayer.Role == NodeRole.Viewer)
            {
                break;
            }
        }
    }

    public void JoinToGame(IPAddress ip, int port, NodeRole role)
    {
        _gameInProcess = true;

        _isGameOver = role == NodeRole.Viewer ? true : false;

        var gameAnnouncement = _announcements[(ip, port)].games.First();
        foreach (var player in gameAnnouncement.Players.Players)
        {
            if (player.Role == NodeRole.Master)
            {
                _masterGamePlayer = player;
                _masterGamePlayer.IpAddress = ip.ToString();
                _masterGamePlayer.Port = port;
                break;
            }
        }

        _currentPlayer.Role = role;

        AppConstant.StaticFood = gameAnnouncement.Config.FoodStatic;
        AppConstant.Size = new GameState.Types.Coord
        {
            X = gameAnnouncement.Config.Width,
            Y = gameAnnouncement.Config.Height
        };

        _game = new SnakesGame(AppConstant.Size, gameAnnouncement.GameName);

        SendJoinMsg();

        new Thread(GameProcess).Start();
        new Thread(WorkWithSentMessages).Start();
        new Thread(CheckPlayers).Start();
        new Thread(SenderAnnouncementMsgs).Start();
    }

    private void WorkWithSentMessages()
    {
        while (_gameInProcess)
        {
            Thread.Sleep(AppConstant.DelayBetweenRepeatMsg);
            foreach (var seq in _sentMessages.Keys)
            {
                if (_sentMessages.TryGetValue(seq, out var value) && (DateTime.Now - value.timeSent).TotalMilliseconds >
                    AppConstant.DelayBetweenRepeatMsg)
                {
                    var id = -1;
                    foreach (var player in _game.GetState().Players.Players)
                    {
                        if (player.IpAddress == value.ip.ToString() && player.Port == value.port)
                        {
                            id = player.Id;
                        }
                    }

                    if (!_lastMsgs.ContainsKey(id))
                    {
                        _sentMessages.TryRemove(seq, out _);
                        continue;
                    }

                    if (value.msg.TypeCase is GameMessage.TypeOneofCase.RoleChange
                        or GameMessage.TypeOneofCase.State
                        or GameMessage.TypeOneofCase.Ping)
                    {
                        byte[] bytes;
                        try
                        {
                            bytes = value.msg.ToByteArray();
                        }
                        catch
                        {
                            continue;
                        }

                        try
                        {
                            _mainClient.Send(bytes, bytes.Length, new IPEndPoint(value.ip, value.port));
                            Console.WriteLine($"RESEND {value.msg.TypeCase}: {value.msg.MsgSeq}");
                        }
                        catch (Exception ex)
                        {
                            if (_isRunning)
                            {
                                OnError?.Invoke(ex.Message);
                                continue;
                            }

                            return;
                        }

                        value.timeSent = DateTime.Now;
                    }
                    else if (value.msg.TypeCase is GameMessage.TypeOneofCase.Steer)
                    {
                        if (_currentPlayer.Role == NodeRole.Master)
                        {
                            _sentMessages.TryRemove(seq, out _);
                            continue;
                        }

                        byte[] bytes;
                        try
                        {
                            bytes = value.msg.ToByteArray();
                        }
                        catch
                        {
                            continue;
                        }

                        try
                        {
                            _mainClient.Send(bytes, bytes.Length,
                                new IPEndPoint(IPAddress.Parse(_masterGamePlayer.IpAddress), _masterGamePlayer.Port));


                            Console.WriteLine($"RESEND {value.msg.TypeCase}: {value.msg.MsgSeq}");
                        }
                        catch (Exception ex)
                        {
                            if (_isRunning)
                            {
                                OnError?.Invoke(ex.Message);
                                continue;
                            }

                            return;
                        }

                        value.ip = IPAddress.Parse(_masterGamePlayer.IpAddress);
                        value.port = _masterGamePlayer.Port;
                        value.timeSent = DateTime.Now;
                    }
                    else
                    {
                        _sentMessages.TryRemove(seq, out _);
                        OnError?.Invoke("Game is not exist");
                    }
                }
            }
        }
    }

    private void DeleteSomeMessages(IPAddress ip, int port)
    {
        foreach (var seq in _sentMessages.Keys)
        {
            if (_sentMessages.TryGetValue(seq, out var value) && Equals(value.ip, ip) && value.port == port)
            {
                if (value.msg.TypeCase is not GameMessage.TypeOneofCase.Steer
                        and not GameMessage.TypeOneofCase.RoleChange
                    || _currentPlayer.Role == NodeRole.Master)
                {
                    _sentMessages.TryRemove(seq, out _);
                }
                else
                {
                    value.ip = IPAddress.Parse(_masterGamePlayer.IpAddress);
                    value.port = _masterGamePlayer.Port;
                }
            }
        }
    }

    private void CheckPlayers()
    {
        while (_gameInProcess)
        {
            Thread.Sleep(AppConstant.DelayBetweenMsgs);
            foreach (var playerId in _lastMsgs.Keys)
            {
                if (_lastMsgs.TryGetValue(playerId, out var time) &&
                    (DateTime.Now - time).TotalMilliseconds >= AppConstant.PlayerTimeout)
                {
                    if (playerId == _masterGamePlayer.Id)
                    {
                        if (_currentPlayer.Role == NodeRole.Deputy)
                        {
                            _deputyGamePlayer = null;
                            ChangeRoleToMaster();
                            _masterGamePlayer = _currentPlayer;
                            _currentPlayer.Role = NodeRole.Master;
                            _game.UpdateLastId();
                        }
                        else if (_deputyGamePlayer != null &&
                                 _currentPlayer.Role is NodeRole.Normal or NodeRole.Viewer)
                        {
                            _masterGamePlayer = _deputyGamePlayer;
                            _deputyGamePlayer = null;
                        }
                        else
                        {
                            _gameInProcess = false;
                            break;
                        }
                    }
                    else if (_currentPlayer.Id == _masterGamePlayer.Id && playerId == _deputyGamePlayer?.Id)
                    {
                        _deputyGamePlayer.Role = NodeRole.Normal;
                        _deputyGamePlayer = null;
                        foreach (var player in _game.GetState().Players.Players)
                        {
                            if (player.Role == NodeRole.Normal && playerId != player.Id)
                            {
                                SendRoleChangeMsg(player, NodeRole.Master, NodeRole.Deputy);
                                _deputyGamePlayer = player;
                                player.Role = NodeRole.Deputy;
                                break;
                            }
                        }
                    }

                    var ip = IPAddress.Any;
                    var port = 0;
                    for (var i = 0; i < _game.GetState().Players.Players.Count; i++)
                    {
                        var player = _game.GetState().Players.Players[i];
                        if (player.Id == playerId)
                        {
                            ip = IPAddress.Parse(player.IpAddress);
                            port = player.Port;
                            _game.GetState().Players.Players.RemoveAt(i);
                            break;
                        }
                    }

                    DeleteSomeMessages(ip, port);

                    for (var i = 0; i < _game.GetState().Players.Players.Count; i++)
                    {
                        if (playerId == _game.GetState().Players.Players[i].Id)
                        {
                            _game.GetState().Players.Players.RemoveAt(i);
                            break;
                        }
                    }

                    _lastMsgs.TryRemove(playerId, out _);
                }
                else if ((DateTime.Now - time).TotalMilliseconds >= AppConstant.DelayBetweenMsgs)
                {
                    foreach (var player in _game.GetState().Players.Players)
                    {
                        if (player.Id == _masterGamePlayer.Id)
                        {
                            player.IpAddress = _masterGamePlayer.IpAddress;
                            player.Port = _masterGamePlayer.Port;
                        }

                        if (player.Id == playerId)
                        {
                            if (_currentPlayer.Role != NodeRole.Master && player.Role != NodeRole.Master)
                            {
                                var ip = IPAddress.Parse(player.IpAddress);
                                var port = player.Port;
                                DeleteSomeMessages(ip, port);
                                _lastMsgs.TryRemove(playerId, out _);
                                break;
                            }

                            SendPingMsg(IPAddress.Parse(player.IpAddress), player.Port);
                        }
                    }
                }
            }
        }
    }

    private void GameProcess()
    {
        UpdateUserInterface();
        while (_gameInProcess)
        {
            Thread.Sleep(AppConstant.StateDelayMs);
            if (_currentPlayer.Role == NodeRole.Master)
            {
                _lastSteerMsg.Clear();
                var listKilledPlayer = _game.Tick();
                UpdateUserInterface();

                var flagIsDied = false;
                var flagDeputyDied = false;

                foreach (var player in listKilledPlayer)
                {
                    if (player.Id == _currentPlayer.Id)
                    {
                        flagIsDied = true;
                    }
                    else if (player.Role is NodeRole.Deputy)
                    {
                        _deputyGamePlayer = null;
                        flagDeputyDied = true;
                        SendRoleChangeMsg(player, NodeRole.Master, NodeRole.Viewer);
                    }
                    else
                    {
                        SendRoleChangeMsg(player, NodeRole.Master, NodeRole.Viewer);
                    }
                }

                foreach (var player in _game.GetState().Players.Players)
                {
                    if (_currentPlayer.Id == player.Id)
                    {
                        continue;
                    }

                    SendStateMsg(IPAddress.Parse(player.IpAddress), player.Port);
                }

                if (flagIsDied)
                {
                    if (_deputyGamePlayer != null)
                    {
                        SendRoleChangeMsg(_deputyGamePlayer, NodeRole.Viewer, NodeRole.Master);
                        _masterGamePlayer = _deputyGamePlayer;
                        _deputyGamePlayer = null;
                    }

                    foreach (var player in _game.GetState().Players.Players)
                    {
                        if (player.Id != _currentPlayer.Id && player.Id != _masterGamePlayer.Id)
                        {
                            SendRoleChangeMsg(player, NodeRole.Viewer, player.Role);
                        }
                    }

                    _currentPlayer.Role = NodeRole.Viewer;
                    GameOver();
                    _sentMessages.Clear();
                }
                else if (flagDeputyDied)
                {
                    foreach (var player in _game.GetState().Players.Players)
                    {
                        if (player.Id != _currentPlayer.Id && player.Role is NodeRole.Normal)
                        {
                            player.Role = NodeRole.Deputy;
                            _deputyGamePlayer = player;
                            SendRoleChangeMsg(player, NodeRole.Master, NodeRole.Deputy);
                            break;
                        }
                    }
                }
            }
            else if (_currentPlayer.Role == NodeRole.Viewer)
            {
                break;
            }
        }
    }

    private void ListenMulticastGroup()
    {
        while (_isRunning)
        {
            var remoteEp = new IPEndPoint(IPAddress.Any, 0);
            byte[] res;
            try
            {
                res = _multicastClient.Receive(ref remoteEp);
            }
            catch (Exception ex)
            {
                if (_isRunning)
                {
                    OnError?.Invoke(ex.Message);
                    continue;
                }

                return;
            }

            var sourceIp = remoteEp.Address;
            var sourcePort = remoteEp.Port;

            var msg = GameMessage.Parser.ParseFrom(res);
            if (msg.Announcement != null)
            {
                ReceiveAnnouncementMsg(sourceIp, sourcePort, msg);
            }

            if (msg.Discover != null && _currentPlayer.Role == NodeRole.Master)
            {
                SendAnnouncementMsg(sourceIp, sourcePort);
            }

            var time = DateTime.Now;
            var expired = _announcements
                .Where(kv => (time - kv.Value.lastTime).TotalMilliseconds > AppConstant.DelayDeleteAnnouncementMsg)
                .Select(kv => kv.Key).ToList();

            foreach (var ipAddress in expired)
            {
                _announcements.TryRemove(ipAddress, out var del);
            }
        }
    }

    private void ReceiveAnnouncementMsg(IPAddress sourceIp, int sourcePort, GameMessage msg)
    {
        var time = DateTime.Now;
        if (!_announcements.TryGetValue((sourceIp, sourcePort), out var announcement)
            || announcement.seq < msg.MsgSeq)
        {
            var games = msg.Announcement.Games;
            _announcements[(sourceIp, sourcePort)] = (msg.MsgSeq, games.ToList(), time);
        }
    }

    private void SendAnnouncementMsg(IPAddress sourceIp, int sourcePort)
    {
        var gameAnnouncement = new GameAnnouncement
        {
            Players = _game.GetState().Players,
            Config = new GameConfig
            {
                Width = AppConstant.Size.X,
                Height = AppConstant.Size.Y,
                FoodStatic = AppConstant.StaticFood,
                StateDelayMs = AppConstant.StateDelayMs
            },
            CanJoin = _game.CanJoin(),
            GameName = _game.GetName()
        };

        var announcementMsg = new GameMessage.Types.AnnouncementMsg
        {
            Games = { gameAnnouncement }
        };
        var msg = new GameMessage
        {
            Announcement = announcementMsg,
            MsgSeq = _numberMessage++
        };

        var remoteEndPoint = new IPEndPoint(sourceIp, sourcePort);
        var bytes = msg.ToByteArray();
        try
        {
            _mainClient.Send(bytes, bytes.Length, remoteEndPoint);
        }
        catch (Exception ex)
        {
            if (_isRunning)
            {
                OnError?.Invoke(ex.Message);
            }

            return;
        }
    }

    private void ReceiveErrorMsg(IPAddress sourceIp, int sourcePort, GameMessage msg)
    {
        if (_masterGamePlayer.IpAddress == sourceIp.ToString() && _masterGamePlayer.Port == sourcePort)
        {
            OnError?.Invoke(msg.Error.ErrorMessage);
            _sentMessages.TryRemove(msg.MsgSeq, out _);
        }
    }

    private void SendErrorMsg(IPAddress sourceIp, int sourcePort, string error, long seq)
    {
        var errorMsg = new GameMessage.Types.ErrorMsg
        {
            ErrorMessage = error
        };
        var msg = new GameMessage
        {
            Error = errorMsg,
            MsgSeq = seq
        };
        var remoteEndPoint = new IPEndPoint(sourceIp, sourcePort);
        var bytes = msg.ToByteArray();
        try
        {
            _mainClient.Send(bytes, bytes.Length, remoteEndPoint);
        }
        catch (Exception ex)
        {
            if (_isRunning)
            {
                OnError?.Invoke(ex.Message);
            }

            return;
        }


        Console.WriteLine($"Send Error: {msg.MsgSeq}");
    }

    private void ReceiveAckMsg(GameMessage msg)
    {
        _sentMessages.TryRemove(msg.MsgSeq, out var msgSent);
        if (msgSent.msg.Join != null)
        {
            _currentPlayer.Id = msg.ReceiverId;
            _masterGamePlayer.Id = msg.SenderId;
            OnError?.Invoke("My id: " + msg.ReceiverId + "\nMaster id: " + msg.SenderId);
        }
    }

    private void SendAckMsg(IPAddress sourceIp, int sourcePort, long seq)
    {
        var id = -1;
        foreach (var player in _game.GetState().Players.Players)
        {
            if (player.IpAddress == sourceIp.ToString() && player.Port == sourcePort)
            {
                id = player.Id;
                break;
            }
        }

        if (id == -1 && _currentPlayer.Role == NodeRole.Master)
        {
            _lastSteerMsg.TryRemove((sourceIp, sourcePort), out _);
            var error = "Error on master";
            SendErrorMsg(sourceIp, sourcePort, error, seq);
            return;
        }

        var ackMsg = new GameMessage.Types.AckMsg();
        var msg = new GameMessage
        {
            Ack = ackMsg,
            MsgSeq = seq,
            SenderId = _currentPlayer.Id,
            ReceiverId = id
        };
        var remoteEndPoint = new IPEndPoint(sourceIp, sourcePort);
        var bytes = msg.ToByteArray();
        try
        {
            _mainClient.Send(bytes, bytes.Length, remoteEndPoint);
        }
        catch (Exception ex)
        {
            if (_isRunning)
            {
                OnError?.Invoke(ex.Message);
            }

            return;
        }


        Console.WriteLine($"Send Ack: {msg.MsgSeq}");
    }

    private void SendPingMsg(IPAddress sourceIp, int sourcePort)
    {
        var pingMsg = new GameMessage.Types.PingMsg();
        var msg = new GameMessage
        {
            Ping = pingMsg,
            MsgSeq = _numberMessage++
        };
        var remoteEndPoint = new IPEndPoint(sourceIp, sourcePort);
        var bytes = msg.ToByteArray();
        try
        {
            _mainClient.Send(bytes, bytes.Length, remoteEndPoint);
        }
        catch (Exception ex)
        {
            if (_isRunning)
            {
                OnError?.Invoke(ex.Message);
            }

            return;
        }

        _sentMessages[msg.MsgSeq] = (msg, DateTime.Now, sourceIp, sourcePort);


        Console.WriteLine($"Send Ping: {msg.MsgSeq}");
    }

    private void ReceiveJoinMsg(IPAddress sourceIp, int sourcePort, GameMessage msg)
    {
        if (_currentPlayer.Role == NodeRole.Master &&
            msg.Join.GameName == _game.GetName() &&
            msg.Join.RequestedRole is NodeRole.Normal or NodeRole.Viewer)
        {
            var player = new GamePlayer
            {
                Type = msg.Join.PlayerType,
                Name = msg.Join.PlayerName,
                Role = msg.Join.RequestedRole,
                Score = 0,
                IpAddress = sourceIp.ToString(),
                Port = sourcePort
            };
            try
            {
                _game.AddPlayer(player);
                SendAckMsg(sourceIp, sourcePort, msg.MsgSeq);
                if (_deputyGamePlayer == null && player.Role == NodeRole.Normal)
                {
                    player.Role = NodeRole.Deputy;
                    _deputyGamePlayer = player;
                    SendRoleChangeMsg(player, NodeRole.Master, NodeRole.Deputy);
                }
            }
            catch
            {
                var error = "Field hasn't empty places";
                SendErrorMsg(sourceIp, sourcePort, error, msg.MsgSeq);
            }
        }
        else
        {
            string error;
            if (_currentPlayer.Role != NodeRole.Master)
            {
                error = "Appeal not to the master";
            }
            else if (msg.Join.GameName != _game.GetName())
            {
                error = "Game is not exist";
            }
            else
            {
                error = "Invalid role";
            }

            SendErrorMsg(sourceIp, sourcePort, error, msg.MsgSeq);
        }
    }

    private void SendJoinMsg()
    {
        var joinMsg = new GameMessage.Types.JoinMsg
        {
            PlayerType = _currentPlayer.Type,
            PlayerName = _currentPlayer.Name,
            GameName = _game.GetName(),
            RequestedRole = _currentPlayer.Role
        };
        var msg = new GameMessage
        {
            Join = joinMsg,
            MsgSeq = _numberMessage++
        };
        var sourceIp = IPAddress.Parse(_masterGamePlayer.IpAddress);
        var sourcePort = _masterGamePlayer.Port;
        var remoteEndPoint = new IPEndPoint(sourceIp, sourcePort);
        var bytes = msg.ToByteArray();
        try
        {
            _mainClient.Send(bytes, bytes.Length, remoteEndPoint);
        }
        catch (Exception ex)
        {
            if (_isRunning)
            {
                OnError?.Invoke(ex.Message);
            }

            return;
        }

        _sentMessages[msg.MsgSeq] = (msg, DateTime.Now, sourceIp, sourcePort);


        Console.WriteLine($"Send Join: {msg.MsgSeq}");
    }

    private void ReceiveSteerMsg(IPAddress sourceIp, int sourcePort, GameMessage msg)
    {
        if (_currentPlayer.Role == NodeRole.Master)
        {
            if (!_lastSteerMsg.TryGetValue((sourceIp, sourcePort), out var seq) || seq < msg.MsgSeq)
            {
                foreach (var player in _game.GetState().Players.Players)
                {
                    if (player.IpAddress == sourceIp.ToString() && player.Port == sourcePort)
                    {
                        _game.ChangeDirectionSnake(player.Id, msg.Steer.Direction);
                        break;
                    }
                }

                _lastSteerMsg[(sourceIp, sourcePort)] = msg.MsgSeq;
            }

            SendAckMsg(sourceIp, sourcePort, msg.MsgSeq);
        }
        else
        {
            var error = "Appeal not to the master";
            SendErrorMsg(sourceIp, sourcePort, error, msg.MsgSeq);
        }
    }

    private void SendSteerMsg(Direction direction)
    {
        var steerMsg = new GameMessage.Types.SteerMsg
        {
            Direction = direction
        };
        var msg = new GameMessage
        {
            Steer = steerMsg,
            MsgSeq = _numberMessage++
        };
        var sourceIp = IPAddress.Parse(_masterGamePlayer.IpAddress);
        var sourcePort = _masterGamePlayer.Port;
        var remoteEndPoint = new IPEndPoint(sourceIp, sourcePort);
        var bytes = msg.ToByteArray();
        try
        {
            _mainClient.Send(bytes, bytes.Length, remoteEndPoint);
        }
        catch (Exception ex)
        {
            if (_isRunning)
            {
                OnError?.Invoke(ex.Message);
            }

            return;
        }

        _sentMessages[msg.MsgSeq] = (msg, DateTime.Now, sourceIp, sourcePort);


        Console.WriteLine($"Send Steer: {msg.MsgSeq}");
    }

    private void ReceiveStateMsg(IPAddress sourceIp, int sourcePort, GameMessage msg)
    {
        if (_masterGamePlayer.IpAddress == sourceIp.ToString() && _masterGamePlayer.Port == sourcePort)
        {
            if (_game.GetState().StateOrder < msg.State.State.StateOrder)
            {
                _game.UpdateGameState(msg.State.State);
            }

            if (_currentPlayer.Role != NodeRole.Deputy)
            {
                _deputyGamePlayer = null;
            }

            foreach (var player in msg.State.State.Players.Players)
            {
                if (player.Role == NodeRole.Deputy)
                {
                    _deputyGamePlayer = player;
                }
                else if (player.Id == _masterGamePlayer.Id)
                {
                    player.IpAddress = sourceIp.ToString();
                    player.Port = sourcePort;
                }
            }

            SendAckMsg(sourceIp, sourcePort, msg.MsgSeq);
        }

        UpdateUserInterface();
    }

    private void SendStateMsg(IPAddress sourceIp, int sourcePort)
    {
        var stateMsg = new GameMessage.Types.StateMsg
        {
            State = _game.GetState()
        };
        var msg = new GameMessage
        {
            State = stateMsg,
            MsgSeq = _numberMessage++
        };
        var remoteEndPoint = new IPEndPoint(sourceIp, sourcePort);
        var bytes = msg.ToByteArray();
        try
        {
            _mainClient.Send(bytes, bytes.Length, remoteEndPoint);
        }
        catch (Exception ex)
        {
            if (_isRunning)
            {
                OnError?.Invoke(ex.Message);
            }

            return;
        }

        _sentMessages[msg.MsgSeq] = (msg, DateTime.Now, sourceIp, sourcePort);

        Console.WriteLine($"Send State: {msg.MsgSeq}");
    }

    private void SendRoleChangeMsg(GamePlayer player, NodeRole roleSender, NodeRole roleReceiver)
    {
        var sourceIp = IPAddress.Parse(player.IpAddress);
        var sourcePort = player.Port;

        var roleChangeMsg = new GameMessage.Types.RoleChangeMsg
        {
            SenderRole = roleSender,
            ReceiverRole = roleReceiver
        };
        var msg = new GameMessage
        {
            RoleChange = roleChangeMsg,
            MsgSeq = _numberMessage++,
            SenderId = _currentPlayer.Id,
            ReceiverId = player.Id
        };
        var remoteEndPoint = new IPEndPoint(sourceIp, sourcePort);
        var bytes = msg.ToByteArray();
        try
        {
            _mainClient.Send(bytes, bytes.Length, remoteEndPoint);
        }
        catch (Exception ex)
        {
            if (_isRunning)
            {
                OnError?.Invoke(ex.Message);
            }

            return;
        }

        _sentMessages[msg.MsgSeq] = (msg, DateTime.Now, sourceIp, sourcePort);

        Console.WriteLine($"Send RoleChange: {msg.MsgSeq}");
    }

    private void ChangeRoleToMaster()
    {
        var flagAddDeputy = false;
        foreach (var player in _game.GetState().Players.Players)
        {
            if (player.Id == _masterGamePlayer.Id)
            {
                player.Role = NodeRole.Viewer;
                continue;
            }

            if (player.Id == _currentPlayer.Id)
            {
                player.Role = NodeRole.Master;
                continue;
            }

            if (player.Role == NodeRole.Normal && !flagAddDeputy)
            {
                player.Role = NodeRole.Deputy;
                _deputyGamePlayer = player;
                flagAddDeputy = true;
                SendRoleChangeMsg(player, NodeRole.Master, NodeRole.Deputy);
                continue;
            }

            SendRoleChangeMsg(player, NodeRole.Master, player.Role);
        }
    }

    private void ReceiveRoleChangeMsg(IPAddress sourceIp, int sourcePort, GameMessage msg)
    {
        if (_masterGamePlayer.IpAddress == sourceIp.ToString() && _masterGamePlayer.Port == sourcePort)
        {
            if (msg.RoleChange.ReceiverRole == NodeRole.Master && _currentPlayer.Role == NodeRole.Deputy)
            {
                _currentPlayer.Role = NodeRole.Master;
                _game.UpdateLastId();
                _masterGamePlayer.Role = msg.RoleChange.SenderRole;
                foreach (var player in _game.GetState().Players.Players)
                {
                    if (player.Id == _masterGamePlayer.Id)
                    {
                        player.IpAddress = sourceIp.ToString();
                        player.Port = sourcePort;
                        player.Role = msg.RoleChange.SenderRole;
                        break;
                    }
                }

                _deputyGamePlayer = null;
                ChangeRoleToMaster();
                _masterGamePlayer = _currentPlayer;
            }
            else if (msg.RoleChange.ReceiverRole == NodeRole.Deputy && _currentPlayer.Role == NodeRole.Normal)
            {
                _currentPlayer.Role = NodeRole.Deputy;
                _deputyGamePlayer = _currentPlayer;
            }
            else if (msg.RoleChange.ReceiverRole == NodeRole.Viewer && _currentPlayer.Role != NodeRole.Viewer)
            {
                _currentPlayer.Role = NodeRole.Viewer;
                GameOver();
            }
            else if (msg.RoleChange.ReceiverRole == NodeRole.Master && msg.RoleChange.SenderRole == NodeRole.Viewer &&
                     _currentPlayer.Role == NodeRole.Normal && _deputyGamePlayer == null)
            {
                _currentPlayer.Role = NodeRole.Master;
                _game.UpdateLastId();
                ChangeRoleToMaster();
                _masterGamePlayer.Role = msg.RoleChange.SenderRole;
                _masterGamePlayer = _currentPlayer;
            }
        }
        else if (_deputyGamePlayer?.IpAddress == sourceIp.ToString() && _deputyGamePlayer?.Port == sourcePort)
        {
            if (msg.RoleChange.SenderRole == NodeRole.Master)
            {
                _masterGamePlayer = _deputyGamePlayer;
                if (msg.RoleChange.ReceiverRole == NodeRole.Deputy)
                {
                    _currentPlayer.Role = NodeRole.Deputy;
                    _deputyGamePlayer = _currentPlayer;
                }

                else
                {
                    _deputyGamePlayer = null;
                }
            }
        }
        else if (_deputyGamePlayer == null && msg.RoleChange.SenderRole == NodeRole.Master)
        {
            foreach (var player in _game.GetState().Players.Players)
            {
                if (player.Id == msg.SenderId)
                {
                    _masterGamePlayer = player;
                    break;
                }
            }

            if (msg.RoleChange.ReceiverRole == NodeRole.Deputy)
            {
                _currentPlayer.Role = NodeRole.Deputy;
                _deputyGamePlayer = _currentPlayer;
            }
        }
        else
        {
            if (_currentPlayer.Role == NodeRole.Master && msg.RoleChange.SenderRole == NodeRole.Viewer)
            {
                foreach (var player in _game.GetState().Players.Players)
                {
                    if (player.IpAddress == sourceIp.ToString() && player.Port == sourcePort)
                    {
                        player.Role = NodeRole.Viewer;
                        foreach (var snake in _game.GetState().Snakes)
                        {
                            if (snake.PlayerId == player.Id)
                            {
                                snake.State = GameState.Types.Snake.Types.SnakeState.Zombie;
                                break;
                            }
                        }

                        break;
                    }
                }
            }
        }

        SendAckMsg(sourceIp, sourcePort, msg.MsgSeq);
    }

    private void MessageProcessing(GameMessage msg, IPEndPoint remoteEp)
    {
        var sourceIp = remoteEp.Address;
        var sourcePort = remoteEp.Port;

        if (_masterGamePlayer.IpAddress == sourceIp.ToString() && _masterGamePlayer.Port == sourcePort)
        {
            _lastMsgs[_masterGamePlayer.Id] = DateTime.Now;
        }
        else
        {
            foreach (var player in _game.GetState().Players.Players)
            {
                if (player.IpAddress == sourceIp.ToString() && player.Port == sourcePort)
                {
                    _lastMsgs[player.Id] = DateTime.Now;
                    break;
                }
            }
        }

        Console.WriteLine($"Receive {msg.TypeCase.ToString()} message: {msg.MsgSeq}");

        switch (msg.TypeCase)
        {
            case GameMessage.TypeOneofCase.Ack:
                ReceiveAckMsg(msg);
                break;
            case GameMessage.TypeOneofCase.Announcement:
                ReceiveAnnouncementMsg(sourceIp, sourcePort, msg);
                break;
            case GameMessage.TypeOneofCase.Error:
                ReceiveErrorMsg(sourceIp, sourcePort, msg);
                break;
            case GameMessage.TypeOneofCase.Join:
                ReceiveJoinMsg(sourceIp, sourcePort, msg);
                break;
            case GameMessage.TypeOneofCase.Ping:
                SendAckMsg(sourceIp, sourcePort, msg.MsgSeq);
                break;
            case GameMessage.TypeOneofCase.RoleChange:
                ReceiveRoleChangeMsg(sourceIp, sourcePort, msg);
                break;
            case GameMessage.TypeOneofCase.State:
                ReceiveStateMsg(sourceIp, sourcePort, msg);
                break;
            case GameMessage.TypeOneofCase.Steer:
                ReceiveSteerMsg(sourceIp, sourcePort, msg);
                break;
        }
    }

    private void ListenMain()
    {
        while (_isRunning)
        {
            var remoteEp = new IPEndPoint(IPAddress.Any, 0);
            byte[] res;
            try
            {
                res = _mainClient.Receive(ref remoteEp);
            }
            catch (Exception ex)
            {
                if (_isRunning)
                {
                    OnError?.Invoke(ex.Message);
                    continue;
                }

                return;
            }

            var msg = GameMessage.Parser.ParseFrom(res);
            if (msg.TypeCase != GameMessage.TypeOneofCase.Announcement && !_gameInProcess)
            {
                continue;
            }

            Task.Run(() => MessageProcessing(msg, remoteEp));
        }
    }
}