using System;
using System.Runtime.Serialization;
using UnityEngine;

namespace tracer
{
    public class NetworkException : Exception
    {
        public NetworkException()
        {
        }

        public NetworkException(string message) : base(message)
        {
            Debug.LogException(this);
        }

        public NetworkException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected NetworkException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }

    public class NetworkSettingsException : NetworkException
    {
        public NetworkSettingsException()
        {
        }
        public NetworkSettingsException(string message) : base(message)
        {
        }
    }
}
