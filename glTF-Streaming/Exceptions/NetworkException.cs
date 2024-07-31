using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.Exceptions
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
