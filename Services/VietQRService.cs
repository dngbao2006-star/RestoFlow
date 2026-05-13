using System;
using System.Net;

namespace AppManagermentRestaurant.Services
{
    /// <summary>
    /// Tạo URL hình ảnh QR thanh toán qua VietQR Quick Link (img.vietqr.io).
    /// Không cần API key.
    /// </summary>
    public static class VietQRService
    {
        // ── Cấu hình ngân hàng ──
        private const string BankId = "970436";               // Mã BIN Vietcombank
        private const string AccountNumber = "1034973536";     // Số tài khoản
        private const string AccountName = "DANG NGUYEN GIA BAO"; // Tên chủ TK (in hoa, không dấu)
        private const string Template = "compact2";            // compact2 = có logo ngân hàng + thông tin

        /// <summary>
        /// Tạo URL hình ảnh QR VietQR.
        /// </summary>
        /// <param name="amount">Số tiền (VND, số nguyên)</param>
        /// <param name="addInfo">Nội dung chuyển khoản</param>
        /// <returns>URL trực tiếp tới hình ảnh QR</returns>
        public static string BuildQrImageUrl(decimal amount, string addInfo)
        {
            var encodedInfo = WebUtility.UrlEncode(addInfo ?? "");
            return $"https://img.vietqr.io/image/{BankId}-{AccountNumber}-{Template}.png"
                 + $"?amount={amount:0}"
                 + $"&addInfo={encodedInfo}"
                 + $"&accountName={WebUtility.UrlEncode(AccountName)}";
        }
    }
}
