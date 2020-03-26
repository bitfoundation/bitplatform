using System;
using System.Collections.Concurrent;
using System.Linq;
using Bit.Core.Contracts;
using Bit.Owin.Contracts;
using TimeZoneConverter;

namespace Bit.Owin.Implementations
{
    public class DefaultTimeZoneManager : ITimeZoneManager
    {
        private static readonly ConcurrentDictionary<string, TimeZoneInfo> _timeZonesCache = new ConcurrentDictionary<string, TimeZoneInfo>();
        private string _currentTimeZoneName;
        private string _desiredTimeZoneName;

        public virtual IRequestInformationProvider RequestInformationProvider
        {
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(RequestInformationProvider));

                _currentTimeZoneName = value.CurrentTimeZone;
                if (_currentTimeZoneName == "British Summer Time")
                    _currentTimeZoneName = "GMT Daylight Time";

                _desiredTimeZoneName = value.DesiredTimeZone;
                if (_desiredTimeZoneName == "British Summer Time")
                    _desiredTimeZoneName = "GMT Daylight Time";
            }
        }

        protected virtual TimeZoneInfo GetTimeZoneInfoByName(string timeZoneName)
        {
            TimeZoneInfo timeZoneInfo = null;

            if (!_timeZonesCache.ContainsKey(timeZoneName))
            {
                timeZoneInfo = TimeZoneInfo.GetSystemTimeZones().ExtendedSingleOrDefault($"Finding {timeZoneName} in {nameof(TimeZoneInfo)}.{nameof(TimeZoneInfo.GetSystemTimeZones)}",
                    t => t.Id == timeZoneName || t.StandardName == timeZoneName || t.DaylightName == timeZoneName || t.DisplayName == timeZoneName);

                if (timeZoneInfo == null)
                    TZConvert.TryGetTimeZoneInfo(timeZoneName, out timeZoneInfo);

                _timeZonesCache.TryAdd(timeZoneName, timeZoneInfo ?? throw new InvalidOperationException($"Time zone {timeZoneName} could not be found"));
            }
            else
            {
                timeZoneInfo = _timeZonesCache[timeZoneName];
            }

            return timeZoneInfo;
        }

        public virtual TimeZoneInfo GetClientCurrentTimeZone()
        {
            return GetTimeZoneInfoByName(_currentTimeZoneName);
        }

        public virtual TimeZoneInfo GetClientDesiredTimeZone()
        {
            return GetTimeZoneInfoByName(_desiredTimeZoneName);
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
