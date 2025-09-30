namespace Protocol.Protocol;

using Segments;

public interface ITransfer
{
    public void ReceiveSegment(byte[] bytes, long size, int id);
    public Segment ExitSegment();
    public int AskSegments();
    
    public static byte[] Serialize(Segment segment)
    {
        byte[]?[] allBytes = new byte[6][];
        allBytes[0] = BitConverter.GetBytes(segment.SegmentSize);
        allBytes[1] = BitConverter.GetBytes(segment.SegmentId);
        allBytes[2] = BitConverter.GetBytes((long) segment.TypeSegment);
        allBytes[3] = BitConverter.GetBytes(segment.SegmentData.Size);
        allBytes[4] = BitConverter.GetBytes(segment.SegmentData.NumberOfSegment);
        allBytes[5] = segment.SegmentData.Data;
        var size = segment.SegmentSize;
        var bytes = new byte[size];
        for (int i = 0, j = 0; i < allBytes.Length; i++)
        {
            if (allBytes[i] == null) continue;
            Buffer.BlockCopy(allBytes[i], 0, bytes, j, allBytes[i].Length);
            j += allBytes[i].Length;

        }

        return bytes;
    }
    
    public static Segment Deserialize(byte[] bytes, long size)
    {
        var segment = new Segment
        {
            SegmentSize = size
        };

        var offset = 0;

        segment.SegmentId = BitConverter.ToInt64(bytes, offset);
        offset += sizeof(long);
        segment.TypeSegment = (TypeSegment)BitConverter.ToInt64(bytes, offset);
        offset += sizeof(long);
        segment.SegmentData.Size = BitConverter.ToInt64(bytes, offset);
        offset += sizeof(long);
        segment.SegmentData.NumberOfSegment = BitConverter.ToInt64(bytes, offset);
        offset += sizeof(long);
        var dataLen = bytes.Length - offset;
        segment.SegmentData.Data = new byte[dataLen];
        Buffer.BlockCopy(bytes, offset, segment.SegmentData.Data, 0, dataLen);

        return segment;
    }
}