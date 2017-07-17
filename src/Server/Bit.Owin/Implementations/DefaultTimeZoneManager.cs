using System;
using System.Collections.Concurrent;
using System.Linq;
using Bit.Core.Contracts;
using Bit.Owin.Contracts;

namespace Bit.Owin.Implementations
{
    public class DefaultTimeZoneManager : ITimeZoneManager
    {
        private static readonly ConcurrentDictionary<string, TimeZoneInfo> _timeZonesCache = new ConcurrentDictionary<string, TimeZoneInfo>();
        private readonly string _currentTimeZoneName;
        private readonly string _desiredTimeZoneName;

        public DefaultTimeZoneManager(IRequestInformationProvider requestInformationProvider)
        {
            if (requestInformationProvider == null)
                throw new ArgumentNullException(nameof(requestInformationProvider));

            _currentTimeZoneName = requestInformationProvider.CurrentTimeZone;

            _desiredTimeZoneName = requestInformationProvider.DesiredTimeZone;
        }

#if DEBUG
        protected DefaultTimeZoneManager()
        {
        }
#endif

        public virtual TimeZoneInfo GetClientCurrentTimeZone()
        {
            if (!_timeZonesCache.ContainsKey(_currentTimeZoneName))
            {
                lock (_timeZonesCache)
                {
                    if (!_timeZonesCache.ContainsKey(_currentTimeZoneName))
                    {
                        _timeZonesCache.TryAdd(_currentTimeZoneName,
                            TimeZoneInfo.GetSystemTimeZones().Single(t => t.Id == _currentTimeZoneName || t.StandardName == _currentTimeZoneName || t.DaylightName == _currentTimeZoneName || t.DaylightName == _currentTimeZoneName));
                    }
                }
            }

            TimeZoneInfo currentTimeZoneInfo = _timeZonesCache[_currentTimeZoneName];

            return currentTimeZoneInfo;
        }

        public virtual TimeZoneInfo GetClientDesiredTimeZone()
        {
            if (!_timeZonesCache.ContainsKey(_desiredTimeZoneName))
            {
                lock (_timeZonesCache)
                {
                    if (!_timeZonesCache.ContainsKey(_desiredTimeZoneName))
                    {
                        _timeZonesCache.TryAdd(_desiredTimeZoneName,
                        TimeZoneInfo.GetSystemTimeZones().Single(t => t.Id == _desiredTimeZoneName || t.StandardName == _desiredTimeZoneName || t.DaylightName == _desiredTimeZoneName || t.DaylightName == _desiredTimeZoneName));
                    }
                }
            }

            TimeZoneInfo desiredTimeZoneInfo = _timeZonesCache[_desiredTimeZoneName];

            return desiredTimeZoneInfo;
        }

        public virtual DateTimeOffset MapFromClientToServer(DateTimeOffset dateTime)
        {
            if (_currentTimeZoneName == null || _desiredTimeZoneName == null)
                return dateTime;

            if (dateTime == DateTimeOffset.MinValue)
                return dateTime;

            if (_currentTimeZoneName == _desiredTimeZoneName)
                return dateTime;

            TimeZoneInfo currentTimeZoneInfo = GetClientCurrentTimeZone();

            TimeZoneInfo desiredTimeZoneInfo = GetClientDesiredTimeZone();

            if (currentTimeZoneInfo.HasSameRules(desiredTimeZoneInfo))
                return dateTime;
            else
            {
                return dateTime + (currentTimeZoneInfo.BaseUtcOffset - desiredTimeZoneInfo.BaseUtcOffset);
            }
        }

        public virtual DateTimeOffset MapFromServerToClient(DateTimeOffset dateTime)
        {
            if (_currentTimeZoneName == null || _desiredTimeZoneName == null)
                return dateTime;

            if (dateTime == DateTimeOffset.MinValue)
                return dateTime;

            if (_currentTimeZoneName == _desiredTimeZoneName)
                return dateTime;

            TimeZoneInfo currentTimeZoneInfo = GetClientCurrentTimeZone();

            TimeZoneInfo desiredTimeZoneInfo = GetClientDesiredTimeZone();

            if (currentTimeZoneInfo.HasSameRules(desiredTimeZoneInfo))
                return dateTime;
            else
            {
                return dateTime - (currentTimeZoneInfo.BaseUtcOffset - desiredTimeZoneInfo.BaseUtcOffset);
            }
        }
    }
}
