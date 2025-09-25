namespace ProtocolLibrary.Protocol.Segments;

public struct Segment
{
    public long SegmentSize;
    public long SegmentId;
    public TypeSegment TypeSegment;
    public SegmentData SegmentData;
}