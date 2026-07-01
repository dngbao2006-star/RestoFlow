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
        private const string Template = "compact2";            // compact2 = có logo ngân hàng + thông tin

        /// <summary>
        /// Tạo URL hình ảnh QR VietQR.
        /// </summary>
        /// <param name="amount">Số tiền (VND, số nguyên)</param>
        /// <param name="addInfo">Nội dung chuyển khoản</param>
        /// <returns>URL trực tiếp tới hình ảnh QR</returns>
        public static string BuildQrImageUrl(decimal amount, string addInfo)
        {
            var config = AppContext.Instance.SystemConfiguration;
            var bankId = string.IsNullOrWhiteSpace(config.BankBin) ? "970436" : config.BankBin.Trim();
            var accountNumber = string.IsNullOrWhiteSpace(config.AccountNumber) ? "1034973536" : config.AccountNumber.Trim();
            var accountName = string.IsNullOrWhiteSpace(config.AccountName) ? "DANG NGUYEN GIA BAO" : config.AccountName.Trim();
            var encodedInfo = WebUtility.UrlEncode(addInfo ?? "");
            return $"https://img.vietqr.io/image/{bankId}-{accountNumber}-{Template}.png"
                 + $"?amount={amount:0}"
                 + $"&addInfo={encodedInfo}"
                 + $"&accountName={WebUtility.UrlEncode(accountName)}";
        }
    }
}
