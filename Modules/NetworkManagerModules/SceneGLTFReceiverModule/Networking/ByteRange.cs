using System;
using System.Collections.Generic;

namespace tracer
{
    public class ByteRange
    {
        public long StartByte { get; private set; }
        public long EndByte { get; private set; }
        public ByteRange(long startByte, long endByte)
        {
            SetRange(startByte, endByte);
        }

        public static ByteRange Create(long startByte, long endByte)
        {
            return new ByteRange(startByte, endByte);
        }

        public void SetRange(long startByte, long endByte)
        {
            if (endByte < startByte)
                throw new ArgumentException("Cannot create range: end byte must be greater or equal to start byte.");
            StartByte = startByte;
            EndByte = endByte;
        }

        public bool CanContain(ByteRange range)
        {
            return StartByte <= range.StartByte && EndByte >= range.EndByte;
        }

        /// <summary>
        /// Returns the amount of bytes within this ByteRange.
        /// </summary>
        /// <returns></returns>
        public long GetLength()
        {
            return EndByte - StartByte + 1;
        }

        /// <summary>
        /// Extracts bytes from the given data using this ByteRange.
        /// </summary>
        /// <param name="data">the data to extract from</param>
        /// <returns></returns>
        public byte[] GetBytes(byte[] data)
        {
            byte[] extracted = new byte[GetLength()];
            for (int i = 0; i < GetLength(); i++)
                extracted[i] = data[i + StartByte];
            return extracted;
        }

        public override bool Equals(object obj)
        {
            return obj is ByteRange range &&
                   StartByte == range.StartByte &&
                   EndByte == range.EndByte;
        }

        public override string ToString()
        {
            return $"{StartByte}-{EndByte}";
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(StartByte, EndByte);
        }

        public static List<List<ByteRange>> PartitionRanges(List<ByteRange> ranges, long maxBytesPerList = 5000000)
        {
            List<List<ByteRange>> partitionedRanges = new List<List<ByteRange>>();

            List<ByteRange> currentPartition = new List<ByteRange>();
            long currentPartitionBytes = 0;

            foreach (var range in ranges)
            {
                long rangeSize = range.GetLength();

                if (rangeSize >= maxBytesPerList)
                {
                    partitionedRanges.Add(new List<ByteRange> { range });
                }
                else
                {
                    if (currentPartitionBytes + rangeSize > maxBytesPerList)
                    {
                        partitionedRanges.Add(currentPartition);
                        currentPartition = new List<ByteRange>();
                        currentPartitionBytes = 0;
                    }
                    currentPartition.Add(range);
                    currentPartitionBytes += rangeSize;
                }
            }

            // Add the last partition if it's not empty
            if (currentPartition.Count > 0)
            {
                partitionedRanges.Add(currentPartition);
            }

            return partitionedRanges;
        }
    }
}
