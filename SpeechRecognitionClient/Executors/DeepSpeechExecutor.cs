using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SpeechRecognitionClient.Abstractions;

namespace SpeechRecognitionClient.Executors
{
    public class DeepSpeechExecutor : IExecutor
    {
        private string webSocketUrl;
        private string inputWav;
        private string outputFile;

        public void Execute(string url, string inputWav, string outputTxt)
        {
            this.webSocketUrl = url;
            this.inputWav = inputWav;
            this.outputFile = outputTxt;

            DecodeFile().Wait();
        }

        private async Task DecodeFile()
        {
            ClientWebSocket ws = new ClientWebSocket();
            await ws.ConnectAsync(new Uri(this.webSocketUrl), CancellationToken.None);

            FileStream fsSource = new FileStream(
                this.inputWav,
                FileMode.Open,
                FileAccess.Read);

            byte[] data = new byte[1024 * 8];
            while (true)
            {
                int count = fsSource.Read(data, 0, 1024 * 8);
                if (count == 0)
                {
                    break;
                }

                await this.SendFile(ws, data, count);
            }

            await this.ProcessResult(ws);

            await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "OK", CancellationToken.None);
        }

        private async Task SendFile(ClientWebSocket ws, byte[] data, int count)
        {
            await ws.SendAsync(new ArraySegment<byte>(data, 0, count), WebSocketMessageType.Binary, true, CancellationToken.None);
            await this.RecieveStatus(ws);
        }

        private async Task RecieveStatus(ClientWebSocket ws)
        {
            ArraySegment<Byte> buffer = new ArraySegment<byte>(new Byte[1024 * 8]);

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
                    Console.WriteLine($"{DateTime.UtcNow.Hour}:{DateTime.UtcNow.Minute}:{DateTime.UtcNow.Second} || Result: {resultStr}");
                    if(resultStr != "OK")
                    {
                        Console.WriteLine("|| ERROR || ## Something Went Wrong. Please, Check DeepSpeech WebSocket Server Logs.");
                    }
                }
            }
        }

        private async Task ProcessResult(ClientWebSocket ws)
        {
            byte[] eof = Encoding.UTF8.GetBytes("{\"eof\" : 1}");
            await ws.SendAsync(new ArraySegment<byte>(eof), WebSocketMessageType.Text, true, CancellationToken.None);

            Console.WriteLine("");
            Console.WriteLine("");
            Console.WriteLine("Result:");
            Console.WriteLine("");

            //TODO: Receive a FILE

            await this.ReceiveResult(ws);
        }

        private async Task ReceiveResult(ClientWebSocket ws)
        {
            var buffer = new byte[1024 * 8];
            WebSocketReceiveResult result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            while (!result.CloseStatus.HasValue)
            {
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    using (var ms = new MemoryStream())
                    {
                        do
                        {
                            var eofResultBuffer = new ArraySegment<byte>(buffer);
                            ms.Write(eofResultBuffer.Array, eofResultBuffer.Offset, result.Count);
                        }
                        while (!result.EndOfMessage);

                        ms.Seek(0, SeekOrigin.Begin);

                        using (var reader = new StreamReader(ms, Encoding.UTF8))
                        {
                            var eofResultStr = reader.ReadToEnd();
                            if (eofResultStr.Contains("{\"eof\" : 1}"))
                            {
                                Console.WriteLine("");
                                Console.WriteLine($"{DateTime.UtcNow.Hour}:{DateTime.UtcNow.Minute}:{DateTime.UtcNow.Second} || File was successfullly received || Result: {eofResultStr}");
                                break;
                            }
                        }
                    }
                }
                else
                {
                    using (var stream = new FileStream(outputFile, File.Exists(outputFile) ? FileMode.Append : FileMode.OpenOrCreate))
                    {
                        stream.Write(buffer, 0, buffer.Length);
                    }

                    Console.WriteLine(System.Text.Encoding.Default.GetString(buffer));

                    await ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes("OK")), WebSocketMessageType.Text, true, CancellationToken.None);
                    result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                }
            }
        }
    }
}
