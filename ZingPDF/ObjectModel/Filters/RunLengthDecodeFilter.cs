using ZingPDF.ObjectModel.Objects;

namespace ZingPDF.ObjectModel.Filters
{
    /// <summary>
    /// ISO 32000-2:2020 7.4.5
    /// 
    /// RunLengthDecode
    /// 
    /// The RunLengthDecode filter decodes data that has been encoded in a simple byte-oriented format based on run length.
    /// </summary>
    /// <remarks>
    /// The encoded data shall be a sequence of runs, where each run shall consist of a length byte followed by 1 to 128 bytes of data.
    /// If the length byte is in the range 0 to 127, the following length + 1 (1 to 128) bytes shall be copied literally during decompression.
    /// If length is in the range 129 to 255, the following single byte shall be copied 257 - length (2 to 128) times during decompression.
    /// A length value of 128 shall denote EOD.
    /// </remarks>
    internal class RunLengthDecodeFilter : IFilter
    {
        public Name Name => Constants.Filters.RunLength;
        public Dictionary? Params => null;

        public byte[] Decode(byte[] data)
        {
            List<byte> decompressedData = new();
            int i = 0;

            while (i < data.Length)
            {
                byte lengthByte = data[i];
                i++;

                if (lengthByte < 128)
                {
                    // Run consists of length + 1 bytes to be copied literally
                    int runLength = lengthByte + 1;
                    for (int j = 0; j < runLength; j++)
                    {
                        decompressedData.Add(data[i]);
                        i++;
                    }
                }
                else if (lengthByte > 128)
                {
                    // Run consists of a single byte to be copied multiple times
                    int runLength = 257 - lengthByte;
                    byte value = data[i];
                    for (int j = 0; j < runLength; j++)
                    {
                        decompressedData.Add(value);
                    }
                    i++;
                }
                else
                {
                    // End of Data marker (lengthByte == 128)
                    break;
                }
            }

            return decompressedData.ToArray();
        }

        public byte[] Encode(byte[] data)
        {
            List<byte> compressedData = new();

            int i = 0;

            while (i < data.Length)
            {
                byte currentByte = data[i];
                int runLength = 1;

                // Count the length of the run
                while (i + runLength < data.Length && data[i + runLength] == currentByte && runLength < 128)
                {
                    runLength++;
                }

                // Encode the run and add it to the compressed data
                if (runLength > 1)
                {
                    compressedData.Add((byte)(257 - runLength));
                    compressedData.Add(currentByte);
                }
                else
                {
                    // Count the length of the run where the next byte is different

                    var countWithinArrayRange = () => i + runLength < data.Length;
                    var countLessThan128 = () => runLength < 128;
                    var nextByteDifferent = () => i + runLength == data.Length - 1 || data[i + runLength] != data[i + runLength + 1];

                    while (
                        countWithinArrayRange()
                        && countLessThan128()
                        && nextByteDifferent()
                        )
                    {
                        runLength++;
                    }

                    compressedData.Add((byte)(runLength - 1));
                    compressedData.AddRange(data[i..(i + runLength)]);
                }

                i += runLength;
            }

            // Add End of Data marker
            compressedData.Add(128);

            return compressedData.ToArray();
        }
    }
}
