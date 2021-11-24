using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpeechRecognitionClient.Models
{
    public class DeepSpeechResponse
    {
        [JsonProperty("transcripts")]
        public List<Transcript> Transcripts { get; set; }
    }

    public class Transcript
    {
        [JsonProperty("confidence")]
        public string Confidence { get; set; }

        [JsonProperty("words")]
        public List<WordInfo> WordInfos { get; set; }
    }

    public class WordInfo
    {
        [JsonProperty("word")]
        public string Word { get; set; }
        [JsonProperty("start_time")]
        public string StartTime { get; set; }
        [JsonProperty("duration")]
        public string Duration { get; set; }
    }
}
