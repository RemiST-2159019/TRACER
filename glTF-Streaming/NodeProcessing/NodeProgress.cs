using Assets.Scripts;
using Assets.Scripts.Enums;
using GLTF.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Scripts.NodeProcessing
{
    public class ProgressInfo
    {
        public DownloadStatus DownloadStatus { get; set; }
        public RangeType RangeType { get; set; }
    }
    public class NodeProgress
    {
        private Dictionary<ByteRange, ProgressInfo> _ranges;
        public DownloadStatus DownloadStatus
        {
            get
            {
                bool foundDownloaded = false;
                var allRangesDownloaded = _ranges.Values.All(s =>
                {
                    var isDownloaded = s.DownloadStatus == DownloadStatus.Completed;
                    if (isDownloaded)
                        foundDownloaded = true;
                    return isDownloaded;
                });
                if (allRangesDownloaded)
                    return DownloadStatus.Completed;
                else if (foundDownloaded) // At least 1 range has been downloaded, but not all
                    return DownloadStatus.InProgress;
                else return DownloadStatus.Unbegun;
            }
        }
        public RenderStatus RenderStatus { get; set; }

        public NodeProgress(StreamingState state, Node node)
        {
            _ranges = new Dictionary<ByteRange, ProgressInfo>();
            NodeCalculator calculator = new NodeCalculator(state.GLTFRoot, state.GetJsonLength());
            var geometryRanges = calculator.CalculateGeometryRanges(node);
            var imageRanges = calculator.CalculateImageRanges(node);

            AddRanges(geometryRanges, RangeType.Geometry);
            AddRanges(imageRanges, RangeType.Image);


            RenderStatus = RenderStatus.Not;
        }


        private void AddRanges(List<ByteRange> ranges, RangeType type)
        {
            for (int i = 0; i < ranges.Count; i++)
            {
                _ranges.Add(ranges[i], new ProgressInfo()
                {
                    DownloadStatus = DownloadStatus.Unbegun,
                    RangeType = type
                });
            }
        }


        public List<ByteRange> GetRanges(RangeType type)
        {
            return _ranges.Keys.Where(k => _ranges[k].RangeType == type).ToList();
        }

        public bool HasDownloadedRangesOfType(RangeType type)
        {
            foreach (var kvp in _ranges)
            {
                if (kvp.Value.RangeType == type && kvp.Value.DownloadStatus != DownloadStatus.Completed)
                    return false;
            }
            return true;
        }

        public void TrackRange(ByteRange range, DownloadStatus status)
        {
            var rangesToModify = new List<ByteRange>();

            foreach (var trackedRange in _ranges.Keys)
            {
                if (range.CanContain(trackedRange) && _ranges[trackedRange].DownloadStatus != status)
                    rangesToModify.Add(trackedRange);
            }

            foreach (var modifiedRange in rangesToModify)
                _ranges[modifiedRange].DownloadStatus = status;
        }


        public void TrackRanges(List<ByteRange> ranges, DownloadStatus status)
        {
            foreach(var range in ranges)
                TrackRange(range, status);
        }

        public long GetGeometryByteLength()
        {
            long total = 0;
            foreach (var range in _ranges.Keys)
            {
                if (_ranges[range].RangeType != RangeType.Geometry) continue;
                total += range.GetLength();
            }
            return total;
        }


        public bool HasDownloadedGeometry()
        {
            return _ranges
                .Where(kvp => kvp.Value.RangeType == RangeType.Geometry)
                .All(kvp => kvp.Value.DownloadStatus == DownloadStatus.Completed);
        }

        public long GetUndownloadedByteLength()
        {
            long total = 0;
            foreach (var range in _ranges.Keys)
            {
                if (_ranges[range].DownloadStatus == DownloadStatus.Unbegun)
                    total += range.GetLength();
            }
            return total;
        }


        public bool CanContain(ByteRange range)
        {
            foreach (var r in _ranges.Keys)
            {
                if (r.CanContain(range))
                    return true;
            }
            return false;
        }

        public List<ByteRange> GetRanges()
        {
            return _ranges.Keys.ToList();
        }

        public void TrackAllRanges(DownloadStatus status)
        {
            foreach (var range in _ranges.Keys.ToList())
                _ranges[range].DownloadStatus = status;
        }

        public List<ByteRange> GetRangesOfStatus(DownloadStatus status)
        {
            return _ranges.Keys.Where(r => _ranges[r].DownloadStatus == status).ToList();
        }
    }
}
