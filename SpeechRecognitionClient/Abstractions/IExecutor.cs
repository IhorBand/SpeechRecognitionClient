using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpeechRecognitionClient.Abstractions
{
    public interface IExecutor
    {
        public void Execute(string url, string inputWav, string outputTxt);
    }
}
