using NReco.VideoConverter;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace realtime_transcriber
{
    public class Program
    {
        private static MemoryStream outputStream;

        private static AudioProcessor audioProcesser;

        static void Main(string[] args)
        {
            Console.WriteLine("Welcome to the Realtime Audio Transcriber");
            audioProcesser = new AudioProcessor();
            var ffMpeg = new NReco.VideoConverter.FFMpegConverter();
            ffMpeg.LogLevel = "verbose";
            ffMpeg.LogReceived += logs;
            outputStream = new MemoryStream();
            Console.WriteLine("Created FFMPeg");
            string streamUrl = "testing_file.mp3";
            audioProcesser.AudioStart();
            var vidtask = ffMpeg.ConvertLiveMedia(streamUrl, null, outputStream, "wav", new ConvertSettings
            {

                CustomOutputArgs = "-vn -r 25 -c:a pcm_s16le -b:a 16k -ac 1 -ar 16000"
            });
            vidtask.OutputDataReceived += DataReceived;
            Console.WriteLine("Starting");
            vidtask.Start();
            Console.WriteLine("Started");
            vidtask.Wait();
            byte[] buffer = new byte[2048]; // using 2kB byte arrays
            int bytesRead;

            Console.Read();
            //connection.Stop();
            Console.WriteLine("Complete");

        }

        private static void logs(object sender, FFMpegLogEventArgs e)
        {
            //Console.WriteLine(e.Data);
        }

        static int pos = 0;
        private static void DataReceived(object sender, EventArgs e)
        {

            byte[] buffer = new byte[2048]; // using 2kB byte arrays
            outputStream.Position = pos;

            int bytesRead;
            var v = outputStream.ReadAsync(buffer, 0, buffer.Length);
            // Console.WriteLine(v);
            while (outputStream.Read(buffer, 0, buffer.Length) > 0)
            {
                pos += buffer.Length;
                //Console.WriteLine("Sending chunk");
                // connection is declared somewhere else like this:
                // HubConnection connection = new HubConnectionBuilder().WithUrl...
                audioProcesser.ReceiveAudio(buffer);
            }


        }
    }
}
