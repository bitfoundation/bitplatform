using Foundation.Api.Contracts;
using Foundation.Core.Contracts;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Foundation.Api.Implementations
{
    public class DefaultTimeZoneManager : ITimeZoneManager
    {
        private static readonly IDictionary<string, TimeZoneInfo> _timeZonesCache = new ConcurrentDictionary<string, TimeZoneInfo>();
        private readonly string _currentTimeZone;
        private readonly string _desiredTimeZone;

        public DefaultTimeZoneManager(IRequestInformationProvider requestInformationProvider)
        {
            if (requestInformationProvider == null)
                throw new ArgumentNullException(nameof(requestInformationProvider));

            _currentTimeZone = requestInformationProvider.CurrentTimeZone;

            _desiredTimeZone = requestInformationProvider.DesiredTimeZone;
        }

        protected DefaultTimeZoneManager()
        {

        }

        public virtual DateTimeOffset MapFromClientToServer(DateTimeOffset dateTime)
        {
            if (_currentTimeZone == null || _desiredTimeZone == null)
                return dateTime;

            if (dateTime == DateTimeOffset.MinValue)
                return dateTime;

            if (_currentTimeZone == _desiredTimeZone)
                return dateTime;

            if (!_timeZonesCache.ContainsKey(_currentTimeZone))
            {
                lock (_timeZonesCache)
                {
                    if (!_timeZonesCache.ContainsKey(_currentTimeZone))
                    {
                        _timeZonesCache.Add(_currentTimeZone,
                            TimeZoneInfo.GetSystemTimeZones().Single(t => t.Id == _currentTimeZone || t.StandardName == _currentTimeZone || t.DaylightName == _currentTimeZone || t.DaylightName == _currentTimeZone));
                    }
                }
            }

            TimeZoneInfo currentTimeZoneInfo = _timeZonesCache[_currentTimeZone];

            if (!_timeZonesCache.ContainsKey(_desiredTimeZone))
            {
                lock (_timeZonesCache)
                {
                    if (!_timeZonesCache.ContainsKey(_desiredTimeZone))
                    {
                        _timeZonesCache.Add(_desiredTimeZone,
                        TimeZoneInfo.GetSystemTimeZones().Single(t => t.Id == _desiredTimeZone || t.StandardName == _desiredTimeZone || t.DaylightName == _desiredTimeZone || t.DaylightName == _desiredTimeZone));
                    }
                }
            }

            TimeZoneInfo desiredTimeZoneInfo = _timeZonesCache[_desiredTimeZone];

            if (currentTimeZoneInfo.HasSameRules(desiredTimeZoneInfo))
                return dateTime;
            else
            {
                return dateTime + (currentTimeZoneInfo.BaseUtcOffset - desiredTimeZoneInfo.BaseUtcOffset);
            }
        }

        public virtual DateTimeOffset MapFromServerToClient(DateTimeOffset dateTime)
        {
            if (_currentTimeZone == null || _desiredTimeZone == null)
                return dateTime;

            if (dateTime == DateTimeOffset.MinValue)
                return dateTime;

            if (_currentTimeZone == _desiredTimeZone)
                return dateTime;

            if (!_timeZonesCache.ContainsKey(_currentTimeZone))
            {
                _timeZonesCache.Add(_currentTimeZone,
                TimeZoneInfo.GetSystemTimeZones()
                    .Single(t => t.Id == _currentTimeZone || t.StandardName == _currentTimeZone || t.DaylightName == _currentTimeZone || t.DaylightName == _currentTimeZone));
            }

            TimeZoneInfo currentTimeZoneInfo = _timeZonesCache[_currentTimeZone];

            if (!_timeZonesCache.ContainsKey(_desiredTimeZone))
            {
                _timeZonesCache.Add(_desiredTimeZone,
                TimeZoneInfo.GetSystemTimeZones()
                    .Single(t => t.Id == _desiredTimeZone || t.StandardName == _desiredTimeZone || t.DaylightName == _desiredTimeZone || t.DaylightName == _desiredTimeZone));
            }

            TimeZoneInfo desiredTimeZoneInfo = _timeZonesCache[_desiredTimeZone];

            if (currentTimeZoneInfo.HasSameRules(desiredTimeZoneInfo))
                return dateTime;
            else
            {
                return dateTime - (currentTimeZoneInfo.BaseUtcOffset - desiredTimeZoneInfo.BaseUtcOffset); ;
            }
        }
    }
}
