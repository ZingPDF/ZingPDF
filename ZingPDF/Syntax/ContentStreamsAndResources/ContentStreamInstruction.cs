using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZingPDF.Syntax.ContentStreamsAndResources
{
    internal class ContentStreamInstruction : PdfObject
    {
        protected override Task WriteOutputAsync(Stream stream)
        {
            throw new NotImplementedException();
        }
    }
}
