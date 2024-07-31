using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Windows;
using Debug = UnityEngine.Debug;
using Directory = System.IO.Directory;
using File = System.IO.File;

namespace Assets.Scripts.QualityMetrics
{
    public class QualityMetricsTool
    {
        private string _path;
        private static readonly string VMAFLogPath = "";
        public string ReferenceFileName { get; set; }
        private Dictionary<string, DateTime> _screenshotTimes;


        static QualityMetricsTool()
        {
            VMAFLogPath = Application.persistentDataPath + "/results.json";

        }
         
        public QualityMetricsTool(string imagePath, string referenceName = null)
        {
            _path = imagePath;
            if (referenceName != null)
                ReferenceFileName = referenceName;
            _screenshotTimes = new Dictionary<string, DateTime>();
            if (!Directory.Exists(_path))
                _path = Application.persistentDataPath;
        }

        public void AddScreenshotTime(string screenshotName, DateTime timeTaken)
        {
            _screenshotTimes[screenshotName] = timeTaken;
        }


        public string GetScreenshotName(int screenshotIndex)
        {
            return _screenshotTimes.ElementAt(screenshotIndex).Key;
        }

        public DateTime GetScreenshotTime(string screenshotName)
        {
            return _screenshotTimes[screenshotName];    
        }

        private void ExecuteCommand(string command, string args)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = command,
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            StringBuilder outputBuilder = new StringBuilder();

            using (Process process = new Process())
            {
                process.StartInfo = startInfo;

                process.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        outputBuilder.AppendLine(e.Data);
                };

                process.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        outputBuilder.AppendLine(e.Data);
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                process.WaitForExit();

                string output = outputBuilder.ToString();
            }
        }

        public VMAFResults CalculateMetrics()
        {
            var modelPath = $"{Application.dataPath}/VMAF/vmaf_v0.6.1.json";

            if (!File.Exists(modelPath))
            {
                throw new DirectoryNotFoundException($"Using path: {modelPath}: Couldn't find 'vmaf_v0.6.1.json' model in Assets/VMAF/");
            }
            modelPath = modelPath.Replace(":", "\\\\:");
            var logPath = VMAFLogPath.Replace(":", "\\\\:");


            string args = $"/c ffmpeg -framerate 1 -i {$"{_path}/%d.png"} -i {$"{_path}/{ReferenceFileName}"} -lavfi " +
                $"\"libvmaf=model_path={modelPath}:log_path={logPath}:psnr=1:ssim=1:ms_ssim=1:log_fmt=json\" -f null -";

            ExecuteCommand("cmd.exe", args);
            var output = File.ReadAllText(VMAFLogPath);
            var json = JsonConvert.DeserializeObject<VMAFResults>(output);
            return json;
        }
    }
}

