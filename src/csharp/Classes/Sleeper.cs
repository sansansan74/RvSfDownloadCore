namespace RvSfDownloadCore.Classes
{
    public class Sleeper
    {
        /// <summary>
        /// Дата и время последнего обращения к загрузчику
        /// </summary>
        static DateTime? lastRequestDate;

        /// <summary>
        /// Сделать задержку
        /// </summary>
        /// <param name="_timeout"></param>
        public static void WaitOne(int _timeout)
        {
            DateTime dt = DateTime.Now;

            if (!lastRequestDate.HasValue)      // Первый раз - выходим
            {
                lastRequestDate = dt;
                return;
            }

            TimeSpan span = dt - lastRequestDate.Value;  // Вычисляем количество секунд между 2 вызовами

            int ms = Convert.ToInt32(span.TotalMilliseconds);   // Количество миллисекунд
            if (ms < _timeout)
            {
                ms = _timeout - ms;
                System.Threading.Thread.Sleep(ms);
            }

            lastRequestDate = DateTime.Now;
        }
    }
}
