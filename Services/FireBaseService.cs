using System.Net.Http;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Firebase.Database;
using Firebase.Database.Query;
using AppManagermentRestaurant.Models;
using Firebase.Database.Streaming;

namespace AppManagermentRestaurant.Services
{
    public sealed record FirebaseLoginResult(
        string? Token,
        string? Uid,
        string? HoTen,
        string? Quyen,
        string? TrangThai,
        string Phone,
        DateTime? JoinDate,
        bool IsEmailVerified,
        string ErrorMessage);

    public sealed record RevenueUpdateResult(
        string DailyLabel, decimal DailyValue,
        string WeeklyLabel, decimal WeeklyValue,
        string MonthlyLabel, decimal MonthlyValue);

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
                if (res.IsSuccessStatusCode) return true;

                // Treat an unknown email as an accepted request so the UI does not
                // reveal which addresses are registered.
                string responseBody = await res.Content.ReadAsStringAsync();
                return responseBody.Contains("EMAIL_NOT_FOUND", StringComparison.Ordinal);
            }
            catch
            {
                return false;
            }
        }

        public async Task<string> SendMessageAsync(FirebaseChatMessage message)
        {
            var result = await firebaseClient
                .Child("Chats")
                .PostAsync(message);
            return result.Key;
        }

        public async Task<string> CreateNotificationAsync(Notification notification)
        {
            var key = $"notification_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_{Guid.NewGuid():N}";
            var payload = new
            {
                notification.Id,
                Type = (int)notification.Type,
                notification.Title,
                notification.Message,
                Timestamp = notification.Timestamp.ToString("yyyy-MM-ddTHH:mm:ss"),
                notification.Read,
                notification.Audience,
                notification.ReadBy,
                notification.TableId,
                notification.OrderId
            };

            await firebaseClient.Child("Notifications").Child(key).PutAsync(payload);
            notification.FirebaseKey = key;
            return key;
        }

        public async Task DeleteNotificationAsync(Notification notification)
        {
            var key = notification.FirebaseKey;
            if (string.IsNullOrWhiteSpace(key))
            {
                var records = await firebaseClient.Child("Notifications").OnceAsync<Notification>();
                key = records.FirstOrDefault(record => record.Object?.Id == notification.Id)?.Key;
            }

            if (string.IsNullOrWhiteSpace(key))
                throw new InvalidOperationException("Kh�ng t�m th?y th�ng b�o tr�n Firebase.");

            await firebaseClient.Child("Notifications").Child(key).DeleteAsync();
        }

        public async Task MarkNotificationsReadAsync(IEnumerable<Notification> notifications)
        {
            var uid = AppContext.Instance.CurrentUser?.FirebaseUid;
            if (string.IsNullOrWhiteSpace(uid)) return;
            var tasks = notifications
                .Where(notification => !string.IsNullOrWhiteSpace(notification.FirebaseKey))
                .Select(notification => firebaseClient.Child("Notifications")
                    .Child(notification.FirebaseKey).Child("ReadBy").Child(uid).PutAsync(true));
            await Task.WhenAll(tasks);
        }

        public async Task SaveSystemConfigurationAsync(SystemConfiguration configuration)
        {
            await firebaseClient.Child("SystemConfig").Child("general").PutAsync(configuration);
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

        public async Task<FirebaseLoginResult> LoginAndGetProfileAsync(string email, string password)
        {
            try
            {
                string authEndpoint = $"{GOOGLE_AUTH_URL}:signInWithPassword?key={WEB_API_KEY}";
                var payload = new { email, password, returnSecureToken = true };
                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                HttpResponseMessage authRes = await client.PostAsync(authEndpoint, content);
                string authResult = await authRes.Content.ReadAsStringAsync();
                if (!authRes.IsSuccessStatusCode)
                {
                    return new FirebaseLoginResult(null, null, null, null, null, "", null, false,
                        MapFirebaseAuthError(authResult));
                }

                using JsonDocument doc = JsonDocument.Parse(authResult);
                string? uid = doc.RootElement.GetProperty("localId").GetString();
                string? token = doc.RootElement.GetProperty("idToken").GetString();
                if (string.IsNullOrWhiteSpace(uid) || string.IsNullOrWhiteSpace(token))
                {
                    return new FirebaseLoginResult(null, null, null, null, null, "", null, false,
                        "Firebase kh�ng tr? v? phi�n dang nh?p h?p l?.");
                }

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
                if (!dbRes.IsSuccessStatusCode)
                {
                    return new FirebaseLoginResult(token, uid, null, null, null, "", null, isEmailVerified,
                        "Kh�ng th? t?i h? so v� ph�n quy?n ngu?i d�ng.");
                }

                string dbResult = await dbRes.Content.ReadAsStringAsync();

                if (dbResult == "null")
                {
                    return new FirebaseLoginResult(token, uid, null, null, null, "", null, isEmailVerified,
                        "T�i kho?n d� x�c th?c nhung chua c� h? so trong Users.");
                }

                using JsonDocument dbDoc = JsonDocument.Parse(dbResult);
                string? hoTen = GetOptionalString(dbDoc.RootElement, "hoTen");
                string? quyen = GetOptionalString(dbDoc.RootElement, "quyen");
                string? trangThai = GetOptionalString(dbDoc.RootElement, "trangThai");
                string phone = GetOptionalString(dbDoc.RootElement, "sdt") ?? "";
                string? joinDateText = GetOptionalString(dbDoc.RootElement, "ngayVaoLam");
                DateTime? joinDate = DateTime.TryParse(joinDateText, out var parsedJoinDate) ? parsedJoinDate : null;

                return new FirebaseLoginResult(token, uid, hoTen, quyen, trangThai, phone, joinDate,
                    isEmailVerified, "");
            }
            catch (HttpRequestException)
            {
                return new FirebaseLoginResult(null, null, null, null, null, "", null, false,
                    "Kh�ng th? k?t n?i Firebase. Vui l�ng ki?m tra m?ng v� th? l?i.");
            }
            catch (Exception)
            {
                return new FirebaseLoginResult(null, null, null, null, null, "", null, false,
                    "Kh�ng th? ho�n t?t dang nh?p. Vui l�ng th? l?i.");
            }
        }

        private static string? GetOptionalString(JsonElement element, string propertyName)
            => element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
                ? property.GetString()
                : null;

        private static string MapFirebaseAuthError(string responseBody)
        {
            try
            {
                using var document = JsonDocument.Parse(responseBody);
                var code = document.RootElement.GetProperty("error").GetProperty("message").GetString() ?? "";
                return code switch
                {
                    "INVALID_LOGIN_CREDENTIALS" or "INVALID_PASSWORD" or "EMAIL_NOT_FOUND" =>
                        "Email ho?c m?t kh?u kh�ng ch�nh x�c.",
                    "USER_DISABLED" => "T�i kho?n d� b? v� hi?u h�a tr�n Firebase Authentication.",
                    "TOO_MANY_ATTEMPTS_TRY_LATER" => "B?n d� th? qu� nhi?u l?n. Vui l�ng th? l?i sau.",
                    _ => "�ang nh?p th?t b?i. Vui l�ng th? l?i."
                };
            }
            catch
            {
                return "�ang nh?p th?t b?i. Vui l�ng th? l?i.";
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
                    if (errorMsg == "EMAIL_EXISTS") return (false, "Email n�y d� du?c s? d?ng!");
                    if (errorMsg.Contains("WEAK_PASSWORD")) return (false, "M?t kh?u ph?i t? 6 k� t? tr? l�n!");
                    return (false, "L?i t?o t�i kho?n: " + errorMsg);
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
                    return (true, "T?o t�i kho?n v� g?i email x�c minh th�nh c�ng!");
                else
                    return (false, "T?o t�i kho?n th�nh c�ng nhung l?i khi luu Database.");
            }
            catch (Exception ex)
            {
                return (false, "L?i k?t n?i h? th?ng: " + ex.Message);
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
        /// �?i m?t kh?u ngu?i d�ng th�ng qua Firebase Auth REST API.
        /// Bu?c 1: X�c th?c m?t kh?u hi?n t?i b?ng signInWithPassword ? l?y idToken m?i.
        /// Bu?c 2: G?i accounts:update v?i idToken + password m?i.
        /// </summary>
        public async Task<(bool IsSuccess, string ErrorMessage)> ChangePasswordAsync(string email, string currentPassword, string newPassword)
        {
            try
            {
                // BU?C 1: X�c th?c m?t kh?u hi?n t?i
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
                        "INVALID_PASSWORD" => (false, "M?t kh?u hi?n t?i kh�ng d�ng."),
                        "INVALID_LOGIN_CREDENTIALS" => (false, "M?t kh?u hi?n t?i kh�ng d�ng."),
                        "USER_DISABLED" => (false, "T�i kho?n d� b? v� hi?u h�a."),
                        "TOO_MANY_ATTEMPTS_TRY_LATER" => (false, "Qu� nhi?u l?n th?. Vui l�ng th? l?i sau."),
                        _ => (false, "X�c th?c th?t b?i: " + errorMsg)
                    };
                }

                // L?y idToken t? phi�n x�c th?c
                string signInResult = await signInRes.Content.ReadAsStringAsync();
                using JsonDocument signInDoc = JsonDocument.Parse(signInResult);
                string idToken = signInDoc.RootElement.GetProperty("idToken").GetString() ?? "";

                // BU?C 2: �?i m?t kh?u b?ng idToken
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
                        return (false, "M?t kh?u m?i qu� y?u. Vui l�ng ch?n m?t kh?u m?nh hon (�t nh?t 6 k� t?).");

                    return (false, "�?i m?t kh?u th?t b?i: " + updateErrorMsg);
                }

                return (true, "M?t kh?u d� du?c c?p nh?t th�nh c�ng!");
            }
            catch (Exception ex)
            {
                return (false, "L?i k?t n?i: " + ex.Message);
            }
        }

        public IDisposable StartPresenceListener()
        {
            return firebaseClient
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
                }, error => System.Diagnostics.Debug.WriteLine($"[Presence] Listener stopped: {error}"));
        }

        /// <summary>
        /// L?ng nghe to�n b? node Presence v� l?c theo uid c?a user hi?n t?i.
        /// Khi SessionId tr�n Firebase thay d?i (kh�ng kh?p localSessionId),
        /// g?i onConflictDetected d? th?c hi?n force logout.
        ///
        /// LUU � QUAN TR?NG:
        /// Ph?i subscribe ? c?p .Child("Presence") ch? KH�NG PH?I .Child("Presence").Child(uid).
        /// V� AsObservable lu�n l?ng nghe CHILDREN c?a node du?c ch? d?nh:
        ///   - .Child("Presence").AsObservable() ? children = {uid} ? deserialize OK ?
        ///   - .Child("Presence").Child(uid).AsObservable() ? children = HoTen, IsOnline...
        ///     ? c? deserialize string/bool th�nh PresenceModel ? FAIL SILENT ?
        /// </summary>
        public IDisposable ListenForSessionConflict(string uid, string localSessionId, Action onConflictDetected)
        {
            return firebaseClient
                .Child("Presence")
                .AsObservable<PresenceModel>()
                .Subscribe(d =>
                {
                    if (d.Object == null || string.IsNullOrEmpty(d.Key)) return;

                    // Ch? x? l� event c?a ch�nh user hi?n t?i, b? qua user kh�c
                    if (d.Key != uid) return;

                    var remoteSessionId = d.Object.SessionId;

                    // N?u SessionId tr�n Firebase kh�c v?i SessionId local
                    // ? c� thi?t b? kh�c v?a dang nh?p c�ng t�i kho?n
                    if (!string.IsNullOrEmpty(remoteSessionId)
                        && remoteSessionId != localSessionId)
                    {
                        onConflictDetected?.Invoke();
                    }
                }, error => System.Diagnostics.Debug.WriteLine($"[Session] Listener stopped: {error}"));
        }

        public async Task TransferTableAsync(Table currentTable, Table targetTable, Order? order)
        {
            var updates = new Dictionary<string, object?>
            {
                [$"Tables/table_{currentTable.Id}/Status"] = (int)currentTable.Status,
                [$"Tables/table_{currentTable.Id}/CurrentOrderId"] = currentTable.CurrentOrderId ?? (object)string.Empty,
                [$"Tables/table_{currentTable.Id}/HasOrdered"] = currentTable.HasOrdered,
                [$"Tables/table_{currentTable.Id}/OrderItemCount"] = currentTable.OrderItemCount,
                [$"Tables/table_{currentTable.Id}/OrderTotal"] = currentTable.OrderTotal,
                [$"Tables/table_{currentTable.Id}/ArrivalTime"] = currentTable.ArrivalTime?.ToString("yyyy-MM-ddTHH:mm:ss") ?? string.Empty,

                [$"Tables/table_{targetTable.Id}/Status"] = (int)targetTable.Status,
                [$"Tables/table_{targetTable.Id}/CurrentOrderId"] = targetTable.CurrentOrderId ?? (object)string.Empty,
                [$"Tables/table_{targetTable.Id}/HasOrdered"] = targetTable.HasOrdered,
                [$"Tables/table_{targetTable.Id}/OrderItemCount"] = targetTable.OrderItemCount,
                [$"Tables/table_{targetTable.Id}/OrderTotal"] = targetTable.OrderTotal,
                [$"Tables/table_{targetTable.Id}/ArrivalTime"] = targetTable.ArrivalTime?.ToString("yyyy-MM-ddTHH:mm:ss") ?? string.Empty
            };

            if (order != null)
            {
                updates[$"Orders/order_{order.Id}/TableId"] = order.TableId;
                updates[$"Orders/order_{order.Id}/TableNumber"] = order.TableNumber;
            }

            using var request = new HttpRequestMessage(new HttpMethod("PATCH"), $"{FIREBASE_URL}/.json")
            {
                Content = new StringContent(JsonSerializer.Serialize(updates), Encoding.UTF8, "application/json")
            };
            using var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
        }

        public async Task UpdateTableAsync(Table table)
        {
            var key = $"table_{table.Id}";
            var data = new Dictionary<string, object>
            {
                ["Status"] = (int)table.Status,
                ["Capacity"] = table.Capacity,
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

        public async Task CompletePaymentAsync(Order order, Table table, Invoice invoice)
        {
            var updates = new Dictionary<string, object?>
            {
                [$"Orders/order_{order.Id}/Status"] = (int)OrderStatus.Paid,
                [$"Orders/order_{order.Id}/PaymentMethod"] = (int)invoice.PaymentMethod,
                [$"Orders/order_{order.Id}/Discount"] = order.Discount,

                [$"Tables/table_{table.Id}/Status"] = (int)TableStatus.NeedsClearing,
                [$"Tables/table_{table.Id}/CurrentOrderId"] = string.Empty,
                [$"Tables/table_{table.Id}/HasOrdered"] = false,
                [$"Tables/table_{table.Id}/OrderItemCount"] = 0,
                [$"Tables/table_{table.Id}/OrderTotal"] = string.Empty,

                [$"Invoices/invoice_{invoice.Id}/Id"] = invoice.Id,
                [$"Invoices/invoice_{invoice.Id}/OrderId"] = invoice.OrderId,
                [$"Invoices/invoice_{invoice.Id}/TableNumber"] = invoice.TableNumber,
                [$"Invoices/invoice_{invoice.Id}/ServerName"] = invoice.ServerName,
                [$"Invoices/invoice_{invoice.Id}/CreatedAt"] = invoice.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ss"),
                [$"Invoices/invoice_{invoice.Id}/PaymentMethod"] = (int)invoice.PaymentMethod,
                [$"Invoices/invoice_{invoice.Id}/Discount"] = invoice.Discount,
                [$"Invoices/invoice_{invoice.Id}/Total"] = invoice.Total
            };

            using var request = new HttpRequestMessage(new HttpMethod("PATCH"), $"{FIREBASE_URL}/.json")
            {
                Content = new StringContent(JsonSerializer.Serialize(updates), Encoding.UTF8, "application/json")
            };
            using var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
        }

        public async Task<RevenueUpdateResult> IncrementRevenueAsync(DateTime paidAt, decimal amount)
        {
            var dailyLabel = paidAt.ToString("HH:00");
            var weeklyLabel = paidAt.DayOfWeek switch
            {
                DayOfWeek.Monday => "T2",
                DayOfWeek.Tuesday => "T3",
                DayOfWeek.Wednesday => "T4",
                DayOfWeek.Thursday => "T5",
                DayOfWeek.Friday => "T6",
                DayOfWeek.Saturday => "T7",
                _ => "CN"
            };
            var monthlyLabel = $"Th{paidAt.Month}";

            var dailyTask = IncrementRevenueBucketAsync("Daily", dailyLabel, $"h_{paidAt:HH}", amount);
            var weekIndex = paidAt.DayOfWeek == DayOfWeek.Sunday ? 6 : (int)paidAt.DayOfWeek - 1;
            var weeklyTask = IncrementRevenueBucketAsync("Weekly", weeklyLabel, $"w_{weekIndex}", amount);
            var monthlyTask = IncrementRevenueBucketAsync("Monthly", monthlyLabel, $"m_{paidAt.Month - 1}", amount);

            await Task.WhenAll(dailyTask, weeklyTask, monthlyTask);
            return new RevenueUpdateResult(
                dailyLabel, dailyTask.Result,
                weeklyLabel, weeklyTask.Result,
                monthlyLabel, monthlyTask.Result);
        }

        private async Task<decimal> IncrementRevenueBucketAsync(
            string period, string label, string fallbackKey, decimal amount)
        {
            var buckets = await firebaseClient.Child("Revenue").Child(period).OnceAsync<FirebaseRevenuePointDto>();
            var key = buckets.FirstOrDefault(bucket =>
                string.Equals(bucket.Object?.Label, label, StringComparison.OrdinalIgnoreCase))?.Key
                ?? fallbackKey;
            var url = $"{FIREBASE_URL}/Revenue/{period}/{key}.json";

            for (var attempt = 0; attempt < 6; attempt++)
            {
                using var getRequest = new HttpRequestMessage(HttpMethod.Get, url);
                getRequest.Headers.TryAddWithoutValidation("X-Firebase-ETag", "true");
                using var getResponse = await client.SendAsync(getRequest);
                getResponse.EnsureSuccessStatusCode();

                var json = await getResponse.Content.ReadAsStringAsync();
                var current = string.Equals(json, "null", StringComparison.OrdinalIgnoreCase)
                    ? null
                    : JsonSerializer.Deserialize<FirebaseRevenuePointDto>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                var nextValue = (current?.Value ?? 0) + amount;
                var payload = new FirebaseRevenuePointDto { Label = label, Value = nextValue };
                var etag = getResponse.Headers.TryGetValues("ETag", out var values)
                    ? values.FirstOrDefault()
                    : null;

                using var putRequest = new HttpRequestMessage(HttpMethod.Put, url)
                {
                    Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
                };
                if (!string.IsNullOrEmpty(etag))
                    putRequest.Headers.TryAddWithoutValidation("if-match", etag);

                using var putResponse = await client.SendAsync(putRequest);
                if (putResponse.IsSuccessStatusCode)
                    return nextValue;
                if (putResponse.StatusCode != HttpStatusCode.PreconditionFailed)
                    putResponse.EnsureSuccessStatusCode();
            }

            throw new InvalidOperationException($"Kh�ng th? c?p nh?t doanh thu {period} do c� thay d?i d?ng th?i.");
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
            var key = string.IsNullOrWhiteSpace(item.FirebaseKey)
                ? $"oi_{order.Id}_{item.Id}"
                : item.FirebaseKey;
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
            item.FirebaseKey = key;
        }

        public async Task UpdateOrderItemStatusAsync(Order order, OrderItem item, DishStatus status)
        {
            var key = await ResolveOrderItemKeyAsync(order, item)
                ?? throw new InvalidOperationException("Kh�ng t�m th?y m�n trong don tr�n Firebase.");
            var data = new Dictionary<string, object>
            {
                ["Status"] = (int)status
            };
            await firebaseClient.Child("OrderItems").Child(key).PatchAsync(data);
            item.FirebaseKey = key;
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

        private async Task<string?> ResolveOrderItemKeyAsync(Order order, OrderItem item)
        {
            if (!string.IsNullOrWhiteSpace(item.FirebaseKey))
                return item.FirebaseKey;

            var orderKey = $"order_{order.Id}";
            var allItems = await firebaseClient.Child("OrderItems").OnceAsync<FirebaseOrderItemDto>();
            var match = allItems.FirstOrDefault(entry =>
                entry.Object != null && entry.Object.OrderId == orderKey && entry.Object.Id == item.Id);
            if (match != null)
                item.FirebaseKey = match.Key;
            return match?.Key;
        }

        public async Task DeleteOrderItemAsync(Order order, OrderItem item)
        {
            var key = await ResolveOrderItemKeyAsync(order, item)
                ?? throw new InvalidOperationException("Kh�ng t�m th?y m�n c?n x�a tr�n Firebase.");
            await firebaseClient.Child("OrderItems").Child(key).DeleteAsync();
        }

        public async Task UpdateStaffProfileAsync(string firebaseUid, string name, string email, string phone, string joinDate, string status)
        {
            var data = new Dictionary<string, object>
            {
                ["hoTen"] = name,
                ["email"] = email,
                ["sdt"] = phone,
                ["ngayVaoLam"] = joinDate,
                ["trangThai"] = status
            };
            await firebaseClient.Child("Users").Child(firebaseUid).PatchAsync(data);
        }


        public async Task DeleteInvoiceAsync(string invoiceKey)
        {
            await firebaseClient.Child("Invoices").Child(invoiceKey).DeleteAsync();
        }
    }
}
