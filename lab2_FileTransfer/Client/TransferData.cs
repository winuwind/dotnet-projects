using Protocol.Constants;

namespace Client;

using System.Text;
using Protocol.Protocol;
using Protocol.Protocol.Segments;

public class TransferData : ITransfer
{
    private readonly Client _client;
    private readonly ServerHandler _handler;
    private readonly string _filePath;
    private readonly string _fileName;
    private readonly long _sizeFile;
    
    private static readonly ManualResetEvent Evt = new(false);

    private static uint _segmentId;

    private long _numberSegment;
    private bool _isCanceled;
    private bool _isStarted;
    private bool _isCompleted;
    private Thread? _thread;

    public TransferData(Client client, ServerHandler handler, string filePath)
    {
        _client = client;
        _handler = handler;
        _filePath = filePath;
        
        var fileInfo = new FileInfo(_filePath);
        _sizeFile = fileInfo.Length;
        _fileName = fileInfo.Name;
        
        _isCanceled = false;
        _isStarted = false;
        _isCompleted = false;
    }
    
    public int AskSegments(){return 0;}

    public void SendFile()
    {
        _thread = new Thread(_ =>
        {
            while (!_isCanceled && !_isStarted && !_isCompleted)
            {
                SendFileInfo();
                Thread.Sleep(3000);
            }
        });
        _thread.Start();
        

        while (!_isStarted)
        {
            Evt.WaitOne();
        }

        using var fs = File.OpenRead(_filePath);
        var buffer = new byte[AppConstants.MaxSizeData];
        int bytesRead;
        while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0 && !_isCanceled)
        {
            var part = new byte[bytesRead];
            Array.Copy(buffer, part, bytesRead);
                
            var segmentData = new SegmentData
            {
                Size = bytesRead,
                NumberOfSegment = _numberSegment++,
                Data = part
            };

            var segment = new Segment
            {
                SegmentSize = segmentData.Size + AppConstants.HeaderLenght,
                SegmentId = _segmentId,
                TypeSegment = TypeSegment.Data,
                SegmentData = segmentData
            };

            _segmentId += 2;
            SendSegment(segment);
        }
    }

    public void Cancel()
    {
        _isCanceled = true;

        var segmentData = new SegmentData
        {
            Size = 0,
            NumberOfSegment = 0,
            Data = null
        };

        var segment = new Segment
        {
            SegmentSize = AppConstants.HeaderLenght,
            SegmentId = _segmentId,
            TypeSegment = TypeSegment.Cancel,
            SegmentData = segmentData
        };

        _segmentId += 2;
        SendSegment(segment);
    }

    public Segment ExitSegment()
    {
        var segmentData = new SegmentData
        {
            Size = 0,
            NumberOfSegment = 0
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
        var segment = ITransfer.Deserialize(bytes, size);
        segment.SegmentSize = size;

        if (segment.TypeSegment == TypeSegment.Exit)
        {
            _isStarted = true;
            _isCanceled = true;
            Evt.Set();
            _client.Exit();
        }
        else if (_isCanceled)
        {
            Cancel();
        }
        else switch (segment.TypeSegment)
        {
            case TypeSegment.Fail:
            {
                var newSegment = GenerateSegment(segment.SegmentData.NumberOfSegment);
                SendSegment(newSegment);
                break;
            }
            case TypeSegment.Ok when _isStarted:
                Console.WriteLine($"File {_filePath} transferred to server");
                _thread?.Join();
                _isCompleted = true;
                break;
            case TypeSegment.Ok:
                _isStarted = true;
                Evt.Set();
                break;
        }
    }

    private void SendSegment(Segment segment)
    {
        _handler.Send(ITransfer.Serialize(segment));
    }

    private void SendFileInfo()
    {
        var segmentData = new SegmentData
        {
            Size = _sizeFile,
            NumberOfSegment = _numberSegment,
            Data = Encoding.UTF8.GetBytes(_fileName)
        };

        var segment = new Segment
        {
            SegmentSize = segmentData.Data.Length + AppConstants.HeaderLenght,
            SegmentId = _segmentId,
            TypeSegment = TypeSegment.Name,
            SegmentData = segmentData
        };


        _segmentId += 2;
        SendSegment(segment);
    }
    
    private Segment GenerateSegment(long numberOfSegment)
    {
        using var fs = new FileStream(_filePath, FileMode.Open, FileAccess.Read);
        fs.Seek(numberOfSegment * AppConstants.MaxSizeData, SeekOrigin.Begin);

        var buffer = new byte[AppConstants.MaxSizeData];
        var bytesRead = fs.Read(buffer, 0, buffer.Length);

        var part = new byte[bytesRead];
        Array.Copy(buffer, part, bytesRead);
            
        var segmentData = new SegmentData
        {
            Size = bytesRead,
            NumberOfSegment = numberOfSegment,
            Data = part
        };

        var segment = new Segment
        {
            SegmentSize = segmentData.Size + AppConstants.HeaderLenght,
            SegmentId = _segmentId,
            TypeSegment = TypeSegment.Data,
            SegmentData = segmentData
        };

        _segmentId += 2;
        return segment;
    }
}