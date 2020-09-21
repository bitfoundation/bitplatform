using Bit.Core.Contracts;
using Bit.Owin.Contracts;
using NodaTime;
using System;

namespace Bit.Owin.Implementations
{
    public class DefaultTimeZoneManager : ITimeZoneManager
    {
        private string? _currentTimeZoneName;
        private string? _desiredTimeZoneName;

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

        protected virtual TimeZoneInfo? GetTimeZoneInfoByName(string? timeZoneName, DateTimeOffset dateTimeOffset)
        {
            if (timeZoneName == null)
                return null;

            DateTimeZone? nodaTimeZone = DateTimeZoneProviders.Tzdb.GetZoneOrNull(timeZoneName) ?? DateTimeZoneProviders.Bcl.GetZoneOrNull(timeZoneName);

            if (nodaTimeZone != null)
            {
                var nodaOffset = nodaTimeZone.GetUtcOffset(OffsetDateTime.FromDateTimeOffset(dateTimeOffset).ToInstant());

                return TimeZoneInfo.CreateCustomTimeZone(nodaTimeZone.Id, TimeSpan.FromTicks(nodaOffset.Ticks), timeZoneName, timeZoneName);
            }

            return null;
        }

        public virtual TimeZoneInfo? GetClientCurrentTimeZone(DateTimeOffset dateTimeOffset)
        {
            return GetTimeZoneInfoByName(_currentTimeZoneName, dateTimeOffset);
        }

        public virtual TimeZoneInfo? GetClientDesiredTimeZone(DateTimeOffset dateTimeOffset)
        {
            return GetTimeZoneInfoByName(_desiredTimeZoneName, dateTimeOffset);
        }

        public virtual DateTimeOffset MapFromClientToServer(DateTimeOffset dateTime)
        {
            if (_currentTimeZoneName == null || _desiredTimeZoneName == null)
                return dateTime;

            if (dateTime == DateTimeOffset.MinValue)
                return dateTime;

            if (_currentTimeZoneName == _desiredTimeZoneName)
                return dateTime;

            TimeZoneInfo currentTimeZoneInfo = GetClientCurrentTimeZone(dateTime)!;

            TimeZoneInfo desiredTimeZoneInfo = GetClientDesiredTimeZone(dateTime)!;

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

            TimeZoneInfo currentTimeZoneInfo = GetClientCurrentTimeZone(dateTime)!;

            TimeZoneInfo desiredTimeZoneInfo = GetClientDesiredTimeZone(dateTime)!;

            if (currentTimeZoneInfo.HasSameRules(desiredTimeZoneInfo))
                return dateTime;
            else
            {
                return dateTime - (currentTimeZoneInfo.BaseUtcOffset - desiredTimeZoneInfo.BaseUtcOffset);
            }
        }
    }
}
