using System.Text;
using ZingPDF.ObjectModel.Objects;

namespace ZingPDF.ObjectModel.Filters
{
    /// <summary>
    /// ISO 32000-2:2020 7.4.3
    /// 
    /// ASCII85Decode
    /// 
    /// Represents data using ASCII base-85 encoding.
    /// </summary>
    internal class ASCII85DecodeFilter : IFilter
    {
        /// <summary>
        /// Maximum line length for encoded ASCII85 string; 
        /// set to zero for one unbroken line.
        /// </summary>
        public int LineLength = 75;

        /// <summary>
        /// Add the Suffix mark when encoding, and enforce its presence for decoding
        /// </summary>
        public bool EnforceSuffixMark = true;

        private const int _asciiOffset = 33;
        private readonly byte[] _encodedBlock = new byte[5];
        private readonly byte[] _decodedBlock = new byte[4];
        private uint _tuple = 0;
        private int _linePos = 0;


        private readonly uint[] _pow85 = { 85 * 85 * 85 * 85, 85 * 85 * 85, 85 * 85, 85, 1 };

        private readonly byte[] _endOfDataMarker = Encoding.ASCII.GetBytes("~>");

        public Name Name => Constants.Filters.ASCII85;
        public Dictionary? Params => null;

        public byte[] Decode(byte[] data)
        {
            if (data is null) throw new ArgumentNullException(nameof(data));
            if (EnforceSuffixMark && !data.Skip(data.Length - 2).SequenceEqual(_endOfDataMarker)) throw new FilterInputFormatException(nameof(data), $"'{nameof(data)}' must end with the EOD marker: {_endOfDataMarker}.");

            data = data[..^_endOfDataMarker.Length];

            var ms = new MemoryStream();
            int count = 0;
            foreach (char c in data)
            {
                bool processChar;
                switch (c)
                {
                    case 'z':
                        if (count != 0)
                        {
                            throw new FilterInputFormatException(nameof(data), "The character 'z' is invalid inside an ASCII85 block.");
                        }
                        _decodedBlock[0] = 0;
                        _decodedBlock[1] = 0;
                        _decodedBlock[2] = 0;
                        _decodedBlock[3] = 0;
                        ms.Write(_decodedBlock, 0, _decodedBlock.Length);
                        processChar = false;
                        break;
                    case '\n':
                    case '\r':
                    case '\t':
                    case '\0':
                    case '\f':
                    case '\b':
                    case ' ':
                        processChar = false;
                        break;
                    default:
                        if (c < '!' || c > 'u')
                        {
                            throw new FilterInputFormatException(nameof(data), "Bad character '" + c + "' found. ASCII85 only allows characters '!' to 'u'.");
                        }
                        processChar = true;
                        break;
                }

                if (processChar)
                {
                    _tuple += (uint)(c - _asciiOffset) * _pow85[count];
                    count++;
                    if (count == _encodedBlock.Length)
                    {
                        DecodeBlock();
                        ms.Write(_decodedBlock, 0, _decodedBlock.Length);
                        _tuple = 0;
                        count = 0;
                    }
                }
            }

            // if we have some bytes left over at the end..
            if (count != 0)
            {
                if (count == 1)
                {
                    throw new FilterInputFormatException(nameof(data), "The last block of ASCII85 data cannot be a single byte.");
                }
                count--;
                _tuple += _pow85[count];
                DecodeBlock(count);
                for (int i = 0; i < count; i++)
                {
                    ms.WriteByte(_decodedBlock[i]);
                }
            }

            return ms.ToArray();
        }

        public byte[] Encode(byte[] data)
        {
            if (data is null) throw new ArgumentNullException(nameof(data));

            var sb = new StringBuilder(data.Length * (_encodedBlock.Length / _decodedBlock.Length));
            _linePos = 0;

            int count = 0;
            _tuple = 0;
            foreach (byte b in data)
            {
                if (count >= _decodedBlock.Length - 1)
                {
                    _tuple |= b;
                    if (_tuple == 0)
                    {
                        AppendChar(sb, 'z');
                    }
                    else
                    {
                        EncodeBlock(sb);
                    }
                    _tuple = 0;
                    count = 0;
                }
                else
                {
                    _tuple |= (uint)(b << 24 - count * 8);
                    count++;
                }
            }

            // if we have some bytes left over at the end..
            if (count > 0)
            {
                EncodeBlock(count + 1, sb);
            }

            if (EnforceSuffixMark)
            {
                AppendString(sb, Encoding.ASCII.GetString(_endOfDataMarker));
            }

            return Encoding.ASCII.GetBytes(sb.ToString());
        }

        private void EncodeBlock(StringBuilder sb)
        {
            EncodeBlock(_encodedBlock.Length, sb);
        }

        private void EncodeBlock(int count, StringBuilder sb)
        {
            for (int i = _encodedBlock.Length - 1; i >= 0; i--)
            {
                _encodedBlock[i] = (byte)(_tuple % 85 + _asciiOffset);
                _tuple /= 85;
            }

            for (int i = 0; i < count; i++)
            {
                char c = (char)_encodedBlock[i];
                AppendChar(sb, c);
            }

        }

        private void DecodeBlock()
        {
            DecodeBlock(_decodedBlock.Length);
        }

        private void DecodeBlock(int bytes)
        {
            for (int i = 0; i < bytes; i++)
            {
                _decodedBlock[i] = (byte)(_tuple >> 24 - i * 8);
            }
        }

        private void AppendString(StringBuilder sb, string s)
        {
            if (LineLength > 0 && _linePos + s.Length > LineLength)
            {
                _linePos = 0;
                sb.Append('\n');
            }
            else
            {
                _linePos += s.Length;
            }
            sb.Append(s);
        }

        private void AppendChar(StringBuilder sb, char c)
        {
            sb.Append(c);
            _linePos++;
            if (LineLength > 0 && _linePos >= LineLength)
            {
                _linePos = 0;
                sb.Append('\n');
            }
        }
    }
}
