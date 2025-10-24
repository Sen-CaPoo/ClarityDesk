using System;

namespace ClarityDesk.Infrastructure.Helpers
{
    /// <summary>
    /// 時區轉換輔助類別
    /// </summary>
    public static class TimeZoneHelper
    {
        /// <summary>
        /// 台北時區資訊
        /// </summary>
        private static readonly TimeZoneInfo TaipeiTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Taipei Standard Time");

        /// <summary>
        /// 將 UTC 時間轉換為台北時間
        /// </summary>
        /// <param name="utcTime">UTC 時間</param>
        /// <returns>台北時間</returns>
        public static DateTime ConvertToTaipeiTime(DateTime utcTime)
        {
            if (utcTime.Kind != DateTimeKind.Utc)
            {
                utcTime = DateTime.SpecifyKind(utcTime, DateTimeKind.Utc);
            }
            
            return TimeZoneInfo.ConvertTimeFromUtc(utcTime, TaipeiTimeZone);
        }

        /// <summary>
        /// 將台北時間轉換為 UTC 時間
        /// </summary>
        /// <param name="taipeiTime">台北時間</param>
        /// <returns>UTC 時間</returns>
        public static DateTime ConvertToUtc(DateTime taipeiTime)
        {
            return TimeZoneInfo.ConvertTimeToUtc(taipeiTime, TaipeiTimeZone);
        }

        /// <summary>
        /// 取得當前台北時間
        /// </summary>
        /// <returns>當前台北時間</returns>
        public static DateTime GetTaipeiNow()
        {
            return ConvertToTaipeiTime(DateTime.UtcNow);
        }
    }
}
