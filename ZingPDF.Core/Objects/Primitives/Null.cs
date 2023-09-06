using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZingPdf.Core.Objects.Primitives
{
    internal class Null : PdfObject
    {
        public override Task WriteOutputAsync(Stream stream)
        {
            throw new NotImplementedException();
        }
    }
}
