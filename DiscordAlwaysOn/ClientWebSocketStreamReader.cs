using System.Net.WebSockets;

namespace DiscordAlwaysOn;

public class ClientWebSocketStreamReader(ReadOnlyMemory<byte> probeBuffer, ClientWebSocket client) : Stream
{
    private int _probeBufferOffset;
    private ValueWebSocketReceiveResult _result;
    public ValueWebSocketReceiveResult Result => _result;

    public override int Read(byte[] buffer, int offset, int count)
        => throw new NotSupportedException();

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        if (_result.EndOfMessage)
            return 0;

        var written = 0;

        if (probeBuffer.Length > _probeBufferOffset)
        {
            var len = Math.Min(probeBuffer.Length - _probeBufferOffset, count);
            probeBuffer
                .Slice(_probeBufferOffset, len)
                .CopyTo(buffer.AsMemory(offset, len));
            _probeBufferOffset += len;
            written += len;
        }

        while (count > written && !_result.EndOfMessage)
        {
            _result = await client.ReceiveAsync(buffer
                .AsMemory(offset + written, count - written), cancellationToken);

            written += _result.Count;
        }

        return written;
    }

    public override void Flush()
        => throw new NotSupportedException();

    public override long Seek(long offset, SeekOrigin origin)
        => throw new NotSupportedException();

    public override void SetLength(long value)
        => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count)
        => throw new NotSupportedException();

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }
}