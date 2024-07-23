namespace ZingPDF.Syntax.Filters.FilterUtils;

internal static class PngPredictor
{
    public enum PredictorType
    {
        None = 0,
        Sub = 1,
        Up = 2,
        Average = 3,
        Paeth = 4
    }

    public static byte[] Encode(byte[] inputBytes, PredictorType predictor, int colors, int bitsPerComponent, int columns)
    {
        byte[] outputBytes = predictor switch
        {
            PredictorType.None => EncodeNone(inputBytes),
            PredictorType.Sub => EncodeSub(inputBytes, colors, bitsPerComponent, columns),
            PredictorType.Up => EncodeUp(inputBytes, columns),
            PredictorType.Average => EncodeAverage(inputBytes, columns),
            PredictorType.Paeth => EncodePaeth(inputBytes, columns),
            _ => throw new ArgumentException("Unsupported predictor type."),
        };

        return outputBytes;
    }

    public static byte[] Decode(byte[] inputBytes, int predictor, int colors, int bitsPerComponent, int columns)
    {
        ArgumentNullException.ThrowIfNull(inputBytes);

        if (predictor == 1)
        {
            return inputBytes;
        }

        int bitsPerPixel = colors * bitsPerComponent;
        int bytesPerPixel = (bitsPerPixel + 7) / 8;
        int rowlength = (columns * bitsPerPixel + 7) / 8;
        byte[] actline = new byte[rowlength];
        byte[] lastline = new byte[rowlength];

        int linepredictor = predictor;

        var result = new List<byte>();

        using var memoryStream = new MemoryStream();
        using var output = new BinaryWriter(memoryStream);
        using var input = new RandomAccessBuffer(inputBytes);

        while (input.Available() > 0)
        {
            // test for PNG predictor; each value >= 10 (not only 15) indicates usage of PNG predictor
            if (predictor >= 10)
            {
                // PNG predictor; each row starts with predictor type (0, 1, 2, 3, 4)
                // read per line predictor
                linepredictor = input.Read();
                if (linepredictor == -1)
                {
                    return [.. result];
                }
                // add 10 to tread value 0 as 10, 1 as 11, ...
                linepredictor += 10;
            }

            // read line
            int i;
            int offset = 0;
            while (offset < rowlength && (i = input.Read(actline, offset, rowlength - offset)) != -1)
            {
                if (i == 0)
                {
                    // TODO: #291, this indicates a bug in reading logic.
                    // This only avoids the infinite loop it does not fix the logic bug.
                    break;
                }

                offset += i;
            }

            // do prediction as specified input PNG-Specification 1.2
            switch (linepredictor)
            {
                case 2:
                    // PRED TIFF SUB
                    if (bitsPerComponent == 8)
                    {
                        // for 8 bits per component it is the same algorithm as PRED SUB of PNG format
                        for (int p = bytesPerPixel; p < rowlength; p++)
                        {
                            int sub = actline[p] & 0xff;
                            int left = actline[p - bytesPerPixel] & 0xff;
                            actline[p] = (byte)(sub + left);
                        }
                        break;
                    }
                    if (bitsPerComponent == 16)
                    {
                        for (int p = bytesPerPixel; p < rowlength; p += 2)
                        {
                            int sub = ((actline[p] & 0xff) << 8) + (actline[p + 1] & 0xff);
                            int left = ((actline[p - bytesPerPixel] & 0xff) << 8)
                                        + (actline[p - bytesPerPixel + 1] & 0xff);
                            actline[p] = (byte)(sub + left >> 8 & 0xff);
                            actline[p + 1] = (byte)(sub + left & 0xff);
                        }
                        break;
                    }
                    if (bitsPerComponent == 1 && colors == 1)
                    {
                        // bytesPerPixel cannot be used:
                        // "A row shall occupy a whole number of bytes, rounded up if necessary.
                        // Samples and their components shall be packed into bytes 
                        // from high-order to low-order bits."
                        for (int p = 0; p < rowlength; p++)
                        {
                            for (int bit = 7; bit >= 0; --bit)
                            {
                                int sub = actline[p] >> bit & 1;
                                if (p == 0 && bit == 7)
                                {
                                    continue;
                                }
                                int left;
                                if (bit == 7)
                                {
                                    // use bit #0 from previous byte
                                    left = actline[p - 1] & 1;
                                }
                                else
                                {
                                    // use "previous" bit
                                    left = actline[p] >> bit + 1 & 1;
                                }
                                if ((sub + left & 1) == 0)
                                {
                                    // reset bit
                                    actline[p] = (byte)(actline[p] & ~(1 << bit));
                                }
                                else
                                {
                                    // set bit
                                    actline[p] = (byte)(actline[p] | 1 << bit);
                                }
                            }
                        }
                        break;
                    }
                    // everything else, i.e. bpc 2 and 4, but has been tested for bpc 1 and 8 too
                    int elements = columns * colors;
                    for (int p = colors; p < elements; ++p)
                    {
                        int bytePosSub = p * bitsPerComponent / 8;
                        int bitPosSub = 8 - p * bitsPerComponent % 8 - bitsPerComponent;
                        int bytePosLeft = (p - colors) * bitsPerComponent / 8;
                        int bitPosLeft = 8 - (p - colors) * bitsPerComponent % 8 - bitsPerComponent;

                        int sub = GetBitSeq(actline[bytePosSub], bitPosSub, bitsPerComponent);
                        int left = GetBitSeq(actline[bytePosLeft], bitPosLeft, bitsPerComponent);
                        actline[bytePosSub] = (byte)CalcSetBitSeq(actline[bytePosSub], bitPosSub,
                            bitsPerComponent,
                            sub + left);
                    }
                    break;
                case 10:
                    // PRED NONE
                    // do nothing
                    break;
                case 11:
                    // PRED SUB
                    for (int p = bytesPerPixel; p < rowlength; p++)
                    {
                        int sub = actline[p];
                        int left = actline[p - bytesPerPixel];
                        actline[p] = (byte)(sub + left);
                    }
                    break;
                case 12:
                    // PRED UP
                    for (int p = 0; p < rowlength; p++)
                    {
                        int up = actline[p] & 0xff;
                        int prior = lastline[p] & 0xff;
                        actline[p] = (byte)(up + prior & 0xff);
                    }
                    break;
                case 13:
                    // PRED AVG
                    for (int p = 0; p < rowlength; p++)
                    {
                        int avg = actline[p] & 0xff;
                        int left = p - bytesPerPixel >= 0 ? actline[p - bytesPerPixel] & 0xff : 0;
                        int up = lastline[p] & 0xff;
                        actline[p] = (byte)(avg + (left + up) / 2 & 0xff);
                    }
                    break;
                case 14:
                    // PRED PAETH
                    for (int p = 0; p < rowlength; p++)
                    {
                        int paeth = actline[p] & 0xff;
                        int a = p - bytesPerPixel >= 0 ? actline[p - bytesPerPixel] & 0xff : 0; // left
                        int b = lastline[p] & 0xff; // upper
                        int c = p - bytesPerPixel >= 0 ? lastline[p - bytesPerPixel] & 0xff : 0; // upperleft
                        int value = a + b - c;
                        int absa = Math.Abs(value - a);
                        int absb = Math.Abs(value - b);
                        int absc = Math.Abs(value - c);

                        if (absa <= absb && absa <= absc)
                        {
                            actline[p] = (byte)(paeth + a & 0xff);
                        }
                        else if (absb <= absc)
                        {
                            actline[p] = (byte)(paeth + b & 0xff);
                        }
                        else
                        {
                            actline[p] = (byte)(paeth + c & 0xff);
                        }
                    }

                    break;
            }
            Array.Copy(actline, 0, lastline, 0, rowlength);
            output.Write(actline);
        }

        return memoryStream.ToArray();
    }

    /// <summary>
    /// GetLongOrDefault value from bit interval from a byte
    /// </summary>
    private static int GetBitSeq(int by, int startBit, int bitSize)
    {
        int mask = (1 << bitSize) - 1;
        return by >> startBit & mask;
    }

    /// <summary>
    /// Set value input a bit interval and return that value
    /// </summary>
    private static int CalcSetBitSeq(int by, int startBit, int bitSize, int val)
    {
        int mask = (1 << bitSize) - 1;
        int truncatedVal = val & mask;
        mask = ~(mask << startBit);
        return by & mask | truncatedVal << startBit;
    }

    private static byte[] EncodeNone(byte[] inputBytes)
    {
        // No encoding needed, simply return input bytes
        return inputBytes;
    }

    private static byte[] EncodeSub(byte[] inputBytes, int colors, int bitsPerComponent, int columns)
    {
        // Sub encoding: each byte is replaced by the difference between it and the byte left to it.
        byte[] outputBytes = new byte[inputBytes.Length];
        for (int i = 0; i < inputBytes.Length; i++)
        {
            if (i % columns == 0)
            {
                outputBytes[i] = inputBytes[i];
            }
            else
            {
                outputBytes[i] = (byte)(inputBytes[i] - inputBytes[i - 1]);
            }
        }
        return outputBytes;
    }

    private static byte[] EncodeUp(byte[] inputBytes, int columns)
    {
        // Up encoding: each byte is replaced by the difference between it and the byte above it.
        byte[] outputBytes = new byte[inputBytes.Length];
        for (int i = 0; i < inputBytes.Length; i++)
        {
            if (i < columns)
            {
                outputBytes[i] = inputBytes[i];
            }
            else
            {
                outputBytes[i] = (byte)(inputBytes[i] - inputBytes[i - columns]);
            }
        }
        return outputBytes;
    }

    private static byte[] EncodeAverage(byte[] inputBytes, int columns)
    {
        // Average encoding: each byte is replaced by the difference between it and the average of the byte left to it and the byte above it.
        byte[] outputBytes = new byte[inputBytes.Length];
        for (int i = 0; i < inputBytes.Length; i++)
        {
            if (i % columns == 0)
            {
                outputBytes[i] = inputBytes[i];
            }
            else
            {
                byte left = i >= 1 ? outputBytes[i - 1] : (byte)0;
                byte above = i >= columns ? outputBytes[i - columns] : (byte)0;
                byte average = (byte)((left + above) / 2);
                outputBytes[i] = (byte)(inputBytes[i] - average);
            }
        }
        return outputBytes;
    }

    private static byte[] EncodePaeth(byte[] inputBytes, int columns)
    {
        // Paeth encoding: each byte is replaced by the difference between it and a prediction made by the Paeth predictor.
        byte[] outputBytes = new byte[inputBytes.Length];
        for (int i = 0; i < inputBytes.Length; i++)
        {
            if (i % columns == 0)
            {
                outputBytes[i] = inputBytes[i];
            }
            else
            {
                byte left = i >= 1 ? outputBytes[i - 1] : (byte)0;
                byte above = i >= columns ? outputBytes[i - columns] : (byte)0;
                byte upperLeft = i >= columns + 1 ? outputBytes[i - columns - 1] : (byte)0;

                int p = left + above - upperLeft;
                int pa = Math.Abs(p - left);
                int pb = Math.Abs(p - above);
                int pc = Math.Abs(p - upperLeft);

                byte prediction;
                if (pa <= pb && pa <= pc) prediction = left;
                else if (pb <= pc) prediction = above;
                else prediction = upperLeft;

                outputBytes[i] = (byte)(inputBytes[i] - prediction);
            }
        }
        return outputBytes;
    }
}
