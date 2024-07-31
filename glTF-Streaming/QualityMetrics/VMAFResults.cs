using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Scripts.QualityMetrics
{
    /// <summary>
    /// Class used to deserialize ffmpeg VMAF results
    /// </summary>
    public class VMAFResults
    {
        [JsonProperty("version")]
        public string Version { get; set; }
        [JsonProperty("fps")]
        public double FPS { get; set; }
        [JsonProperty("frames")]
        public List<Frame> Frames { get; set; }
        [JsonProperty("pooled_metrics")]
        public PooledMetrics PooledMetrics { get; set; }
        [JsonProperty("aggregate_metrics")]
        public Dictionary<string, object> AggregateMetrics { get; set; }
    }
    public class Metrics
    {
        [JsonProperty("integer_adm2")]
        public double IntegerAdm2 { get; set; }
        [JsonProperty("integer_adm_scale0")]
        public double IntegerAdmScale0 { get; set; }
        [JsonProperty("integer_adm_scale1")]
        public double IntegerAdmScale1 { get; set; }
        [JsonProperty("integer_adm_scale2")]
        public double IntegerAdmScale2 { get; set; }
        [JsonProperty("integer_adm_scale3")]
        public double IntegerAdmScale3 { get; set; }
        [JsonProperty("integer_motion2")]
        public double IntegerMotion2 { get; set; }
        [JsonProperty("integer_motion")]
        public double IntegerMotion { get; set; }
        [JsonProperty("integer_vif_scale0")]
        public double IntegerVifScale0 { get; set; }
        [JsonProperty("integer_vif_scale1")]
        public double IntegerVifScale1 { get; set; }
        [JsonProperty("integer_vif_scale2")]
        public double IntegerVifScale2 { get; set; }
        [JsonProperty("integer_vif_scale3")]
        public double IntegerVifScale3 { get; set; }
        [JsonProperty("psnr")]
        public double PSNR { get; set; }
        [JsonProperty("ssim")]
        public double SSIM { get; set; }
        [JsonProperty("ms_ssim")]
        public double MSSSIM { get; set; }
        [JsonProperty("vmaf")]
        public double VMAF { get; set; }
    }

    public class Frame
    {
        [JsonProperty("frameNum")]
        public int FrameNum { get; set; }
        [JsonProperty("metrics")]
        public Metrics Metrics { get; set; }
    }

    public class MetricPSNR : PooledMetric
    {

    }

    public class MetricVMAF : PooledMetric
    {

    }

    public class MetricMSSSIM : PooledMetric
    {

    }

    public class PooledMetrics
    {
        [JsonProperty("ssim")]
        public MetricSSIM SSIM { get; set; }
        [JsonProperty("psnr")]
        public MetricPSNR PSNR { get; set; }
        [JsonProperty("ms_ssim")]
        public MetricPSNR MSSSIM { get; set; }
        [JsonProperty("vmaf")]
        public MetricPSNR VMAF { get; set; }
        // Add other metrics here
    }

    public class PooledMetric
    {
        [JsonProperty("min")]
        public float Min { get; set; }
        [JsonProperty("max")]
        public float Max { get; set; }
        [JsonProperty("mean")]
        public float Mean { get; set; }
        [JsonProperty("harmonic_mean")]
        public float HarmonicMean { get; set; }
    }

    public class MetricSSIM : PooledMetric
    {

    }
}
