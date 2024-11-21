namespace MamoLib
{
    public class TimeStamp
    {
        /// <summary>
        /// 获取当前时间的Unix时间戳（毫秒）
        /// </summary>
        /// <returns>当前时间的Unix时间戳，以毫秒为单位</returns>
        public static long GetNow()
        {
            DateTime currentTime = DateTime.UtcNow;
            long timestamp = ((DateTimeOffset)currentTime).ToUnixTimeMilliseconds();
            return timestamp;
        }

        /// <summary>
        /// 将时间戳转换为DateTime对象。
        /// </summary>
        /// <param name="timestamp">以毫秒为单位的时间戳。</param>
        /// <returns>转换后的DateTime对象，表示对应的时间戳所代表的日期和时间。</returns>
        public static DateTime Prase(long timestamp)
        {
            // 使用 DateTimeOffset.FromUnixTimeMilliseconds 将时间戳转换为带有时区偏移的日期时间，然后获取其 UTC 时间部分
            DateTime timestampDateTime = DateTimeOffset.FromUnixTimeMilliseconds(timestamp).UtcDateTime;
            return timestampDateTime;
        }

        /// <summary>
        /// 将 1d 转化为TimeSpan
        /// </summary>
        /// <param name="duration">接受d,h,m,s 天，小时，分钟，秒</param>
        /// <returns>转换后的TimeSpan对象，表示对应的时间间隔。</returns>
        public static TimeSpan ParseDuration(string duration)
        {
            if (string.IsNullOrEmpty(duration))
                throw new ArgumentException("Duration string cannot be null or empty.", nameof(duration));

            if (!int.TryParse(duration.Substring(0, duration.Length - 1), out int timeValue))
                throw new ArgumentException("Invalid number in the duration string.", nameof(duration));

            char unit = duration[duration.Length - 1];

            switch (char.ToLower(unit))
            {
                case 'd':
                    return TimeSpan.FromDays(timeValue);

                case 'm':
                    return TimeSpan.FromMinutes(timeValue);

                case 'h':
                    return TimeSpan.FromHours(timeValue);

                case 's':
                    return TimeSpan.FromSeconds(timeValue);

                default:
                    throw new ArgumentException("Invalid time unit in the duration string.", nameof(duration));
            }
        }
    }
}