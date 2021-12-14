using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SpeechRecognitionClient.Abstractions;
using SpeechRecognitionClient.Models;

namespace SpeechRecognitionClient.Executors
{
    public class CoquiExecutor : IExecutor
    {
        public void Execute(string url, string inputWav, string outputTxt)
        {
            ClientWebSocket ws = new ClientWebSocket();
            ws.ConnectAsync(new Uri(url), CancellationToken.None).Wait();
            var fileBytes = File.ReadAllBytes(inputWav);
            ws.SendAsync(fileBytes, WebSocketMessageType.Binary, true, CancellationToken.None);

            this.ProccessResult(ws, outputTxt).Wait();
        }

        private async Task ProccessResult(ClientWebSocket ws, string outputTxt)
        {
            ArraySegment<Byte> buffer = new ArraySegment<byte>(new Byte[8192]);

            WebSocketReceiveResult result = null;

            using (var ms = new MemoryStream())
            {
                do
                {
                    result = await ws.ReceiveAsync(buffer, CancellationToken.None);
                    ms.Write(buffer.Array, buffer.Offset, result.Count);
                }
                while (!result.EndOfMessage);

                ms.Seek(0, SeekOrigin.Begin);

                using (var reader = new StreamReader(ms, Encoding.UTF8))
                {
                    var resultStr = reader.ReadToEnd();
                    Console.WriteLine("");
                    Console.WriteLine($"Result {resultStr}");
                    if (string.IsNullOrEmpty(resultStr) == false && resultStr.Contains("\"text\":") == true)
                    {
                        Console.WriteLine("");
                        Console.WriteLine("Appending Text to File...");
                        var resultObj = JsonConvert.DeserializeObject<CoquiResponse>(resultStr);
                        File.AppendAllText(outputTxt, resultObj.text + ". " + Environment.NewLine);
                    }
                }
            }
        }
    }
}
