using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bit.View
{
    public class CultureInfoProvider
    {
        private readonly Dictionary<string, CultureInfo> _CultureInfoCache;

        public CultureInfoProvider()
        {
            CultureInfo Fa = new CultureInfo("Fa");

            Fa.DateTimeFormat.MonthNames = Fa.DateTimeFormat.AbbreviatedMonthGenitiveNames = Fa.DateTimeFormat.MonthGenitiveNames = Fa.DateTimeFormat.AbbreviatedMonthNames = new[] { "فروردین", "اردیبهشت", "خرداد", "تیر", "مرداد", "شهریور", "مهر", "آبان", "آذر", "دی", "بهمن", "اسفند", "" };

            CultureInfo Ar = new CultureInfo("Ar");

            Ar.DateTimeFormat.DayNames = Ar.DateTimeFormat.AbbreviatedDayNames = Ar.DateTimeFormat.ShortestDayNames = new[] { "الأحد", "الإثنين", "الثلاثاء", "الأربعاء", "الخميس", "الجمعة", "السبت" };

            Ar.DateTimeFormat.YearMonthPattern = Fa.DateTimeFormat.YearMonthPattern = "MMMM yyyy";

            _CultureInfoCache = new Dictionary<string, CultureInfo>
            {
                { "Fa", Fa },
                { "Ar", Ar }
            };
        }

        public virtual CultureInfo GetCultureInfo(string cultureName)
        {
            return _CultureInfoCache.ContainsKey(cultureName) ? _CultureInfoCache[cultureName] : CultureInfo.GetCultureInfo(cultureName);
        }

        private static CultureInfoProvider _Current;

        public static CultureInfoProvider Current
        {
            get
            {
                _Current = _Current ?? new CultureInfoProvider { };
                return _Current;
            }
            set
            {
                if (_Current != null)
                    throw new InvalidOperationException($"{nameof(Current)} {nameof(CultureInfoProvider)} has been already set.");

                _Current = value ?? throw new InvalidOperationException($"{nameof(Current)} {nameof(CultureInfoProvider)} may not be null.");
            }
        }
    }
}
