using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tracer
{
    public class DataReceivedEventArgs : EventArgs
    {
        public UnityWebResponse SceneData { get; }

        public DataReceivedEventArgs(UnityWebResponse sceneData)
        {
            SceneData = sceneData;
        }
    }
}
