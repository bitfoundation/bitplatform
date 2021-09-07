namespace Bit.Client.Web.BlazorUI
{
    public class BitDate
    {
        private int year;
        private int month;
        private int day;
        private int dayOfWeek;

        public BitDate(int year, int month, int day)
        {
            this.year = year;
            this.month = month;
            this.day = day;
        }

        public BitDate(int year, int month, int day, int dayOfWeek)
        {
            this.year = year;
            this.month = month;
            this.day = day;
            this.dayOfWeek = dayOfWeek;
        }

        public int GetDate() => day;

        public int GetMonth() => month;

        public int GetYear() => year;

        public int GetDayOfWeek() => dayOfWeek;
    }
}
