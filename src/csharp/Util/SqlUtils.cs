using System.Globalization;

namespace RvSfDownloadCore.Util
{
    public static class SqUtils
    {
        static CultureInfo _enUs = new CultureInfo("en-US");
        public static string RussianDate2MsSqlDate(this string sDate) =>
            sDate.ParseRussianDate().Date2MsSqlDate();

        static string Date2MsSqlDate(this DateTime date) => date.ToString("yyyyMMdd HH:mm:ss");


        // "24.04.2020 14:49:03"
        static DateTime ParseRussianDate(this string sDate) =>
            DateTime.ParseExact(sDate, "dd.MM.yyyy HH:mm:ss", _enUs, DateTimeStyles.None);
    }
}
