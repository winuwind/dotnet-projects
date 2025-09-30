using Protocol.Constants;
using Protocol.Printer;

namespace Server;

using System.Text;
using Protocol.Protocol;
using Protocol.Protocol.Segments;

public class TransferData(ClientHandler handler, Server server) : ITransfer
{
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
            SegmentSize = segmentData.Size + AppConstants.HeaderLenght,
            SegmentId = _segmentId,
            TypeSegment = TypeSegment.Exit,
            SegmentData = segmentData
        };

        _segmentId += 2;
        return segment;
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
            Printer.Print("Transfer of file cancelled", -1, -1);
            
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
                    AppConstants.MaxSizeData, id);

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
            
            Printer.Print("Transfer of file started", -1, -1);
            (_cursorLeft, _cursorTop) = Console.GetCursorPosition();
            Printer.Print("", -1, -1);
            Printer.Print("", -1, -1);
            Printer.Print("", -1, -1);
            
            _receivedParts = new bool[(segment.SegmentData.Size + AppConstants.MaxSizeData - 1) / AppConstants.MaxSizeData];
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
            SegmentSize = segmentData.Size + AppConstants.HeaderLenght,
            SegmentId = _segmentId,
            TypeSegment = TypeSegment.Fail,
            SegmentData = segmentData
        };

        _segmentId += 2;
        return segment;
    }

    private void PrintSpeed(DateTime startTime, DateTime lastTime, DateTime now, long countLast, long countAll)
    {
        var speed = countLast * AppConstants.MaxSizeData / now.Subtract(lastTime).TotalSeconds;
        
        var sb = new StringBuilder();
        sb.AppendFormat($"File: {_fileName}: Loading speed: ");
        
        if (speed < AppConstants.Kilo)
        {
            sb.AppendFormat($"{speed:0.00} B/s\n");
        }
        else if (speed < AppConstants.Mega)
        {
            sb.AppendFormat($"{(speed * AppConstants.KiloR):0.00} KB/s\n");
        }
        else if (speed < AppConstants.Giga)
        {
            sb.AppendFormat($"{(speed * AppConstants.MegaR):0.00} MB/s\n");
        }
        else if (speed < AppConstants.Tera)
        {
            sb.AppendFormat($"{(speed * AppConstants.GigaR):0.00} GB/s\n");
        }
        var avgSpeed = countAll * AppConstants.MaxSizeData / now.Subtract(startTime).TotalSeconds;
        sb.AppendFormat("Average loading speed: ");
        if (avgSpeed < AppConstants.Kilo)
        {
            sb.AppendFormat($"{avgSpeed:0.00} B/s\n");
        }
        else if (avgSpeed < AppConstants.Mega)
        {
            sb.AppendFormat($"{(avgSpeed * AppConstants.KiloR):0.00} KB/s\n");
        }
        else if (avgSpeed < AppConstants.Giga)
        {
            sb.AppendFormat($"{(avgSpeed * AppConstants.MegaR):0.00} MB/s\n");
        }
        else if (avgSpeed < AppConstants.Tera)
        {
            sb.AppendFormat($"{(avgSpeed * AppConstants.GigaR):0.00} GB/s\n");
        }
        Printer.Print(sb.ToString(), _cursorLeft, _cursorTop);
    }
}