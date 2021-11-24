using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Newtonsoft.Json;
using SpeechRecognitionClient.Abstractions;
using SpeechRecognitionClient.Models;

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
            Console.WriteLine("");
            Console.WriteLine($"{DateTime.UtcNow.Date.Year}-{DateTime.UtcNow.Date.Month}-{DateTime.UtcNow.Day} {DateTime.UtcNow.Hour}:{DateTime.UtcNow.Minute}:{DateTime.UtcNow.Second} || INFO || Connectiong to the Server...");

            ClientWebSocket ws = new ClientWebSocket();
            ws.Options.RemoteCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
            await ws.ConnectAsync(new Uri(this.webSocketUrl), CancellationToken.None);

            Console.WriteLine($"{DateTime.UtcNow.Date.Year}-{DateTime.UtcNow.Date.Month}-{DateTime.UtcNow.Day} {DateTime.UtcNow.Hour}:{DateTime.UtcNow.Minute}:{DateTime.UtcNow.Second} || INFO || Connection to the server was established.");
            Console.WriteLine($"{DateTime.UtcNow.Date.Year}-{DateTime.UtcNow.Date.Month}-{DateTime.UtcNow.Day} {DateTime.UtcNow.Hour}:{DateTime.UtcNow.Minute}:{DateTime.UtcNow.Second} || INFO || Sending Audio File to the Server...");

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

            Console.WriteLine($"{DateTime.UtcNow.Date.Year}-{DateTime.UtcNow.Date.Month}-{DateTime.UtcNow.Day} {DateTime.UtcNow.Hour}:{DateTime.UtcNow.Minute}:{DateTime.UtcNow.Second} || INFO || Audio File was successfully sent to the server.");
            Console.WriteLine($"{DateTime.UtcNow.Date.Year}-{DateTime.UtcNow.Date.Month}-{DateTime.UtcNow.Day} {DateTime.UtcNow.Hour}:{DateTime.UtcNow.Minute}:{DateTime.UtcNow.Second} || INFO || Waiting for response from server...");

            await this.ProcessResult(ws);

            Console.WriteLine($"{DateTime.UtcNow.Date.Year}-{DateTime.UtcNow.Date.Month}-{DateTime.UtcNow.Day} {DateTime.UtcNow.Hour}:{DateTime.UtcNow.Minute}:{DateTime.UtcNow.Second} || INFO || Result from server was successfully received.");
            Console.WriteLine($"{DateTime.UtcNow.Date.Year}-{DateTime.UtcNow.Date.Month}-{DateTime.UtcNow.Day} {DateTime.UtcNow.Hour}:{DateTime.UtcNow.Minute}:{DateTime.UtcNow.Second} || INFO || Closing connection with Server.");

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
                    if (resultStr != "OK")
                    {
                        Console.WriteLine($"{DateTime.UtcNow.Date.Year}-{DateTime.UtcNow.Date.Month}-{DateTime.UtcNow.Day} {DateTime.UtcNow.Hour}:{DateTime.UtcNow.Minute}:{DateTime.UtcNow.Second} || ERROR || ## Something Went Wrong. Please, Check DeepSpeech WebSocket Server Logs.");
                    }
                }
            }
        }

        private async Task ProcessResult(ClientWebSocket ws)
        {
            byte[] eof = Encoding.UTF8.GetBytes("{\"eof\" : 1}");
            await ws.SendAsync(new ArraySegment<byte>(eof), WebSocketMessageType.Text, true, CancellationToken.None);
            await this.ReceiveResult(ws);
        }

        private async Task ReceiveResult(ClientWebSocket ws)
        {
            int bytesToRead = 1024 * 8;
            var buffer = new byte[bytesToRead];
            WebSocketReceiveResult result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

            Console.WriteLine($"{DateTime.UtcNow.Date.Year}-{DateTime.UtcNow.Date.Month}-{DateTime.UtcNow.Day} {DateTime.UtcNow.Hour}:{DateTime.UtcNow.Minute}:{DateTime.UtcNow.Second} || INFO || Server transcribed Audio File successfully.");
            Console.WriteLine($"{DateTime.UtcNow.Date.Year}-{DateTime.UtcNow.Date.Month}-{DateTime.UtcNow.Day} {DateTime.UtcNow.Hour}:{DateTime.UtcNow.Minute}:{DateTime.UtcNow.Second} || INFO || Receiving transcribed text from server...");

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
                                Console.WriteLine($"{DateTime.UtcNow.Date.Year}-{DateTime.UtcNow.Date.Month}-{DateTime.UtcNow.Day} {DateTime.UtcNow.Hour}:{DateTime.UtcNow.Minute}:{DateTime.UtcNow.Second} || INFO || File was successfullly received || Result: {eofResultStr}");
                                break;
                            }
                        }
                    }
                }
                else
                {
                    using (var stream = new FileStream(outputFile, File.Exists(outputFile) ? FileMode.Append : FileMode.OpenOrCreate))
                    {
                        stream.Write(buffer, 0, result.Count);
                    }

                    await ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes("OK")), WebSocketMessageType.Text, true, CancellationToken.None);
                    result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                }
            }
        }
    }
}
