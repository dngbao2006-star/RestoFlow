using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows.Forms;

namespace DemoGoiAPI_FireBase
{
    public partial class Form1 : Form
    {
        private Button btnMoDangNhap, btnMoDangKy, btnLayMenu;
        private RichTextBox rtbKetQua;

        // LINK FIREBASE
        private const string FIREBASE_URL = "https://doanquanan-6a948-default-rtdb.firebaseio.com";

        public Form1()
        {
            InitializeComponent();
            InitializeMainUI();
        }

        // Giao dien chinh
        private void InitializeMainUI()
        {
            this.Text = "Quản Lý Quán Ăn - 100% Cloud Firebase Edition";
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
            rtbKetQua.Text = "Hệ thống đã kết nối với máy chủ Google Firebase.\nBạn có thể thử Đăng nhập bằng tài khoản: admin | 123456";

            this.Controls.AddRange(new Control[] { btnMoDangNhap, btnMoDangKy, btnLayMenu, rtbKetQua });
        }

        // Logic login
        private void ShowLoginDialog()
        {
            Form frmLogin = new Form { Text = "Đăng Nhập Firebase", Size = new Size(350, 230), StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false };
            Label lblUser = new Label { Text = "Tên đăng nhập:", Location = new Point(20, 30), AutoSize = true };
            TextBox txtUser = new TextBox { Location = new Point(130, 27), Size = new Size(180, 25) };
            Label lblPass = new Label { Text = "Mật khẩu:", Location = new Point(20, 70), AutoSize = true };
            TextBox txtPass = new TextBox { Location = new Point(130, 67), Size = new Size(180, 25), UseSystemPasswordChar = true };
            Button btnSubmit = new Button { Text = "Đăng Nhập", Location = new Point(130, 115), Size = new Size(180, 40), BackColor = Color.DodgerBlue, ForeColor = Color.White, Font = new Font("Segoe UI", 9, FontStyle.Bold), FlatStyle = FlatStyle.Flat };
            btnSubmit.FlatAppearance.BorderSize = 0;

            btnSubmit.Click += async (s, e) =>
            {
                if (string.IsNullOrEmpty(txtUser.Text) || string.IsNullOrEmpty(txtPass.Text)) { MessageBox.Show("Vui lòng nhập đủ thông tin!"); return; }
                btnSubmit.Text = "Đang kiểm tra..."; btnSubmit.Enabled = false;

                using (HttpClient client = new HttpClient())
                {
                    try
                    {
                        //Dung query firebase
                        string queryUrl = $"{FIREBASE_URL}/TaiKhoan.json?orderBy=\"TenDangNhap\"&equalTo=\"{txtUser.Text}\"";

                        HttpResponseMessage res = await client.GetAsync(queryUrl);
                        string jsonString = await res.Content.ReadAsStringAsync();

                        //Xu ly dang nhap loi
                        if (jsonString != "null" && jsonString != "{}")
                        {
                            bool isSuccess = false;

                            using (JsonDocument doc = JsonDocument.Parse(jsonString))
                            {
                                JsonElement root = doc.RootElement;

                                //Xu ly truong hop nhieu nguoi cung ten
                                foreach (JsonProperty property in root.EnumerateObject())
                                {
                                    JsonElement tk = property.Value;
                                    string dbPass = tk.GetProperty("MatKhau").GetString();

                                    //check mat khau
                                    if (dbPass == txtPass.Text)
                                    {
                                        isSuccess = true;
                                        break;
                                    }
                                }
                            }

                            if (isSuccess)
                            {
                                MessageBox.Show("Đăng nhập thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                frmLogin.Close();
                                rtbKetQua.Text = $"Xin chào {txtUser.Text}! Đăng nhập thành công bằng Firebase Query cực kỳ bảo mật.";
                            }
                            else
                            {
                                MessageBox.Show("Sai mật khẩu!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                        else
                        {
                            MessageBox.Show("Tài khoản không tồn tại trong hệ thống!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                    catch (Exception ex) { MessageBox.Show("Lỗi kết nối Firebase: " + ex.Message); }
                    finally { btnSubmit.Text = "Đăng Nhập"; btnSubmit.Enabled = true; }
                }
            };
            frmLogin.Controls.AddRange(new Control[] { lblUser, txtUser, lblPass, txtPass, btnSubmit });
            frmLogin.ShowDialog();
        }

        //Logic Dang ki
        private void ShowRegisterDialog()
        {
            Form frmReg = new Form { Text = "Đăng Ký Tài Khoản Mới", Size = new Size(350, 280), StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false };
            Label lblUser = new Label { Text = "Tên đăng nhập:", Location = new Point(20, 30), AutoSize = true };
            TextBox txtUser = new TextBox { Location = new Point(130, 27), Size = new Size(180, 25) };
            Label lblPass = new Label { Text = "Mật khẩu:", Location = new Point(20, 70), AutoSize = true };
            TextBox txtPass = new TextBox { Location = new Point(130, 67), Size = new Size(180, 25), UseSystemPasswordChar = true };
            Label lblEmail = new Label { Text = "Email:", Location = new Point(20, 110), AutoSize = true };
            TextBox txtEmail = new TextBox { Location = new Point(130, 107), Size = new Size(180, 25) };
            Button btnSubmit = new Button { Text = "Đăng Ký Ngay", Location = new Point(130, 155), Size = new Size(180, 40), BackColor = Color.DarkOrange, ForeColor = Color.White, Font = new Font("Segoe UI", 9, FontStyle.Bold), FlatStyle = FlatStyle.Flat };
            btnSubmit.FlatAppearance.BorderSize = 0;

            btnSubmit.Click += async (s, e) =>
            {
                if (string.IsNullOrEmpty(txtUser.Text) || string.IsNullOrEmpty(txtPass.Text)) { MessageBox.Show("Vui lòng nhập đủ thông tin!"); return; }
                btnSubmit.Text = "Đang tạo..."; btnSubmit.Enabled = false;

                using (HttpClient client = new HttpClient())
                {
                    try
                    {
                        //Tao user moi
                        var newUser = new { TenDangNhap = txtUser.Text, MatKhau = txtPass.Text, Email = txtEmail.Text, Quyen = "NhanVien", DaXacThucEmail = 0 };
                        var content = new StringContent(JsonSerializer.Serialize(newUser), Encoding.UTF8, "application/json");

                        //Su dung post de push len Firebase
                        HttpResponseMessage res = await client.PostAsync($"{FIREBASE_URL}/TaiKhoan.json", content);

                        if (res.IsSuccessStatusCode)
                        {
                            MessageBox.Show("Tạo tài khoản thành công! Bạn có thể đăng nhập ngay.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            frmReg.Close();
                        }
                    }
                    catch (Exception ex) { MessageBox.Show("Lỗi kết nối Firebase: " + ex.Message); }
                    finally { btnSubmit.Text = "Đăng Ký Ngay"; btnSubmit.Enabled = true; }
                }
            };
            frmReg.Controls.AddRange(new Control[] { lblUser, txtUser, lblPass, txtPass, lblEmail, txtEmail, btnSubmit });
            frmReg.ShowDialog();
        }
        //Logic Lay Menu
        private async void BtnLayMenu_Click(object sender, EventArgs e)
        {
            rtbKetQua.Text = "⏳ Đang tải dữ liệu thực đơn trực tiếp từ Firebase Cloud...";
            btnLayMenu.Enabled = false;

            using (HttpClient client = new HttpClient())
            {
                try
                {
                    HttpResponseMessage res = await client.GetAsync($"{FIREBASE_URL}/Menu.json");
                    string result = await res.Content.ReadAsStringAsync();
                    FormatJsonOutput(result, (int)res.StatusCode, res.IsSuccessStatusCode);
                }
                catch (Exception ex) { rtbKetQua.Text = $"🔴 Lỗi kết nối: {ex.Message}"; }
                finally { btnLayMenu.Enabled = true; }
            }
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