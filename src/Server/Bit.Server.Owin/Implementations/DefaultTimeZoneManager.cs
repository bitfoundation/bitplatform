using Bit.Core.Contracts;
using Bit.Owin.Contracts;
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

                _desiredTimeZoneName = value.DesiredTimeZone ?? _currentTimeZoneName;
                if (_desiredTimeZoneName == "British Summer Time")
                    _desiredTimeZoneName = "GMT Daylight Time";
            }
        }

        protected virtual TimeZoneInfo? GetTimeZoneInfoByName(string? timeZoneName)
        {
            if (timeZoneName == null)
                return null;

            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneName);
        }

        public virtual TimeZoneInfo? GetClientCurrentTimeZone(DateTimeOffset dateTimeOffset)
        {
            return GetTimeZoneInfoByName(_currentTimeZoneName);
        }

        public virtual TimeZoneInfo? GetClientDesiredTimeZone(DateTimeOffset dateTimeOffset)
        {
            return GetTimeZoneInfoByName(_desiredTimeZoneName);
        }

        public virtual DateTimeOffset MapFromClientToServer(DateTimeOffset dateTimeOffset)
        {
            if (_currentTimeZoneName == null || _desiredTimeZoneName == null)
                return dateTimeOffset;

            if (dateTimeOffset == DateTimeOffset.MinValue || dateTimeOffset == DateTimeOffset.MaxValue)
                return dateTimeOffset;

            if (_currentTimeZoneName == _desiredTimeZoneName)
                return dateTimeOffset;

            TimeZoneInfo currentTimeZoneInfo = GetClientCurrentTimeZone(dateTimeOffset)!;

            TimeZoneInfo desiredTimeZoneInfo = GetClientDesiredTimeZone(dateTimeOffset)!;

            if (currentTimeZoneInfo.HasSameRules(desiredTimeZoneInfo))
                return dateTimeOffset;
            else
            {
                return dateTimeOffset + (currentTimeZoneInfo.BaseUtcOffset - desiredTimeZoneInfo.BaseUtcOffset);
            }
        }

        public virtual DateTimeOffset MapFromServerToClient(DateTimeOffset dateTimeOffset)
        {
            if (_currentTimeZoneName == null || _desiredTimeZoneName == null)
                return dateTimeOffset;

            if (dateTimeOffset == DateTimeOffset.MinValue || dateTimeOffset == DateTimeOffset.MaxValue)
                return dateTimeOffset;

            if (_currentTimeZoneName == _desiredTimeZoneName)
                return dateTimeOffset;

            TimeZoneInfo currentTimeZoneInfo = GetClientCurrentTimeZone(dateTimeOffset)!;

            TimeZoneInfo desiredTimeZoneInfo = GetClientDesiredTimeZone(dateTimeOffset)!;

            if (currentTimeZoneInfo.HasSameRules(desiredTimeZoneInfo))
                return dateTimeOffset;
            else
            {
                return dateTimeOffset - (currentTimeZoneInfo.BaseUtcOffset - desiredTimeZoneInfo.BaseUtcOffset);
            }
        }
    }
}
