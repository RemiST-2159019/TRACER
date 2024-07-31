using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;

namespace tracer
{
    public class MultipartResponse : IResponse
    {
        public List<MultipartResponsePart> Parts { get; private set; } = new List<MultipartResponsePart>();
        public UnityWebResponse UnityWebResponse { get; set; }
        public ResponseType ResponseType => ResponseType.Multiple;

        public ContentType ContentType { get; private set; }


        public MultipartResponse(string contentType, byte[] body)
        {
            try
            {
                ContentType = contentType != null ? new ContentType(contentType) : null;
            }
            catch
            {
                throw new WebException($"Error parsing content type from '{contentType}'");
            }
            if (ContentType != null && !ContentType.MediaType.Equals("multipart/byteranges", StringComparison.OrdinalIgnoreCase))
            {
                throw new WebException("Multipart response must have 'multipart/byteranges' media type in Content-Type header");
            }
            ParseMultipartResponse(body);
        }

        private void ParseMultipartResponse(byte[] data)
        {
            var cr = Encoding.UTF8.GetBytes($"\r");
            var lf = Encoding.UTF8.GetBytes($"\n");
            var crlf = Encoding.UTF8.GetBytes($"\r\n");
            var doubleNewLine = Encoding.UTF8.GetBytes($"\r\n\r\n");
            var boundaryBytes = Encoding.UTF8.GetBytes($"--{ContentType.Boundary}");
            var endBoundaryBytes = Encoding.UTF8.GetBytes($"--{ContentType.Boundary}--");
            bool inBoundary = false;
            bool foundFirstBoundary = false;

            MultipartResponsePart currentPart = new MultipartResponsePart();

            for (int i = 0; i < data.Length; i++)
            {
                if (IsEqualPartial(data, i, doubleNewLine))
                {
                    if (foundFirstBoundary && inBoundary)
                    {
                        inBoundary = false;
                    }
                }
                else if ((IsEqualPartial(data, i, crlf)
                    || IsEqualPartial(data, i, cr)
                    || IsEqualPartial(data, i, lf)) && inBoundary)
                {
                }
                else if (IsEqualToRange(data, i))
                {
                    var rangeStr = GetUTF8StringUntilNewline(data, i);
                    var rangeValue = rangeStr.Split(":")[1].TrimStart();
                    currentPart.ContentRange = ContentRangeHeaderValue.Parse(rangeValue);
                }
                else if (IsEqualToType(data, i))
                {
                    var typeStr = GetUTF8StringUntilNewline(data, i);
                    var typeValue = typeStr.Split(":")[1].TrimStart();
                    currentPart.ContentType = new ContentType(typeValue);
                }
                else if (IsEqualPartial(data, i, endBoundaryBytes))
                {
                    RemoveLeadingAndTrailingNewlines(currentPart.Data);
                    Parts.Add(currentPart);
                    break;
                }
                else if (IsEqualPartial(data, i, boundaryBytes))
                {
                    if (foundFirstBoundary)
                    {
                        RemoveLeadingAndTrailingNewlines(currentPart.Data);
                        Parts.Add(currentPart);
                        currentPart = new MultipartResponsePart();
                    }
                    foundFirstBoundary = true;
                    inBoundary = true;
                }
                else if (!inBoundary && foundFirstBoundary)
                    currentPart.Data.Add(data[i]);
                else
                {
                }
            }
        }

        private string GetUTF8StringUntilNewline(byte[] data, int index)
        {
            List<byte> bytes = new List<byte>();
            while (data[index] != '\r' && data[index + 1] != '\n')
            {
                bytes.Add(data[index]);
                if (index + 2 >= data.Length)
                {
                    return null;
                }
                index++;
            }
            return Encoding.UTF8.GetString(bytes.ToArray());
        }

        /// <summary>
        /// Written for performance
        /// </summary>
        /// <param name="data"></param>
        /// <param name="startIndex"></param>
        /// <returns></returns>
        private bool IsEqualToType(byte[] data, int startIndex)
        {
            if (startIndex + 12 > data.Length)
                return false;
            var i = startIndex;
            return (data[i] == 'c' || data[i] == 'C') &&
                   (data[i + 1] == 'o' || data[i + 1] == 'O') &&
                   (data[i + 2] == 'n' || data[i + 2] == 'N') &&
                   (data[i + 3] == 't' || data[i + 3] == 'T') &&
                   (data[i + 4] == 'e' || data[i + 4] == 'E') &&
                   (data[i + 5] == 'n' || data[i + 5] == 'N') &&
                   (data[i + 6] == 't' || data[i + 6] == 'T') &&
                   (data[i + 7] == '-') &&
                   (data[i + 8] == 't' || data[i + 8] == 'T') &&
                   (data[i + 9] == 'y' || data[i + 9] == 'Y') &&
                   (data[i + 10] == 'p' || data[i + 10] == 'P') &&
                   (data[i + 11] == 'e' || data[i + 11] == 'E');
        }


        /// <summary>
        /// Written for performance
        /// </summary>
        /// <param name="data"></param>
        /// <param name="startIndex"></param>
        /// <returns></returns>
        private bool IsEqualToRange(byte[] data, int startIndex)
        {
            if (startIndex + 13 > data.Length)
                return false;
            var i = startIndex;
            return (data[i] == 'c' || data[i] == 'C') &&
                   (data[i + 1] == 'o' || data[i + 1] == 'O') &&
                   (data[i + 2] == 'n' || data[i + 2] == 'N') &&
                   (data[i + 3] == 't' || data[i + 3] == 'T') &&
                   (data[i + 4] == 'e' || data[i + 4] == 'E') &&
                   (data[i + 5] == 'n' || data[i + 5] == 'N') &&
                   (data[i + 6] == 't' || data[i + 6] == 'T') &&
                   (data[i + 7] == '-') &&
                   (data[i + 8] == 'r' || data[i + 8] == 'R') &&
                   (data[i + 9] == 'a' || data[i + 9] == 'A') &&
                   (data[i + 10] == 'n' || data[i + 10] == 'N') &&
                   (data[i + 11] == 'g' || data[i + 11] == 'G') &&
                   (data[i + 12] == 'e' || data[i + 12] == 'E');
        }

        private void RemoveLeadingAndTrailingNewlines(List<byte> byteList)
        {
            // Remove leading newlines
            while (byteList.Count > 0 && (byteList[0] == '\n' || byteList[0] == '\r'))
            {
                byteList.RemoveAt(0);
            }

            // Remove trailing newlines
            while (byteList.Count > 0 && (byteList[byteList.Count - 1] == '\n' || byteList[byteList.Count - 1] == '\r'))
            {
                byteList.RemoveAt(byteList.Count - 1);
            }
        }

        private bool IsEqualPartial(byte[] byteArray, int startIndex, byte[] targetBytes)
        {
            // Check if the length from the start index is less than the length of the target bytes
            if (byteArray.Length - startIndex < targetBytes.Length)
                return false;

            for (int i = 0; i < targetBytes.Length; i++)
            {
                if (byteArray[startIndex + i] != targetBytes[i])
                    return false;
            }
            return true;
        }

        public List<ByteRange> GetDownloadedRanges()
        {
            var ranges = new List<ByteRange>();
            foreach(var part in Parts)
            {
                ranges.Add(new ByteRange((long)part.ContentRange.From, (long)part.ContentRange.To));
            }
            return ranges;
        }
    }
}
