using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZingPDF.Syntax.Encryption;

internal class StandardSecurityHandler : ISecurityHandler
{
    public byte[] Decrypt(byte[] data)
    {
        throw new NotImplementedException();
    }

    public byte[] Encrypt(byte[] data)
    {
        throw new NotImplementedException();
    }
}
