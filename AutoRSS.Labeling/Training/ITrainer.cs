using AutoRSS.Library.Correlation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AutoRSS.Labeling.Training
{
    internal interface ITrainer
    {
        IEnumerable<string> Filter(ProgramArguments programArgs);
        CorrelationMatrix CalculateCorrelationMatrix(IEnumerable<string> documents);
        CorrelationMatrix UpdateCorrelationMatrix(CorrelationMatrix existingMatrix, IEnumerable<string> documents);
    }
}
