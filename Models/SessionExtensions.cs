using System.Globalization;
using System.Text.Json;

namespace ShopNgocLan.Models
{
    public static class SessionExtensions
    {
        /// <summary>
        /// Lưu một giá trị decimal vào Session
        /// </summary>
        public static void SetDecimal(this ISession session, string key, decimal value)
        {
            // Chuyển decimal thành string (dùng CultureInfo.InvariantCulture
            // để đảm bảo dấu '.' được dùng làm dấu thập phân, tránh lỗi)
            session.SetString(key, value.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Lấy một giá trị decimal từ Session
        /// </summary>
        public static decimal? GetDecimal(this ISession session, string key)
        {
            var stringValue = session.GetString(key);

            // Thử chuyển đổi string ngược lại thành decimal
            if (decimal.TryParse(stringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal result))
            {
                return result;
            }

            // Trả về null nếu không tìm thấy hoặc không chuyển đổi được
            return null;
        }
        public static void SetObject(this ISession session, string key, object value)
        {
            // Chuyển đối tượng thành một chuỗi JSON
            string jsonString = JsonSerializer.Serialize(value);
            // Lưu chuỗi JSON đó vào Session
            session.SetString(key, jsonString);
        }

        /// <summary>
        /// Đọc một đối tượng (object) phức tạp từ Session (đã lưu bằng JSON)
        /// </summary>
        public static T GetObject<T>(this ISession session, string key)
        {
            // Đọc chuỗi JSON từ Session
            var jsonString = session.GetString(key);

            if (string.IsNullOrEmpty(jsonString))
            {
                // Trả về giá trị mặc định (thường là null) nếu không tìm thấy
                return default(T);
            }

            // Chuyển chuỗi JSON ngược lại thành đối tượng (kiểu T)
            return JsonSerializer.Deserialize<T>(jsonString);
        }
    }
}
