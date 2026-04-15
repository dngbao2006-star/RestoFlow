#nullable disable // Tắt cảnh báo null khó tính của các bản .NET mới
using System;
using System.Drawing;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace QuanLyQuanAn_Modern
{
    public partial class Form1 : Form
    {
        private Button btnMoDangNhap, btnMoDangKy, btnLayMenu;
        private RichTextBox rtbKetQua;

        // 1. LINK DATABASE
        private const string FIREBASE_URL = "https://doanquanan-6a948-default-rtdb.firebaseio.com";

        // 2. WEB API KEY
        private const string WEB_API_KEY = "AIzaSyCxLsmz6jRmvIgyZywfNm9GVUeqm3ULKOs";

        // URL Gốc của Google Identity Toolkit (Dùng để Đăng nhập / Đăng ký)
        private const string GOOGLE_AUTH_URL = "https://identitytoolkit.googleapis.com/v1/accounts";

        // Dùng chung 1 HttpClient cho toàn ứng dụng để tối ưu hiệu năng
        private static readonly HttpClient client = new HttpClient();

        public Form1()
        {
            InitializeComponent();
            InitializeMainUI();
        }

        // 1. GIAO DIỆN CHÍNH

        private void InitializeMainUI()
        {
            this.Text = "Quản Lý Quán Ăn - Modern .NET Edition";
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.White;

            btnMoDangNhap = new Button { Text = "🔑 Đăng Nhập", Location = new Point(20, 20), Size = new Size(150, 45), Font = new Font("Segoe UI", 10, FontStyle.Bold), BackColor = Color.DodgerBlue, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnMoDangNhap.FlatAppearance.BorderSize = 0;
            btnMoDangNhap.Click += (s, e) => ShowLoginDialog();

            btnMoDangKy = new Button { Text = "📝 Đăng Ký", Location = new Point(180, 20), Size = new Size(150, 45), Font = new Font("Segoe UI", 10, FontStyle.Bold), BackColor = Color.DarkOrange, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnMoDangKy.FlatAppearance.BorderSize = 0;
            btnMoDangKy.Click += (s, e) => ShowRegisterDialog();

            btnLayMenu = new Button { Text = "🔥 Tải Menu Từ Firebase", Location = new Point(340, 20), Size = new Size(420, 45), Font = new Font("Segoe UI", 10, FontStyle.Bold), BackColor = Color.OrangeRed, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnLayMenu.FlatAppearance.BorderSize = 0;
            btnLayMenu.Click += BtnLayMenu_Click;

            rtbKetQua = new RichTextBox { Location = new Point(20, 80), Size = new Size(740, 450), Font = new Font("Consolas", 10, FontStyle.Regular), BackColor = Color.WhiteSmoke, ReadOnly = true };
            rtbKetQua.Text = "Hệ thống đã chạy trên nền tảng .NET mới nhất!\nKhông cần cài thư viện bên thứ 3. Tốc độ gọi API cực nhanh.";

            this.Controls.AddRange(new Control[] { btnMoDangNhap, btnMoDangKy, btnLayMenu, rtbKetQua });
        }


        // 2. LOGIC ĐĂNG NHẬP (GỌI API AUTH CỦA GOOGLE GỐC)

        private void ShowLoginDialog()
        {
            Form frmLogin = new Form { Text = "Đăng Nhập Hiện Đại", Size = new Size(350, 230), StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false };

            Label lblEmail = new Label { Text = "Email:", Location = new Point(20, 30), AutoSize = true };
            TextBox txtEmail = new TextBox { Location = new Point(100, 27), Size = new Size(210, 25) };

            Label lblPass = new Label { Text = "Mật khẩu:", Location = new Point(20, 70), AutoSize = true };
            TextBox txtPass = new TextBox { Location = new Point(100, 67), Size = new Size(210, 25), UseSystemPasswordChar = true };

            Button btnSubmit = new Button { Text = "Đăng Nhập", Location = new Point(100, 115), Size = new Size(210, 40), BackColor = Color.DodgerBlue, ForeColor = Color.White, Font = new Font("Segoe UI", 9, FontStyle.Bold), FlatStyle = FlatStyle.Flat };
            btnSubmit.FlatAppearance.BorderSize = 0;

            btnSubmit.Click += async (s, e) =>
            {
                if (string.IsNullOrEmpty(txtEmail.Text) || string.IsNullOrEmpty(txtPass.Text)) { MessageBox.Show("Vui lòng nhập Email và Mật khẩu!"); return; }
                btnSubmit.Text = "Đang xác thực..."; btnSubmit.Enabled = false;

                try
                {
                    // Bước A: Gửi request Đăng nhập thẳng lên máy chủ Google
                    string authEndpoint = $"{GOOGLE_AUTH_URL}:signInWithPassword?key={WEB_API_KEY}";
                    var payload = new { email = txtEmail.Text, password = txtPass.Text, returnSecureToken = true };
                    string jsonPayload = JsonSerializer.Serialize(payload);
                    var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                    HttpResponseMessage authRes = await client.PostAsync(authEndpoint, content);
                    string authResult = await authRes.Content.ReadAsStringAsync();

                    if (authRes.IsSuccessStatusCode)
                    {
                        // Lấy mã UID và Token từ kết quả trả về
                        using JsonDocument doc = JsonDocument.Parse(authResult);
                        string uid = doc.RootElement.GetProperty("localId").GetString();
                        string token = doc.RootElement.GetProperty("idToken").GetString();

                        // Bước B: Dùng mã UID để chui vào Database lấy Quyền hạn
                        string dbUrl = $"{FIREBASE_URL}/Users/{uid}.json?auth={token}";
                        HttpResponseMessage dbRes = await client.GetAsync(dbUrl);
                        string dbResult = await dbRes.Content.ReadAsStringAsync();

                        if (dbResult != "null")
                        {
                            using JsonDocument dbDoc = JsonDocument.Parse(dbResult);
                            string hoTen = dbDoc.RootElement.GetProperty("hoTen").GetString();
                            string quyen = dbDoc.RootElement.GetProperty("quyen").GetString();
                            string trangThai = dbDoc.RootElement.GetProperty("trangThai").GetString();

                            if (trangThai == "TamKhoa")
                            {
                                MessageBox.Show("Tài khoản của bạn đã bị khóa!", "Từ chối", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                return;
                            }

                            MessageBox.Show($"Đăng nhập thành công!\nXin chào {hoTen} ({quyen})", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            frmLogin.Close();
                            rtbKetQua.Text = $"[TRUY CẬP HỢP LỆ]\nNgười dùng: {hoTen}\nQuyền: {quyen}\nMã UID: {uid}";
                            rtbKetQua.ForeColor = Color.Green;
                        }
                        else
                        {
                            MessageBox.Show("Đăng nhập đúng nhưng không tìm thấy dữ liệu phân quyền!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    else
                    {
                        MessageBox.Show("Sai Email hoặc Mật khẩu!", "Lỗi xác thực", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                catch (Exception ex) { MessageBox.Show("Lỗi hệ thống: " + ex.Message); }
                finally { btnSubmit.Text = "Đăng Nhập"; btnSubmit.Enabled = true; }
            };

            frmLogin.Controls.AddRange(new Control[] { lblEmail, txtEmail, lblPass, txtPass, btnSubmit });
            frmLogin.ShowDialog();
        }


        // 3. LOGIC ĐĂNG KÝ (GỌI API TẠO USER GỐC)

        private void ShowRegisterDialog()
        {
            Form frmReg = new Form { Text = "Tạo Nhân Viên Mới", Size = new Size(350, 280), StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false };

            Label lblEmail = new Label { Text = "Email:", Location = new Point(20, 30), AutoSize = true };
            TextBox txtEmail = new TextBox { Location = new Point(100, 27), Size = new Size(210, 25) };

            Label lblPass = new Label { Text = "Mật khẩu:", Location = new Point(20, 70), AutoSize = true };
            TextBox txtPass = new TextBox { Location = new Point(100, 67), Size = new Size(210, 25), UseSystemPasswordChar = true };

            Label lblName = new Label { Text = "Họ tên:", Location = new Point(20, 110), AutoSize = true };
            TextBox txtName = new TextBox { Location = new Point(100, 107), Size = new Size(210, 25) };

            Button btnSubmit = new Button { Text = "Tạo Tài Khoản", Location = new Point(100, 155), Size = new Size(210, 40), BackColor = Color.DarkOrange, ForeColor = Color.White, Font = new Font("Segoe UI", 9, FontStyle.Bold), FlatStyle = FlatStyle.Flat };
            btnSubmit.FlatAppearance.BorderSize = 0;

            btnSubmit.Click += async (s, e) =>
            {
                if (string.IsNullOrEmpty(txtEmail.Text) || string.IsNullOrEmpty(txtPass.Text) || string.IsNullOrEmpty(txtName.Text)) { MessageBox.Show("Vui lòng nhập đủ thông tin!"); return; }
                btnSubmit.Text = "Đang tạo..."; btnSubmit.Enabled = false;

                try
                {
                    // Bước A: Gọi API tạo tài khoản Auth
                    string signUpEndpoint = $"{GOOGLE_AUTH_URL}:signUp?key={WEB_API_KEY}";
                    var payload = new { email = txtEmail.Text, password = txtPass.Text, returnSecureToken = true };
                    var authContent = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                    HttpResponseMessage authRes = await client.PostAsync(signUpEndpoint, authContent);
                    string authResult = await authRes.Content.ReadAsStringAsync();

                    if (authRes.IsSuccessStatusCode)
                    {
                        // Lấy mã UID để tạo hồ sơ
                        using JsonDocument doc = JsonDocument.Parse(authResult);
                        string newUid = doc.RootElement.GetProperty("localId").GetString();
                        string token = doc.RootElement.GetProperty("idToken").GetString();

                        // Bước B: Tạo hồ sơ cho nhân viên trên Realtime Database
                        var newUserProfile = new { hoTen = txtName.Text, email = txtEmail.Text, quyen = "NhanVien", trangThai = "HoatDong" };
                        var dbContent = new StringContent(JsonSerializer.Serialize(newUserProfile), Encoding.UTF8, "application/json");

                        // Dùng PUT để ghi đè vào đúng nhánh UID
                        HttpResponseMessage dbRes = await client.PutAsync($"{FIREBASE_URL}/Users/{newUid}.json?auth={token}", dbContent);

                        if (dbRes.IsSuccessStatusCode)
                        {
                            MessageBox.Show("Tạo tài khoản thành công! Đã cấp quyền Nhân viên.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            frmReg.Close();
                        }
                    }
                    else
                    {
                        MessageBox.Show("Lỗi tạo tài khoản (Email đã tồn tại hoặc Pass dưới 6 ký tự).", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                catch (Exception ex) { MessageBox.Show("Lỗi hệ thống: " + ex.Message); }
                finally { btnSubmit.Text = "Tạo Tài Khoản"; btnSubmit.Enabled = true; }
            };

            frmReg.Controls.AddRange(new Control[] { lblEmail, txtEmail, lblPass, txtPass, lblName, txtName, btnSubmit });
            frmReg.ShowDialog();
        }


        // 4. LOGIC LẤY MENU

        private async void BtnLayMenu_Click(object sender, EventArgs e)
        {
            rtbKetQua.Text = "⏳ Đang tải dữ liệu thực đơn trực tiếp từ Firebase Cloud...";
            btnLayMenu.Enabled = false;

            try
            {
                HttpResponseMessage res = await client.GetAsync($"{FIREBASE_URL}/Menu.json");
                string result = await res.Content.ReadAsStringAsync();
                FormatJsonOutput(result, (int)res.StatusCode, res.IsSuccessStatusCode);
            }
            catch (Exception ex) { rtbKetQua.Text = $"🔴 Lỗi kết nối: {ex.Message}"; }
            finally { btnLayMenu.Enabled = true; }
        }

        private void FormatJsonOutput(string rawJson, int statusCode, bool isSuccess)
        {
            string statusText = $"[Trạng thái Firebase: HTTP {statusCode}]\n";
            string separator = "--------------------------------------------------\n\n";
            try
            {
                var jsonElement = JsonSerializer.Deserialize<JsonElement>(rawJson);
                var options = new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
                rtbKetQua.Text = statusText + separator + JsonSerializer.Serialize(jsonElement, options);
                rtbKetQua.ForeColor = isSuccess ? Color.Black : Color.Red;
            }
            catch
            {
                rtbKetQua.Text = statusText + separator + rawJson;
                rtbKetQua.ForeColor = Color.Red;
            }
        }
    }
}