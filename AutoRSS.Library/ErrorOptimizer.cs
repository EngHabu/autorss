using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoRSS.Library
{
    public class ErrorOptimizer
    {
        public static double? Optimize(double start, double end, Func<double, bool> goalFunction)
        {
            if (start == end)
            {
                return null;
            }

            double mid = (start + end) / 2;
            bool goalReached = goalFunction(mid);
            if (goalReached)
            {
                return mid;
            }

            double? result = Optimize(start, mid, goalFunction);
            if (null == result)
            {
                result = Optimize(mid, end, goalFunction);
            }

            return result;
        }

        public static double? Optimize(double start1, double end1, double start2, double end2, Func<double, double, bool> goalFunction)
        {
            double mid1 = (start1 + end1) / 2;
            double mid2 = (start2 + end2) / 2;
            double? result = Optimize(start1, end1, (d1) => goalFunction(d1, mid2));
            if (null != result)
            {
                return result;
            }
            else if (null != (result = Optimize(start2, end2, (d2) => goalFunction(mid1, d2))))
            {
                return result;
            }
            else
            {
                return null;
            }
        }
    }
}
