using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpeechRecognitionClient.Abstractions
{
    public interface IClient
    {
        public Task DecodeFile();
    }
}
