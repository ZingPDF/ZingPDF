using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZingPdf.Core.Objects.Primitives;

namespace ZingPdf.Core.Objects.Filters
{
    internal class RunLengthDecodeFilter : IFilter
    {
        public Name Name => Constants.Filters.RunLength;
        public Dictionary? Params => null;

        public byte[] Decode(byte[] data)
        {
            throw new NotImplementedException();
        }

        public byte[] Encode(byte[] data)
        {
            
            throw new NotImplementedException();
        }
    }
}
