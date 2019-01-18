using Microsoft.CognitiveServices.Speech;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace realtime_transcriber
{
    public class AudioProcessor
    {
        private static SpeechRecognizer _speechClient;
        private static VoiceAudioStream _voiceAudioStream;
        private static string _speechKey = ConfigurationManager.AppSettings["SpeechKey"];

        private static string _speechRegion = ConfigurationManager.AppSettings["SpeechRegion"];

        public AudioProcessor()
        {

            if (_speechClient == null)
            {
                CreateSpeechClient();

            }
        }

        private void CreateSpeechClient()
        {
            var format = new AudioInputStreamFormat()
            {
                BitsPerSample = 16,
                BlockAlign = 2,
                AvgBytesPerSec = 32000,
                Channels = 1,
                FormatTag = 1,
                SamplesPerSec = 16000
            };

            _voiceAudioStream = new VoiceAudioStream(format); // custom AudioInputStream

            var factory = SpeechFactory.FromSubscription(_speechKey, _speechRegion);
            _speechClient = factory.CreateSpeechRecognizerWithStream(_voiceAudioStream, "en-gb");
            _speechClient.RecognitionErrorRaised += _speechClient_RecognitionErrorRaised;
            _speechClient.IntermediateResultReceived += _speechClient_IntermediateResultReceived;
            _speechClient.FinalResultReceived += _speechClient_FinalResultReceived;
        }

        private async void _speechClient_IntermediateResultReceived(object sender, SpeechRecognitionResultEventArgs e)
        {
            if (e.Result.Text.Length > 10)
            {
                Console.WriteLine(e.Result.Text);
            }
        }

        private async void _speechClient_RecognitionErrorRaised(object sender, RecognitionErrorEventArgs e)
        {

            Console.WriteLine(e.FailureReason); // do anything with the result here

            //Debug.WriteLine("Intermediate result: " + e.Result.Text);
        }

        

        private async void _speechClient_FinalResultReceived(object sender, SpeechRecognitionResultEventArgs e)
        {
            string result = "";
            if (e.Result.RecognitionStatus == RecognitionStatus.Recognized)
            {
                //Console.WriteLine(e.Result.Text); // do anything with the result here
                result = e.Result.Text;
                //_correlationId = Guid.Empty;
            }
            else if (e.Result.RecognitionStatus == RecognitionStatus.InitialSilenceTimeout)
            {
                result = "[Silence]";

            }
            Console.WriteLine(result);
            using (StreamWriter w = File.AppendText("output.txt"))
            {
                w.WriteLine(result);
            }
                //Debug.WriteLine("Final result: " + e.Result.Text);
            }

        public void AudioStart()
        {
            CreateSpeechClient();
            // client is first expected to send the "AudioStart" message
            _speechClient.StartContinuousRecognitionAsync();
        }

        public void ReceiveAudio(byte[] audioChunk)
        {
            // client can then stream byte arrays as "ReceiveAudio" messages
            _voiceAudioStream.Write(audioChunk, 0, audioChunk.Length);
        }

        public void StopAudio()
        {
            // client can then stream byte arrays as "ReceiveAudio" messages
            if (_speechClient != null)
            {
                _speechClient.StopContinuousRecognitionAsync();
                _speechClient.RecognitionErrorRaised -= _speechClient_RecognitionErrorRaised;
                _speechClient.IntermediateResultReceived -= _speechClient_IntermediateResultReceived;
                _speechClient.FinalResultReceived -= _speechClient_FinalResultReceived;
            }
        }
    }
}
