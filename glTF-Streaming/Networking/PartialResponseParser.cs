using Assets.Scripts.Exceptions;
using Assets.Scripts.Networking.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;

namespace Assets.Scripts.Networking
{




    /// <summary>
    /// Parses HTTP responses with a 206 (Partial Content) or 200 (OK) status code.
    /// A PartialResponseParser provides an IPartialContentResponse that contains all relevant data parsed from the given data and headers.
    /// </summary>
    public class PartialResponseParser
    {
        public IResponse Response { get; private set; }
        public long ResponseCode { get; private set; }


        public PartialResponseParser(UnityWebResponse response)
        {
            var headers = response.ResponseHeaders;
            var body = response.Data;
            var responseCode = response.StatusCode;

            string contentRange = GetHeaderValue(headers, "content-range");
            string contentType = GetHeaderValue(headers, "content-type");

            if (responseCode != 206 && responseCode != 200)
                throw new ParseException($"Response with code {responseCode} was given when parser only handles codes 206 and 200");

            if (responseCode == 200)
            {
                Response = new CompleteResponse(body)
                {
                    UnityWebResponse = response
                };
            }
            else if (contentRange != null)
            {
                Response = new SinglePartResponse(contentType, contentRange, body)
                {
                    UnityWebResponse = response
                };
            }
            else
            {
                Response = new MultipartResponse(contentType, body)
                {
                    UnityWebResponse = response
                };
            }
        }

        private string GetHeaderValue(Dictionary<string, string> headers, string keyName)
        {
            foreach (var key in headers.Keys)
            {
                if (key.Equals(keyName, StringComparison.OrdinalIgnoreCase))
                    return headers[key];
            }
            return null;
        }
    }
}
