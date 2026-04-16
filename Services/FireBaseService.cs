// client-24520222
// server-DuongDangChinh
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AppManagermentRestaurant.Services
{
    public class FirebaseService
    {
        private const string FIREBASE_URL = "https://doanquanan-6a948-default-rtdb.firebaseio.com";
        private const string WEB_API_KEY = "AIzaSyCxLsmz6jRmvIgyZywfNm9GVUeqm3ULKOs";
        private const string GOOGLE_AUTH_URL = "https://identitytoolkit.googleapis.com/v1/accounts";
        private static readonly HttpClient client = new HttpClient();

        // Trả về tuple chứa toàn bộ thông tin user
        public async Task<(string Token, string Uid, string HoTen, string Quyen, string TrangThai, bool IsEmailVerified)> LoginAndGetProfileAsync(string email, string password)
        {
            try
            {
                // 1. Gửi request Đăng nhập
                string authEndpoint = $"{GOOGLE_AUTH_URL}:signInWithPassword?key={WEB_API_KEY}";
                var payload = new { email, password, returnSecureToken = true };
                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                HttpResponseMessage authRes = await client.PostAsync(authEndpoint, content);
                if (!authRes.IsSuccessStatusCode) return (null, null, null, null, null, false);

                string authResult = await authRes.Content.ReadAsStringAsync();
                using JsonDocument doc = JsonDocument.Parse(authResult);
                string uid = doc.RootElement.GetProperty("localId").GetString();
                string token = doc.RootElement.GetProperty("idToken").GetString();

                // 2. Tra cứu trạng thái Xác minh Email (emailVerified)
                bool isEmailVerified = false;
                string lookupEndpoint = $"{GOOGLE_AUTH_URL}:lookup?key={WEB_API_KEY}";
                var lookupPayload = new { idToken = token };
                var lookupContent = new StringContent(JsonSerializer.Serialize(lookupPayload), Encoding.UTF8, "application/json");
                HttpResponseMessage lookupRes = await client.PostAsync(lookupEndpoint, lookupContent);

                if (lookupRes.IsSuccessStatusCode)
                {
                    string lookupResult = await lookupRes.Content.ReadAsStringAsync();
                    using JsonDocument lookupDoc = JsonDocument.Parse(lookupResult);
                    var usersArray = lookupDoc.RootElement.GetProperty("users");
                    if (usersArray.GetArrayLength() > 0)
                    {
                        isEmailVerified = usersArray[0].GetProperty("emailVerified").GetBoolean();
                    }
                }

                // 3. Chui vào Realtime Database lấy Quyền hạn
                string dbUrl = $"{FIREBASE_URL}/Users/{uid}.json?auth={token}";
                HttpResponseMessage dbRes = await client.GetAsync(dbUrl);
                string dbResult = await dbRes.Content.ReadAsStringAsync();

                if (dbResult == "null") return (token, uid, null, null, null, isEmailVerified);

                using JsonDocument dbDoc = JsonDocument.Parse(dbResult);
                string hoTen = dbDoc.RootElement.GetProperty("hoTen").GetString();
                string quyen = dbDoc.RootElement.GetProperty("quyen").GetString();
                string trangThai = dbDoc.RootElement.GetProperty("trangThai").GetString();

                return (token, uid, hoTen, quyen, trangThai, isEmailVerified);
            }
            catch
            {
                return (null, null, null, null, null, false);
            }
        }

        public async Task<bool> SendPasswordResetEmailAsync(string email)
        {
            try
            {
                string resetEndpoint = $"{GOOGLE_AUTH_URL}:sendOobCode?key={WEB_API_KEY}";
                var payload = new { requestType = "PASSWORD_RESET", email = email };
                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                HttpResponseMessage res = await client.PostAsync(resetEndpoint, content);
                return res.IsSuccessStatusCode; // Trả về true nếu Firebase gửi mail thành công
            }
            catch
            {
                return false;
            }
        }

        // Thêm vào bên trong class FirebaseService
        public async Task<(bool IsSuccess, string ErrorMessage)> RegisterNewUserAsync(string email, string password, string name, string role)
        {
            try
            {
                // 1. Gọi API tạo tài khoản (Auth)
                string signUpEndpoint = $"{GOOGLE_AUTH_URL}:signUp?key={WEB_API_KEY}";
                var payload = new { email, password, returnSecureToken = true };
                var authContent = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                HttpResponseMessage authRes = await client.PostAsync(signUpEndpoint, authContent);
                string authResult = await authRes.Content.ReadAsStringAsync();

                if (!authRes.IsSuccessStatusCode)
                {
                    using JsonDocument errorDoc = JsonDocument.Parse(authResult);
                    string errorMsg = errorDoc.RootElement.GetProperty("error").GetProperty("message").GetString();
                    if (errorMsg == "EMAIL_EXISTS") return (false, "Email này đã được sử dụng!");
                    if (errorMsg.Contains("WEAK_PASSWORD")) return (false, "Mật khẩu phải từ 6 ký tự trở lên!");
                    return (false, "Lỗi tạo tài khoản: " + errorMsg);
                }

                // 2. Lấy UID và Token của user vừa tạo
                using JsonDocument doc = JsonDocument.Parse(authResult);
                string newUid = doc.RootElement.GetProperty("localId").GetString();
                string token = doc.RootElement.GetProperty("idToken").GetString();

                // =========================================================
                // BƯỚC 2.5: GỬI NGAY EMAIL XÁC MINH CHO TÀI KHOẢN VỪA TẠO
                // =========================================================
                string verifyEndpoint = $"{GOOGLE_AUTH_URL}:sendOobCode?key={WEB_API_KEY}";
                var verifyPayload = new { requestType = "VERIFY_EMAIL", idToken = token };
                var verifyContent = new StringContent(JsonSerializer.Serialize(verifyPayload), Encoding.UTF8, "application/json");
                await client.PostAsync(verifyEndpoint, verifyContent);
                // =========================================================

                // 3. Lưu hồ sơ vào Realtime Database
                // ... (Giữ nguyên đoạn code lưu Database của bạn ở đây) ...
                var newUserProfile = new { hoTen = name, email = email, quyen = role, trangThai = "HoatDong" };
                var dbContent = new StringContent(JsonSerializer.Serialize(newUserProfile), Encoding.UTF8, "application/json");
                HttpResponseMessage dbRes = await client.PutAsync($"{FIREBASE_URL}/Users/{newUid}.json?auth={token}", dbContent);

                if (dbRes.IsSuccessStatusCode)
                    return (true, "Tạo tài khoản và gửi email xác minh thành công!");
                else
                    return (false, "Tạo tài khoản thành công nhưng lỗi khi lưu Database.");
            }
            catch (Exception ex)
            {
                return (false, "Lỗi kết nối hệ thống: " + ex.Message);
            }
        }

        public async Task<bool> ResendVerificationEmailAsync(string idToken)
        {
            try
            {
                string verifyEndpoint = $"{GOOGLE_AUTH_URL}:sendOobCode?key={WEB_API_KEY}";
                var verifyPayload = new { requestType = "VERIFY_EMAIL", idToken = idToken };
                var verifyContent = new StringContent(JsonSerializer.Serialize(verifyPayload), Encoding.UTF8, "application/json");
                HttpResponseMessage res = await client.PostAsync(verifyEndpoint, verifyContent);
                return res.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

    }
}