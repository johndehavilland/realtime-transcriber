using Microsoft.CognitiveServices.Speech;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace realtime_transcriber
{

    public class VoiceAudioStream : AudioInputStream
    {
        AudioInputStreamFormat _format;
        EchoStream _dataStream;

        public VoiceAudioStream(AudioInputStreamFormat format)
        {
            // Making the job slightly easier by requiring audio format in the constructor.
            // Cognitive Speech services expect:
            //  - PCM WAV
            //  - 16k samples/s
            //  - 32k bytes/s
            //  - 2 block align
            //  - 16 bits per sample
            //  - mono
            _format = format;
            _dataStream = new EchoStream();
        }

        public override AudioInputStreamFormat GetFormat()
        {
            return _format;
        }

        public override int Read(byte[] dataBuffer)
        {
            return _dataStream.Read(dataBuffer, 0, dataBuffer.Length);
        }

        public void Write(byte[] buffer, int offset, int count)
        {
            _dataStream.Write(buffer, offset, count);
        }

        public override void Close()
        {
            _dataStream.Dispose();
            base.Close();
        }
    }

    public class EchoStream : MemoryStream
    {
        private readonly ManualResetEvent _DataReady = new ManualResetEvent(false);
        private readonly ConcurrentQueue<byte[]> _Buffers = new ConcurrentQueue<byte[]>();

        public bool DataAvailable { get { return !_Buffers.IsEmpty; } }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _Buffers.Enqueue(buffer.Take(count).ToArray()); // add new data to buffer
            _DataReady.Set(); // allow waiting reader to proceed
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            _DataReady.WaitOne(); // block until there's something new to read

            byte[] lBuffer;

            if (!_Buffers.TryDequeue(out lBuffer)) // try to read
            {
                _DataReady.Reset();
                return -1;
            }

            if (!DataAvailable)
                _DataReady.Reset();

            Array.Copy(lBuffer, buffer, lBuffer.Length);
            return lBuffer.Length;
        }
    }
}
