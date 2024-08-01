using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.Networking;

namespace tracer
{
    /// <summary>
    /// Downloads resources asynchronously using UnityWebRequest.
    /// </summary>
    public abstract class ResourceDownloader
    {
        private string _resourceUri;
        private int _timeout = 1_800_000; // timeout after 30 minutes
        /// <summary>
        /// <para>Creates a <c>ResourceDownloader</c> instance that will download the resource from the provided URI.</para>
        /// <example>
        /// This shows how to instantiate a <c>ResourceDownloader</c> for an example resource.
        /// <code>
        /// var downloader = new ResourceDownloader("http://localhost/largefile.zip");
        /// </code>
        /// </example>
        /// </summary>
        /// <param name="uri">The location of the resource to download.</param>
        public ResourceDownloader(string uri)
        {
            _resourceUri = uri;
        }





        /// <summary>
        /// Starts downloading the entire resource. 
        /// </summary>
        /// <returns>A task that represents the asynchronous download. The task result contains the <c>WebResponse</c> associated with the download.</returns>
        /// <exception cref="WebException">if any errors occur in the downloading process</exception>
        public async Task<UnityWebResponse> DownloadFullAsync(Dictionary<string, string> extraHeaders = null)
        {
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.CancelAfter(_timeout);
            var token = cancellationTokenSource.Token;
            var sw = Stopwatch.StartNew();
            try
            {
                using (UnityWebRequest webRequest = UnityWebRequest.Get(_resourceUri))
                {
                    token.ThrowIfCancellationRequested();
                    if (extraHeaders != null)
                    {
                        foreach (var key in extraHeaders.Keys)
                            webRequest.SetRequestHeader(key, extraHeaders[key]);
                    }

                    var asyncOperation = webRequest.SendWebRequest();

                    while (!asyncOperation.isDone)
                    {
                        await Task.Yield();
                        token.ThrowIfCancellationRequested();
                    }


                    if (webRequest.result != UnityWebRequest.Result.Success)
                        throw new OperationCanceledException();

                    var responseMessage = ((HttpStatusCode)webRequest.responseCode).ToString();
                    sw.Stop();
                    return new UnityWebResponse()
                    {
                        Data = webRequest.downloadHandler.data,
                        ResponseHeaders = webRequest.GetResponseHeaders(),
                        DownloadTime = sw.ElapsedMilliseconds,
                        StatusCode = webRequest.responseCode,
                        StatusMessage = responseMessage
                    };
                }
            }
            catch (OperationCanceledException)
            {
                UnityEngine.Debug.LogError($"Operation was canceled");
                throw;
            }
            finally
            {
                cancellationTokenSource.Dispose();
            }
        }


        /// <summary>
        /// Downloads the specified ranges from the resource. It is not guaranteed that the server will respond with precisely the requested bytes.
        /// </summary>
        /// <param name="ranges"></param>
        /// <param name="extraHeaders"></param>
        /// <returns>A task that represents the asynchronous download. The task result contains the WebResponse associated with the download.</returns>
        public async Task<UnityWebResponse> DownloadRangesAsync(List<ByteRange> ranges, Dictionary<string, string> extraHeaders = null)
        {
            ranges = NormalizeRanges(ranges);
            var headers = new Dictionary<string, string>();
            if (extraHeaders != null)
                headers.AddRange(extraHeaders); // Add user-provided headers
            headers.Add("Range", CreateRangeHeaderString(ranges));
            return await DownloadFullAsync(headers);
        }

        /// <summary>
        /// Downloads the specified range from the resource. It is not guaranteed that the server will respond with precisely the requested bytes.
        /// </summary>
        /// <param name="ranges"></param>
        /// <param name="extraHeaders"></param>
        /// <returns>A task that represents the asynchronous download. The task result contains the WebResponse associated with the download.</returns>
        public async Task<UnityWebResponse> DownloadRangeAsync(ByteRange range, Dictionary<string, string> extraHeaders = null)
        {
            return await DownloadRangesAsync(new List<ByteRange> { range }, extraHeaders);
        }


        public static List<ByteRange> NormalizeRanges(List<ByteRange> ranges)
        {
            if (ranges == null || ranges.Count <= 1)
                return ranges;

            var rangesCopy = new List<ByteRange>();
            ranges.ForEach(r =>
            {
                rangesCopy.Add(new ByteRange(r.StartByte, r.EndByte));
            });

            rangesCopy.Sort((a, b) => a.StartByte.CompareTo(b.StartByte));

            int currentIndex = 0;
            while (currentIndex < rangesCopy.Count - 1)
            {
                ByteRange currentRange = rangesCopy[currentIndex];
                ByteRange nextRange = rangesCopy[currentIndex + 1];
                long gap = nextRange.StartByte - currentRange.EndByte;

                // Coalesce ranges if they are consecutive or if their gap is small enough
                if (currentRange.EndByte >= nextRange.StartByte - 1 || gap < 80)
                {
                    currentRange.SetRange(currentRange.StartByte, Math.Max(currentRange.EndByte, nextRange.EndByte));
                    rangesCopy.RemoveAt(currentIndex + 1);
                }
                else
                    currentIndex++;
            }
            return rangesCopy;
        }

        protected static string CreateRangeHeaderString(List<ByteRange> ranges)
        {
            string rangeHeader = "bytes=";
            foreach (ByteRange range in ranges)
            {
                rangeHeader += $"{range.StartByte}-{range.EndByte}";

                if (ranges.IndexOf(range) < ranges.Count - 1)
                    rangeHeader += ",";
            }
            return rangeHeader;
        }
    }
}
