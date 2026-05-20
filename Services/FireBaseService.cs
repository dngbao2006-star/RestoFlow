using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Firebase.Database;
using Firebase.Database.Query;
using AppManagermentRestaurant.Models;
using Firebase.Database.Streaming;

namespace AppManagermentRestaurant.Services
{
    public class FirebaseService
    {
        private const string FIREBASE_URL = "https://doanquanan-6a948-default-rtdb.firebaseio.com";
        private const string WEB_API_KEY = "AIzaSyCxLsmz6jRmvIgyZywfNm9GVUeqm3ULKOs";
        private const string GOOGLE_AUTH_URL = "https://identitytoolkit.googleapis.com/v1/accounts";
        private static readonly HttpClient client = new HttpClient();
        public event Action<string, PresenceModel>? PresenceChanged;

        private readonly FirebaseClient firebaseClient = new FirebaseClient(FIREBASE_URL);

        public async Task<bool> CheckEmailExistsAsync(string email)
        {
            try
            {
                string endpoint = $"{GOOGLE_AUTH_URL}:createAuthUri?key={WEB_API_KEY}";
                var payload = new { identifier = email, continueUri = "http://localhost" };
                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                HttpResponseMessage res = await client.PostAsync(endpoint, content);
                if (res.IsSuccessStatusCode)
                {
                    string result = await res.Content.ReadAsStringAsync();
                    using JsonDocument doc = JsonDocument.Parse(result);
                    if (doc.RootElement.TryGetProperty("registered", out JsonElement registeredElement))
                    {
                        return registeredElement.GetBoolean();
                    }
                }
                return false;
            }
            catch
            {
                return false;
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
                return res.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task SendMessageAsync(FirebaseChatMessage message)
        {
            await firebaseClient
                .Child("Chats")
                .PostAsync(message);
        }

        public IDisposable ListenForMessages(Action<FirebaseChatMessage> onMessageReceived)
        {
            return firebaseClient
                .Child("Chats")
                .AsObservable<FirebaseChatMessage>()
                .Subscribe(d =>
                {
                    if (d.Object != null)
                    {
                        onMessageReceived(d.Object);
                    }
                });
        }

        public async Task<(string Token, string Uid, string HoTen, string Quyen, string TrangThai, bool IsEmailVerified)> LoginAndGetProfileAsync(string email, string password)
        {
            try
            {
                string authEndpoint = $"{GOOGLE_AUTH_URL}:signInWithPassword?key={WEB_API_KEY}";
                var payload = new { email, password, returnSecureToken = true };
                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                HttpResponseMessage authRes = await client.PostAsync(authEndpoint, content);
                if (!authRes.IsSuccessStatusCode) return (null, null, null, null, null, false);

                string authResult = await authRes.Content.ReadAsStringAsync();
                using JsonDocument doc = JsonDocument.Parse(authResult);
                string uid = doc.RootElement.GetProperty("localId").GetString();
                string token = doc.RootElement.GetProperty("idToken").GetString();

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

        public async Task<(bool IsSuccess, string ErrorMessage)> RegisterNewUserAsync(string email, string password, string name, string role)
        {
            try
            {
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

                using JsonDocument doc = JsonDocument.Parse(authResult);
                string newUid = doc.RootElement.GetProperty("localId").GetString();
                string token = doc.RootElement.GetProperty("idToken").GetString();

                string verifyEndpoint = $"{GOOGLE_AUTH_URL}:sendOobCode?key={WEB_API_KEY}";
                var verifyPayload = new { requestType = "VERIFY_EMAIL", idToken = token };
                var verifyContent = new StringContent(JsonSerializer.Serialize(verifyPayload), Encoding.UTF8, "application/json");
                await client.PostAsync(verifyEndpoint, verifyContent);

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

        public async Task SetUserOnlineAsync(string uid, string hoTen, string sessionId)
        {
            var data = new PresenceModel
            {
                HoTen = hoTen,
                IsOnline = true,
                LastSeen = DateTime.UtcNow,
                Device = DeviceInfo.Platform.ToString(),
                SessionId = sessionId
            };

            await firebaseClient
                .Child("Presence")
                .Child(uid)
                .PutAsync(data);
        }

        public async Task SetUserOfflineAsync(string uid, string hoTen)
        {
            var data = new PresenceModel
            {
                HoTen = hoTen,
                IsOnline = false,
                LastSeen = DateTime.UtcNow,
                Device = DeviceInfo.Platform.ToString()
            };

            await firebaseClient
                .Child("Presence")
                .Child(uid)
                .PutAsync(data);
        }

        public async Task<PresenceModel?> GetPresenceAsync(string uid)
        {
            return await firebaseClient
                .Child("Presence")
                .Child(uid)
                .OnceSingleAsync<PresenceModel>();
        }

        /// <summary>
        /// Đổi mật khẩu người dùng thông qua Firebase Auth REST API.
        /// Bước 1: Xác thực mật khẩu hiện tại bằng signInWithPassword → lấy idToken mới.
        /// Bước 2: Gọi accounts:update với idToken + password mới.
        /// </summary>
        public async Task<(bool IsSuccess, string ErrorMessage)> ChangePasswordAsync(string email, string currentPassword, string newPassword)
        {
            try
            {
                // BƯỚC 1: Xác thực mật khẩu hiện tại
                string signInEndpoint = $"{GOOGLE_AUTH_URL}:signInWithPassword?key={WEB_API_KEY}";
                var signInPayload = new { email, password = currentPassword, returnSecureToken = true };
                var signInContent = new StringContent(JsonSerializer.Serialize(signInPayload), Encoding.UTF8, "application/json");

                HttpResponseMessage signInRes = await client.PostAsync(signInEndpoint, signInContent);

                if (!signInRes.IsSuccessStatusCode)
                {
                    string signInError = await signInRes.Content.ReadAsStringAsync();
                    using JsonDocument errorDoc = JsonDocument.Parse(signInError);
                    string errorMsg = errorDoc.RootElement.GetProperty("error").GetProperty("message").GetString() ?? "";

                    return errorMsg switch
                    {
                        "INVALID_PASSWORD" => (false, "Mật khẩu hiện tại không đúng."),
                        "INVALID_LOGIN_CREDENTIALS" => (false, "Mật khẩu hiện tại không đúng."),
                        "USER_DISABLED" => (false, "Tài khoản đã bị vô hiệu hóa."),
                        "TOO_MANY_ATTEMPTS_TRY_LATER" => (false, "Quá nhiều lần thử. Vui lòng thử lại sau."),
                        _ => (false, "Xác thực thất bại: " + errorMsg)
                    };
                }

                // Lấy idToken từ phiên xác thực
                string signInResult = await signInRes.Content.ReadAsStringAsync();
                using JsonDocument signInDoc = JsonDocument.Parse(signInResult);
                string idToken = signInDoc.RootElement.GetProperty("idToken").GetString() ?? "";

                // BƯỚC 2: Đổi mật khẩu bằng idToken
                string updateEndpoint = $"{GOOGLE_AUTH_URL}:update?key={WEB_API_KEY}";
                var updatePayload = new { idToken, password = newPassword, returnSecureToken = true };
                var updateContent = new StringContent(JsonSerializer.Serialize(updatePayload), Encoding.UTF8, "application/json");

                HttpResponseMessage updateRes = await client.PostAsync(updateEndpoint, updateContent);

                if (!updateRes.IsSuccessStatusCode)
                {
                    string updateError = await updateRes.Content.ReadAsStringAsync();
                    using JsonDocument updateErrorDoc = JsonDocument.Parse(updateError);
                    string updateErrorMsg = updateErrorDoc.RootElement.GetProperty("error").GetProperty("message").GetString() ?? "";

                    if (updateErrorMsg.Contains("WEAK_PASSWORD"))
                        return (false, "Mật khẩu mới quá yếu. Vui lòng chọn mật khẩu mạnh hơn (ít nhất 6 ký tự).");

                    return (false, "Đổi mật khẩu thất bại: " + updateErrorMsg);
                }

                return (true, "Mật khẩu đã được cập nhật thành công!");
            }
            catch (Exception ex)
            {
                return (false, "Lỗi kết nối: " + ex.Message);
            }
        }

        public void StartPresenceListener()
        {
            firebaseClient
                .Child("Presence")
                .AsObservable<PresenceModel>()
                .Subscribe(d =>
                {
                    if (d.Object == null || string.IsNullOrEmpty(d.Key))
                    {
                        return;
                    }

                    PresenceChanged?.Invoke(
                        d.Key,
                        d.Object);
                });
        }

        /// <summary>
        /// Lắng nghe toàn bộ node Presence và lọc theo uid của user hiện tại.
        /// Khi SessionId trên Firebase thay đổi (không khớp localSessionId),
        /// gọi onConflictDetected để thực hiện force logout.
        ///
        /// LƯU Ý QUAN TRỌNG:
        /// Phải subscribe ở cấp .Child("Presence") chứ KHÔNG PHẢI .Child("Presence").Child(uid).
        /// Vì AsObservable luôn lắng nghe CHILDREN của node được chỉ định:
        ///   - .Child("Presence").AsObservable() → children = {uid} → deserialize OK ✅
        ///   - .Child("Presence").Child(uid).AsObservable() → children = HoTen, IsOnline...
        ///     → cố deserialize string/bool thành PresenceModel → FAIL SILENT ❌
        /// </summary>
        public IDisposable ListenForSessionConflict(string uid, string localSessionId, Action onConflictDetected)
        {
            return firebaseClient
                .Child("Presence")
                .AsObservable<PresenceModel>()
                .Subscribe(d =>
                {
                    if (d.Object == null || string.IsNullOrEmpty(d.Key)) return;

                    // Chỉ xử lý event của chính user hiện tại, bỏ qua user khác
                    if (d.Key != uid) return;

                    var remoteSessionId = d.Object.SessionId;

                    // Nếu SessionId trên Firebase khác với SessionId local
                    // → có thiết bị khác vừa đăng nhập cùng tài khoản
                    if (!string.IsNullOrEmpty(remoteSessionId)
                        && remoteSessionId != localSessionId)
                    {
                        onConflictDetected?.Invoke();
                    }
                });
        }

        public async Task UpdateTableAsync(Table table)
        {
            var key = $"table_{table.Id}";
            var data = new Dictionary<string, object>
            {
                ["Status"] = (int)table.Status,
                ["CurrentOrderId"] = table.CurrentOrderId ?? (object)string.Empty,
                ["HasOrdered"] = table.HasOrdered,
                ["OrderItemCount"] = table.OrderItemCount,
                ["OrderTotal"] = table.OrderTotal ?? string.Empty,
                ["ArrivalTime"] = table.ArrivalTime?.ToString("yyyy-MM-ddTHH:mm:ss") ?? string.Empty,
                ["ReservedFor"] = table.ReservedFor ?? string.Empty,
                ["ReservedPhone"] = table.ReservedPhone ?? string.Empty,
                ["ReservedAt"] = table.ReservedAt?.ToString("yyyy-MM-ddTHH:mm:ss") ?? string.Empty
            };
            await firebaseClient.Child("Tables").Child(key).PatchAsync(data);
        }

        public async Task CreateTableAsync(Table table)
        {
            var key = $"table_{table.Id}";
            var data = new
            {
                table.Id,
                table.Number,
                table.Floor,
                Status = (int)table.Status,
                table.Capacity,
                CurrentOrderId = table.CurrentOrderId ?? (object)string.Empty,
                HasOrdered = table.HasOrdered,
                OrderItemCount = table.OrderItemCount,
                OrderTotal = table.OrderTotal ?? string.Empty,
                ArrivalTime = table.ArrivalTime?.ToString("yyyy-MM-ddTHH:mm:ss") ?? string.Empty,
                ReservedFor = table.ReservedFor ?? string.Empty,
                ReservedPhone = table.ReservedPhone ?? string.Empty,
                ReservedAt = table.ReservedAt?.ToString("yyyy-MM-ddTHH:mm:ss") ?? string.Empty
            };
            await firebaseClient.Child("Tables").Child(key).PutAsync(data);
        }

        public async Task DeleteTableAsync(int tableId)
        {
            var key = $"table_{tableId}";
            await firebaseClient.Child("Tables").Child(key).DeleteAsync();
        }

        public async Task UpdateOrderStatusAsync(Order order)
        {
            var key = $"order_{order.Id}";
            var data = new Dictionary<string, object>
            {
                ["Status"] = (int)order.Status,
                ["PaymentMethod"] = (int)order.PaymentMethod,
                ["Discount"] = order.Discount
            };
            await firebaseClient.Child("Orders").Child(key).PatchAsync(data);
        }

        public async Task CreateOrderAsync(Order order)
        {
            var key = $"order_{order.Id}";
            var data = new
            {
                order.Id,
                order.TableId,
                order.TableNumber,
                Status = (int)order.Status,
                CreatedAt = order.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ss"),
                order.ServerName,
                order.ServerId,
                order.Discount,
                PaymentMethod = (int)order.PaymentMethod
            };
            await firebaseClient.Child("Orders").Child(key).PutAsync(data);
        }

        public async Task SaveOrderItemAsync(Order order, OrderItem item)
        {
            var key = $"oi_{item.Id}";
            var data = new
            {
                item.Id,
                OrderId = $"order_{order.Id}",
                item.MenuItemId,
                item.Name,
                item.Price,
                item.Quantity,
                Status = (int)item.Status,
                item.Image,
                Notes = item.Notes ?? string.Empty
            };
            await firebaseClient.Child("OrderItems").Child(key).PutAsync(data);
        }

        public async Task<List<DishReady>> GetDishReadyAsync()
        {
            var result = await firebaseClient
                .Child("DishReady")
                .OnceAsync<DishReady>();

            return result
                .Select(x => x.Object)
                .ToList();
        }

        public async Task DeleteDishReadyAsync(int id)
        {
            await firebaseClient
                .Child("DishReady")
                .Child($"ready_{id}")
                .DeleteAsync();
        }

        public async Task UpdateOrderItemStatusAsync(int orderItemId, DishStatus status)
        {
            var key = $"oi_{orderItemId}";
            var data = new Dictionary<string, object>
            {
                ["Status"] = (int)status
            };
            await firebaseClient.Child("OrderItems").Child(key).PatchAsync(data);
        }

        public async Task<OrderItem?> GetOrderItemByOrderAndMenuItemAsync(int orderId, int menuItemId)
        {
            var allOrderItems = await firebaseClient
                .Child("OrderItems")
                .OnceAsync<OrderItem>();

            var matchingItem = allOrderItems
                .FirstOrDefault(x => 
                    x.Object != null && 
                    x.Object.MenuItemId == menuItemId &&
                    (x.Object.Id.ToString().Contains(orderId.ToString()) || 
                     x.Key.Contains($"oi_{orderId}")));

            return matchingItem?.Object;
        }

        public async Task UpdateOrderItemStatusByOrderAndMenuItemAsync(int orderId, int menuItemId, DishStatus status)
        {
            try
            {
                var allOrderItems = await firebaseClient
                    .Child("OrderItems")
                    .OnceAsync<dynamic>();

                // Tìm OrderItem dựa trên OrderId từ DishReady so sánh với Id của OrderItem
                // và MenuItemId phải khớp
                var matchingItem = allOrderItems
                    .FirstOrDefault(x => 
                    {
                        try
                        {
                            if (x.Object == null) return false;

                            // Lấy giá trị từ OrderItem
                            var orderItemId = x.Object["Id"];
                            var orderItemMenuItemId = x.Object["MenuItemId"];

                            // So sánh Id của OrderItem với OrderId của DishReady
                            // và MenuItemId phải khớp
                            return orderItemId == orderId && orderItemMenuItemId == menuItemId;
                        }
                        catch
                        {
                            return false;
                        }
                    });

                if (matchingItem != null)
                {
                    var data = new Dictionary<string, object>
                    {
                        ["Status"] = (int)status
                    };
                    await firebaseClient.Child("OrderItems").Child(matchingItem.Key).PatchAsync(data);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating order item status: {ex.Message}");
                throw;
            }
        }

        public async Task SaveMenuItemAsync(FoodItem menuItem)
        {
            try
            {
                var key = $"menu_{menuItem.Id}";
                var data = new
                {
                    menuItem.Id,
                    menuItem.Name,
                    menuItem.Category,
                    menuItem.Price,
                    menuItem.Description,
                    menuItem.Image,
                    menuItem.Available,
                    menuItem.OutOfStock
                };
                await firebaseClient.Child("MenuItems").Child(key).PutAsync(data);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving menu item: {ex.Message}");
                throw;
            }
        }

        public async Task DeleteMenuItemAsync(int menuItemId)
        {
            try
            {
                var key = $"menu_{menuItemId}";
                await firebaseClient.Child("MenuItems").Child(key).DeleteAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deleting menu item: {ex.Message}");
                throw;
            }
        }

        public async Task UpdateOrderItemFieldsAsync(int orderItemId, int quantity, string? notes)
        {
            var key = $"oi_{orderItemId}";
            var data = new Dictionary<string, object>
            {
                ["Quantity"] = quantity,
                ["Notes"] = notes ?? string.Empty
            };
            await firebaseClient.Child("OrderItems").Child(key).PatchAsync(data);
        }

        public async Task DeleteOrderItemAsync(int orderItemId)
        {
            var key = $"oi_{orderItemId}";
            await firebaseClient.Child("OrderItems").Child(key).DeleteAsync();
        }
    }
}