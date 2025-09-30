namespace Protocol.Protocol.Segments;

public struct SegmentData
{
    public long Size;
    public long NumberOfSegment; 
    public byte[]? Data;
}