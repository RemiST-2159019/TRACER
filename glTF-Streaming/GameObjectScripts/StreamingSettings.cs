using Assets.Scripts.Enums;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Analytics;
using UnityEngine;

namespace Assets.Scripts.GameObjectScripts
{
    public class StreamingSettings
    {
        public BudgetStrategy Strategy { get; set; }
        public string ServerPath { get; set; }
        public FileToStream FileToStream { get; set; }
        public bool MetricsMode { get; set; }
        public bool BulkDownloadMode { get; set; }
        public int Patience { get; set; }
        public bool QuickRenderGeometry { get; set; }
        public float CameraSpeed { get; set; }
        public float CameraAcceleration { get; set; }

        public void SaveToPlayerPrefs()
        {
            var settings = JsonConvert.SerializeObject(this);
            PlayerPrefs.SetString("settings", settings);
            PlayerPrefs.Save();
        }

        public static StreamingSettings DefaultSettings => new StreamingSettings()
        {
            BulkDownloadMode = false,
            FileToStream = FileToStream.TwoHouses,
            MetricsMode = false,
            Patience = 1400,
            QuickRenderGeometry = false,
            ServerPath = "localhost:8080",
            Strategy = BudgetStrategy.Distance,
            CameraAcceleration = 5,
            CameraSpeed = 400
        };

        public static StreamingSettings LoadFromPlayerPrefs()
        {
            if (!PlayerPrefs.HasKey("settings"))
                return null;
            var settings = PlayerPrefs.GetString("settings");
            return JsonConvert.DeserializeObject<StreamingSettings>(settings);
        }
    }
}
