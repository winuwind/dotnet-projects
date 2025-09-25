namespace Server;

using System.Text;
using ProtocolLibrary.Protocol;
using ProtocolLibrary.Protocol.Segments;

public class TransferData(ClientHandler handler, Server server) : ITransfer
{
    private const double Kilo = 1024.0;
    private const double Mega = 1024.0 * 1024.0;
    private const double Giga = 1024.0 * 1024.0 * 1024.0;
    private const double Tera = 1024.0 * 1024.0 * 1024.0 * 1024.0;
    private const double KiloR = 1.0 / Kilo;
    private const double MegaR = 1.0 / Mega;
    private const double GigaR = 1.0 / Giga;
    private const int MaxSizeData = 4096;

    private readonly Server? _server = server;

    private static long _segmentId = 1;

    private DateTime _startTime;
    private DateTime _lastTime;
    private Thread? _thread;
    private string? _fileName;
    private bool _isCanceled;
    private bool _isStarted;
    private bool _isFinal;
    private bool[]? _receivedParts;
    private long _numberReceivedSegments;
    private long _countSegmentsInLastTime;
    private int _cursorLeft;
    private int _cursorTop;
    
    private void SendSegment(Segment segment)
    {
        handler.Send(ITransfer.Serialize(segment));
    }

    private static Segment GenerateSegment(long numberOfSegment)
    {
        var segmentData = new SegmentData
        {
            Size = 0,
            NumberOfSegment = numberOfSegment,
            Data = null
        };

        var segment = new Segment
        {
            SegmentSize = segmentData.Size + 40,
            SegmentId = _segmentId,
            TypeSegment = TypeSegment.Fail,
            SegmentData = segmentData
        };

        _segmentId += 2;
        return segment;
    }

    public int AskSegments()
    {
        if (_isCanceled || _receivedParts == null)
        {
            return -1;
        }
        if(_isFinal)
        {
            return 1;
        }

        for (long i = 0; i < _receivedParts.Length; i++)
        {
            if (_receivedParts[i]) continue;
            var segment = GenerateSegment(i);
            SendSegment(segment);
            return 0;
        }
        var okSegment = ExitSegment();
        okSegment.TypeSegment =  TypeSegment.Ok;
        SendSegment(okSegment);
        
        _isFinal = true;
        
        _thread?.Join();
        _thread = null;
        
        return 1;
    }

    public Segment ExitSegment()
    {
        var segmentData = new SegmentData
        {
            Size = 0,
            NumberOfSegment = 0,
            Data = null
        };

        var segment = new Segment
        {
            SegmentSize = segmentData.Size + 40,
            SegmentId = _segmentId,
            TypeSegment = TypeSegment.Exit,
            SegmentData = segmentData
        };

        _segmentId += 2;
        return segment;
    }

    private void PrintSpeed(DateTime startTime, DateTime lastTime, DateTime now, long countLast, long countAll)
    {
        var speed = countLast * MaxSizeData / now.Subtract(lastTime).TotalSeconds;
        
        var sb = new StringBuilder();
        sb.AppendFormat($"File: {_fileName}: Loading speed: ");
        
        if (speed < Kilo)
        {
            sb.AppendFormat($"{speed:0.00} B/s\n");
        }
        else if (speed < Mega)
        {
            sb.AppendFormat($"{(speed * KiloR):0.00} KB/s\n");
        }
        else if (speed < Giga)
        {
            sb.AppendFormat($"{(speed * MegaR):0.00} MB/s\n");
        }
        else if (speed < Tera)
        {
            sb.AppendFormat($"{(speed * GigaR):0.00} GB/s\n");
        }
        var avgSpeed = countAll * MaxSizeData / now.Subtract(startTime).TotalSeconds;
        sb.AppendFormat("Average loading speed: ");
        if (avgSpeed < Kilo)
        {
            sb.AppendFormat($"{avgSpeed:0.00} B/s\n");
        }
        else if (avgSpeed < Mega)
        {
            sb.AppendFormat($"{(avgSpeed * KiloR):0.00} KB/s\n");
        }
        else if (avgSpeed < Giga)
        {
            sb.AppendFormat($"{(avgSpeed * MegaR):0.00} MB/s\n");
        }
        else if (avgSpeed < Tera)
        {
            sb.AppendFormat($"{(avgSpeed * GigaR):0.00} GB/s\n");
        }
        Server.Print(sb.ToString(), _cursorLeft, _cursorTop);
    }

    public void ReceiveSegment(byte[] bytes, long size, int id)
    {
        if (_isCanceled || _isFinal)
        {
            return;
        }

        var segment = ITransfer.Deserialize(bytes, size);
        
        if (!_isStarted && segment.TypeSegment != TypeSegment.Name)
        {
            return;
        }

        if (segment.TypeSegment == TypeSegment.Cancel)
        {
            Server.Print("Transfer of file cancelled", -1, -1);
            
            _isCanceled = true;
            
            _thread?.Join();
            _thread = null;
            
            _server?.Cancel(handler.GetId());
        }
        else if (segment.TypeSegment == TypeSegment.Exit)
        {
            _isCanceled = true;
            
            _thread?.Join();
            _thread = null;
            
            _server?.Cancel(handler.GetId());
            _server?.CloseHandler(handler.GetId());
        }
        else if (segment.TypeSegment == TypeSegment.Data)
        {
            if (_receivedParts != null && _receivedParts[segment.SegmentData.NumberOfSegment])
            {
                return;
            }

            if (_receivedParts == null) return;
            _receivedParts[segment.SegmentData.NumberOfSegment] = true;
            if (segment.SegmentData.Data != null)
                _server?.SetPartFile(segment.SegmentData.Data, segment.SegmentData.Size,
                    segment.SegmentData.NumberOfSegment,
                    MaxSizeData, id);

            _countSegmentsInLastTime++;
            _numberReceivedSegments++;

            if (_receivedParts.Length != _numberReceivedSegments) return;
            _isFinal = true;

            var okSegment = ExitSegment();
            okSegment.TypeSegment = TypeSegment.Ok;
            SendSegment(okSegment);

            _thread?.Join();
            _thread = null;

            _server?.Final(handler.GetId());
        }
        else if (segment.TypeSegment == TypeSegment.Name)
        {
            if (_isStarted)
            {
                return;
            }
            
            Server.Print("Transfer of file started", -1, -1);
            (_cursorLeft, _cursorTop) = Console.GetCursorPosition();
            Server.Print("", -1, -1);
            Server.Print("", -1, -1);
            Server.Print("", -1, -1);
            
            _receivedParts = new bool[(segment.SegmentData.Size + MaxSizeData - 1) / MaxSizeData];
            for (var i = 0; i < _receivedParts.Length; i++)
            {
                _receivedParts[i] = false;
            }

            if (segment.SegmentData.Data != null) _fileName = Encoding.UTF8.GetString(segment.SegmentData.Data);
            if (_fileName != null) _server?.SetFileInfo(segment.SegmentData.Size, _fileName, id);

            var okSegment = ExitSegment();
            okSegment.TypeSegment =  TypeSegment.Ok;
            SendSegment(okSegment);
            
            _isStarted = true;
            
            _startTime = DateTime.Now;
            _lastTime = DateTime.Now;

            _thread = new Thread(_ =>
            {
                while (!(_isCanceled || _isFinal) && _isStarted)
                {
                    Thread.Sleep(3000);
                    PrintSpeed(_startTime, _lastTime, DateTime.Now, _countSegmentsInLastTime, _numberReceivedSegments);
                    _countSegmentsInLastTime = 0;
                    _lastTime = DateTime.Now;
                }
            });
            _thread.Start();
        }
    }
}
