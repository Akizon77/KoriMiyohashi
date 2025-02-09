using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace MamoLib
{
    /// <summary>
    /// A stream that reports progress as bytes are read.
    /// <code language="csharp">
    /// using (var fs_stream = new FileStream("your_file_path", FileMode.Open, FileAccess.Read))
    ///{
    ///    var progressStream = new ProgressStream(fs_stream, progressThreshold: 1.0); // Threshold of 1%
    ///    progressStream.ProgressChanged += (progress) => Console.WriteLine($"Upload progress: {progress:F2}%");
    ///
    ///}
    ///</code>
    /// </summary>
    public class ProgressStream : Stream 
    {
        private readonly Stream _stream;
        private long _totalBytesRead = 0;
        private readonly long _length;
        private double _lastReportedProgress = 0.0;
        private readonly double? _progressThreshold;
        private readonly TimeSpan? _progressInterval;
        private DateTime _lastReported = DateTime.UtcNow;
        public ProgressStream(Stream stream, double progressThreshold, long? length = null)
        {
            _stream = stream ?? throw new ArgumentNullException(nameof(stream));
            if (length <= 0 ) throw new ArgumentOutOfRangeException(nameof(length), "Length must be greater than 0.");
            if (stream is NetworkStream && length is null)
            {
                throw new ArgumentNullException(nameof(length), "Length cannot be null when stream is NetworkStream.");
            }
            _length = length ?? stream.Length;
            _progressThreshold = progressThreshold >= 0 ? progressThreshold : throw new ArgumentException("ProgressThreshold must be greater than 0.");
        }
        public ProgressStream(Stream stream, TimeSpan progressInterval, long? length = null)
        {
            _stream = stream ?? throw new ArgumentNullException(nameof(stream));
            if (length <= 0) throw new ArgumentOutOfRangeException(nameof(length), "Length must be greater than 0.");
            if (stream is NetworkStream && length is null)
            {
                throw new ArgumentNullException(nameof(length), "Length cannot be null when stream is NetworkStream.");
            }
            _length = length ?? stream.Length;
            _progressInterval = progressInterval > TimeSpan.Zero ? progressInterval : throw new ArgumentException("ProgressInterval must be greater than 0.");
        }


        public event Action<double>? ProgressChanged;

        public override int Read(byte[] buffer, int offset, int count)
        {
            int bytesRead = _stream.Read(buffer, offset, count);
            ReportProgress(bytesRead);
            return bytesRead;
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
        {
            int bytesRead = await _stream.ReadAsync(buffer, offset, count, cancellationToken);
            ReportProgress(bytesRead);
            return bytesRead;
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            int bytesRead = await _stream.ReadAsync(buffer, cancellationToken);
            ReportProgress(bytesRead);
            return bytesRead;
        }

        private void ReportProgress(int bytesRead)
        {
            if (bytesRead > 0)
            {
                _totalBytesRead += bytesRead;
                double progress = (double)_totalBytesRead / _length * 100;
                if (_progressInterval is null)
                {
                    if (progress - _lastReportedProgress >= _progressThreshold)
                    {
                        _lastReportedProgress = progress;
                        ProgressChanged?.Invoke(progress);
                    }
                }
                else
                {
                    if (DateTime.UtcNow - _lastReported > _progressInterval)
                    {
                        _lastReported = DateTime.UtcNow;
                        ProgressChanged?.Invoke(progress);
                    }
                }
                
            }
        }

        public override bool CanRead => _stream.CanRead;
        public override bool CanSeek => _stream.CanSeek;
        public override bool CanWrite => _stream.CanWrite;
        public override long Length => _stream.Length;
        public override long Position { get => _stream.Position; set => _stream.Position = value; }

        public override void Flush() => _stream.Flush();

        public override long Seek(long offset, SeekOrigin origin) => _stream.Seek(offset, origin);

        public override void SetLength(long value) => _stream.SetLength(value);

        public override void Write(byte[] buffer, int offset, int count) => _stream.Write(buffer, offset, count);
    }
}