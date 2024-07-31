using GLTFast.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Scripts.Networking
{
    public class DownloadSpeedTracker
    {
        private Queue<float> _downloadSpeeds = new Queue<float>();
        public int WindowSize { get; set; }

        public DownloadSpeedTracker(int windowSize)
        {
            WindowSize = windowSize;
        }

        public void AddDownloadSpeed(float downloadSpeed)
        {
            _downloadSpeeds.Enqueue(downloadSpeed);
            if (_downloadSpeeds.Count > WindowSize)
            {
                _downloadSpeeds.Dequeue();
            }
        }

        public float CalculateAverageSpeed()
        {
            float totalSpeed = 0;
            if (_downloadSpeeds.Count == 0)
                return 0;
            foreach (var speed in _downloadSpeeds)
            {
                totalSpeed += speed;
            }
            return totalSpeed / _downloadSpeeds.Count;
        }
    }
}
