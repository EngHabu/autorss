using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoRSS.Library.Serialization
{
    interface ISerializer<T>
    {
        T Deserialize(Stream inputStream);
        void Serialize(Stream outputStream, T graph);
    }
}
