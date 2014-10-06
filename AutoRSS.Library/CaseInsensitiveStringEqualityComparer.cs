using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoRSS.Library
{
    [Serializable]
    internal class CaseInsensitiveStringEqualityComparer : IEqualityComparer<string>
    {
        public bool Equals(string x, string y)
        {
            return string.Equals(x, y, StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode(string obj)
        {
            if (!string.IsNullOrEmpty(obj))
            {
                return obj.GetHashCode();
            }
            else
            {
                return base.GetHashCode();
            }
        }
    }
}
