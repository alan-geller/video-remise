using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace FFMpegLauncher
{
    internal class Program
    {
        static void Main(string[] args)
        {
            StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            var data = File.ReadAllLines($"{localFolder.Path}\\ffmpeg-command");
            var splitTime = data[0];
            var inputFile = data[1];
            var output1 = data[2];
            var output2 = data[3];

            Process process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/C ffmpeg -i \"{localFolder.Path}\\toSplit.mp4\" -t {splitTime} \"{output1}\" -ss {splitTime} \"{output2}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };

            process.Start();
            process.OutputDataReceived += (sender, e) => { };
            process.ErrorDataReceived += (sender, e) => { };
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            process.WaitForExit();

            File.Create($"{localFolder.Path}\\Done");
        }
    }
}
