using Assets.Scripts.BinaryFormat;
using Assets.Scripts.Networking;
using Assets.Scripts.Networking.Responses;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Networking;

namespace Assets.Scripts.Networking
{
    /// <summary>
    /// 
    /// </summary>
    public class GLBDownloader : ResourceDownloader
    {
        public static readonly DownloadSpeedTracker SpeedTracker = new DownloadSpeedTracker(10);
        /// <summary>
        /// Creates a GLBDownloader assuming the given uri points to a .glb resource.
        /// </summary>
        /// <param name="uri"></param>
        public GLBDownloader(string uri) : base(uri)
        {
        }


        /// <summary>
        /// Downloads the specified GLB file, excluding the actual binary data.
        /// <para>Specifically, the following is downloaded:</para>
        /// <list type="number">
        /// <item>12-byte header</item>
        /// <item>8-byte JSON subheader</item>
        /// <item>JSON chunk</item>
        /// <item>8-byte binary buffer subheader</item>
        /// </list>
        /// In order to download all of this info, two requests are made. First the header and json subheader are downloaded.
        /// By parsing the length of the JSON, the JSON chunk and binary subheader are downloaded next.
        /// </summary>
        /// <returns></returns>
        public async Task<GLBFile> DownloadBasicGLBFile()
        {
            var sw = Stopwatch.StartNew();
            GLBFile file = new GLBFile();
            var firstRange = ByteRange.Create(0, 19);
            var firstPart = await DownloadRangeAsync(firstRange);


            var data = firstPart.Data;
            file.Header = new GLTFHeader(data);
            file.JSONSubheader = new GLTFSubheader(data.ToList().Skip(12).ToArray());
            var jsonRange = ByteRange.Create(20, 20 + file.JSONSubheader.ChunkLength - 1 + 8);
            var secondPart = await DownloadRangeAsync(jsonRange);
            sw.Stop();
            SpeedTracker.AddDownloadSpeed(CalculateDownloadSpeed(sw.ElapsedMilliseconds, firstPart.Data.Length + secondPart.Data.Length));

            data = secondPart.Data;
            var json = Encoding.UTF8.GetString(data.Take((int)file.JSONSubheader.ChunkLength).ToArray());
            file.Json = json;
            var subHeader = new GLTFSubheader(data.Skip((int)file.JSONSubheader.ChunkLength).ToArray());
            file.Data = new byte[subHeader.ChunkLength];
            file.BinaryDataSubheader = subHeader;

            return file;
        }

        /// <summary>
        /// Calculates the approximate download speed in megabits per second (mbps) based on the DownloadTime.
        /// </summary>
        /// <returns></returns>
        public static float CalculateDownloadSpeed(long downloadTimeMs, long length)
        {
            float elapsedTimeSeconds = downloadTimeMs / 1000.0f;
            return (length * 8.0f) / (elapsedTimeSeconds * 1000000f);
        }

        /// <summary>
        /// Downloads the specified ranges of the glb file. If no ranges are specified, the entire file is downloaded.
        /// </summary>
        /// <param name="ranges"></param>
        /// <returns></returns>
        public async Task<IResponse> DownloadGLBAsync(List<ByteRange> ranges = null)
        {
            UnityWebResponse response;
            if (ranges == null || ranges.Count == 0)
                response = await DownloadFullAsync();
            else
                response = await DownloadRangesAsync(ranges);

            SpeedTracker.AddDownloadSpeed((CalculateDownloadSpeed(response.DownloadTime, response.Data.Length)));

            return await Task.Run(() =>
            {
                var parser = new PartialResponseParser(response);
                return parser.Response;
            });
        }
    }
}
