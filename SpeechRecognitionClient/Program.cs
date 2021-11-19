using System;
using SpeechRecognitionClient.Abstractions;
using SpeechRecognitionClient.Models;
using NDesk.Options;

namespace SpeechRecognitionClient
{
    public class Program
    {
        static void Main(string[] args)
        {
            var inputFileFullPath = string.Empty;
            var outputFileFullPath = string.Empty;
            var webSocketUrl = string.Empty;
            var technology = string.Empty;
            var isPrintHelp = false;
            var isPrintTechnologies = false;

            var technologyValuesDescr = $"{TechnologyTypes.Coqui}, {TechnologyTypes.DeepSpeech}, {TechnologyTypes.Vosk}";

            OptionSet p = new OptionSet()
                .Add(InputParameters.Help, "Print Help", value => { isPrintHelp = value != null; })
                .Add(InputParameters.TechnologyTypes, "Print Technologies that you can use", value => { isPrintTechnologies = value != null; })
                .Add(InputParameters.InputFile, "input File Full(Absolute) Path", value => { inputFileFullPath = value; })
                .Add(InputParameters.OutputFile, "output File Full(Absolute) Path", value => { outputFileFullPath = value; })
                .Add(InputParameters.WebSocketUrl, "URL to call websocket and execute SpeechRecognition", value => { webSocketUrl = value; })
                .Add(InputParameters.Technology, $"What Technology you want to use\nCorrect Technology values: {technologyValuesDescr})", value => { technology = value; });
            var extraValues = p.Parse(args);

            if(isPrintHelp)
            {
                p.WriteOptionDescriptions(Console.Out);
                return;
            }

            if(isPrintTechnologies)
            {
                Console.WriteLine("List of Technologies that you can use:");
                Console.WriteLine(technologyValuesDescr);
                return;
            }

            Console.WriteLine("Input Parameters: ");
            Console.WriteLine($"Input File Full Path: {inputFileFullPath}");
            Console.WriteLine($"Output File Full Path: {outputFileFullPath}");
            Console.WriteLine($"VoskUrl: {webSocketUrl}");
            Console.WriteLine($"Technology: {technology}");
            Console.WriteLine("");
            Console.WriteLine("Recognizing Wav...");

            IExecutor executor = null;

            if (technology == TechnologyTypes.Vosk)
            {
                executor = new Executors.VoskExecutor();
            }
            else if (technology == TechnologyTypes.Coqui)
            {
                executor = new Executors.CoquiExecutor();
            }
            else if(technology == TechnologyTypes.DeepSpeech)
            {
                executor = new Executors.DeepSpeechExecutor();
            }

            if (executor != null)
            {
                executor.Execute(webSocketUrl, inputFileFullPath, outputFileFullPath);

                Console.WriteLine("");
                Console.WriteLine("Wav file was Successfully Recognized.");
            }
            else
            {
                Console.WriteLine("");
                Console.WriteLine("Please, Select correct technology.");
            }

            return;

        }
    }
}
