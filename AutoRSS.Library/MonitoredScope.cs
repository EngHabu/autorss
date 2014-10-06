using AutoRSS.Library.Serialization;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace AutoRSS.Library
{
    public class MonitoredScope : IDisposable
    {
        private static ConcurrentDictionary<string, MonitoredScopeMetadata> _statistics = new ConcurrentDictionary<string, MonitoredScopeMetadata>();
        private Stopwatch _watch;
        private TraceLevel _traceLevel;

        public MonitoredScope(string name)
            : this(name, TraceLevel.Verbose)
        {
        }

        public MonitoredScope(string name, TraceLevel traceLevel)
        {
            Name = name;
            EnsureMetadata(name);
            _watch = new Stopwatch();
            _watch.Start();
            _traceLevel = traceLevel;
        }

        private void EnsureMetadata(string name)
        {
            _statistics.GetOrAdd(name, new MonitoredScopeMetadata());
        }

        public string Name { get; private set; }
        public long ElapsedMilliseconds
        {
            get
            {
                return _watch.ElapsedMilliseconds;
            }
        }

        public void Dispose()
        {
            _watch.Stop();
            Logger.Log("Leaving " + Name + " Time: " + _watch.ElapsedMilliseconds.ToString(), _traceLevel);
            _statistics.AddOrUpdate(Name, new MonitoredScopeMetadata(), (key, scope) =>
            {
                scope.Add(_watch.ElapsedMilliseconds);
                return scope;
            });
        }

        public static void SerializeStatistics(Stream stream)
        {
            if (null == stream)
            {
                throw new ArgumentNullException("stream");
            }

            MonitoredScopeStatisticsBinarySerializer serializer = new MonitoredScopeStatisticsBinarySerializer();
            serializer.Serialize(stream, _statistics);
        }

        public static void DeserializeStatistics(Stream inputStream)
        {
            if (null == inputStream)
            {
                throw new ArgumentNullException("inputStream");
            }

            MonitoredScopeStatisticsBinarySerializer serializer = new MonitoredScopeStatisticsBinarySerializer();
            _statistics = serializer.Deserialize(inputStream);
        }
    }

    [Serializable]
    public class MonitoredScopeMetadata
    {
        private double _totalMilliseconds;
        private double _count;

        public MonitoredScopeMetadata()
        {
        }

        public double TotalMilliseconds
        {
            get
            {
                return _totalMilliseconds;
            }
            internal set
            {
                _totalMilliseconds = value;
            }
        }

        public double HitCount
        {
            get
            {
                return _count;
            }
            internal set
            {
                _count = value;
            }
        }

        public long AverageMilliseconds
        {
            get
            {
                if (_count > 0)
                {
                    return (long)(_totalMilliseconds / _count);
                }
                else
                {
                    return 0;
                }
            }
        }

        public void Add(long milliseconds)
        {
            _totalMilliseconds += milliseconds;
            _count++;
        }
    }
}
