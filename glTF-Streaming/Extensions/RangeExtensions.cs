using System.Collections.Generic;
using System.Text;

namespace Assets.Scripts.Extensions
{
    public static class RangeExtensions
    {
        public static string ToDebugString(this List<ByteRange> ranges)
        {
            StringBuilder sb = new StringBuilder();
            foreach (ByteRange r in ranges)
            {
                sb.Append(r);
                if (!ranges[ranges.Count - 1].Equals(r))
                    sb.Append(", ");
            }
            return sb.ToString();
        }
    }
}
