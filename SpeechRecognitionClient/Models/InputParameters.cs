using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpeechRecognitionClient.Models
{
    public static class InputParameters
    {
        public const string InputFile = "inputWav=|input=|i=";
        public const string OutputFile = "outputTxt=|output=|o=";
        public const string WebSocketUrl = "webSocketUrl=|wsUrl=|url=|wsu=|u=";
        public const string Technology = "technology=|t=";
        public const string Help = "h|help";
        public const string TechnologyTypes = "technologies";
    }
}
