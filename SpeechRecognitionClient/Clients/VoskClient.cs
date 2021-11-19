using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SpeechRecognitionClient.Abstractions;

namespace SpeechRecognitionClient.Clients
{
    public class VoskClient : IClient
    {
        private string inputFullFilePath { get; set; }
        private string outputFullFilePath { get; set; }
        private string voskWebsocketUrl { get; set; }

        public VoskClient(string inputFullFilePath, string voskWebsocketUrl, string outputFullFilePath)
        {
            this.inputFullFilePath = inputFullFilePath;
            this.outputFullFilePath = outputFullFilePath;
            this.voskWebsocketUrl = voskWebsocketUrl;
        }

        public async Task DecodeFile()
        {
            ClientWebSocket ws = new ClientWebSocket();
            await ws.ConnectAsync(new Uri(this.voskWebsocketUrl), CancellationToken.None);

            FileStream fsSource = new FileStream(
                this.inputFullFilePath,
                FileMode.Open,
                FileAccess.Read);

            byte[] data = new byte[8000];
            while (true)
            {
                int count = fsSource.Read(data, 0, 8000);
                if (count == 0)
                {
                    break;
                }

                await this.ProcessData(ws, data, count);
            }

            await this.ProcessFinalData(ws);

            await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "OK", CancellationToken.None);
        }

        private async Task RecieveResult(ClientWebSocket ws)
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
                    if (string.IsNullOrEmpty(resultStr) == false && resultStr.Contains("\"partial\" :") == false && resultStr.Contains("\"text\" :") == true)
                    {
                        Console.WriteLine("");
                        Console.WriteLine("Appending Text to File...");
                        var resultObj = JsonConvert.DeserializeObject<Result>(resultStr);
                        File.AppendAllText(this.outputFullFilePath, resultObj.text + ". " + Environment.NewLine);
                    }
                }
            }
        }

        private async Task ProcessData(ClientWebSocket ws, byte[] data, int count)
        {
            await ws.SendAsync(new ArraySegment<byte>(data, 0, count), WebSocketMessageType.Binary, true, CancellationToken.None);
            await this.RecieveResult(ws);
        }

        private async Task ProcessFinalData(ClientWebSocket ws)
        {
            byte[] eof = Encoding.UTF8.GetBytes("{\"eof\" : 1}");
            await ws.SendAsync(new ArraySegment<byte>(eof), WebSocketMessageType.Text, true, CancellationToken.None);
            await this.RecieveResult(ws);
        }

        public class WordResult
        {
            public float conf { get; set; }
            public float end { get; set; }
            public float start { get; set; }
            public string word { get; set; }
        }

        public class Result
        {
            public string text { get; set; }
            [JsonProperty("result")]
            public WordResult[] wordResults { get; set; }
        }

    }
}
