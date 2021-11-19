using SpeechRecognitionClient.Abstractions;
using SpeechRecognitionClient.Clients;

namespace SpeechRecognitionClient.Executors
{
    public class VoskExecutor : IExecutor
    {
        public void Execute(string url, string inputWav, string outputTxt)
        {
            VoskClient client = new VoskClient(inputWav, url, outputTxt);
            client.DecodeFile().Wait();
        }
    }
}
