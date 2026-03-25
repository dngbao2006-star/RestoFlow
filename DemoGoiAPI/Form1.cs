using System;
using System.Drawing;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows.Forms;

namespace DemoGoiAPI 
{
    public partial class Form1 : Form
    {
        // GIAO DIEN MAN HINH CHINH
        private Button btnMoDangNhap, btnMoDangKy, btnLayMenu;
        private RichTextBox rtbKetQua;

        //CAU HINH URL API
        private const string API_BASE_URL = "https://xv6zsdk0-7294.asse.devtunnels.ms";

        public Form1()
        {
            InitializeComponent();
            InitializeMainUI();
        }

        // TAO GIAO DIEN MAN HINH CHINH
        private void InitializeMainUI()
        {
            this.Text = "Phần Mềm Quản Lý Quán Ăn v2.0 - Pro Edition";
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Padding = new Padding(10);
            this.BackColor = Color.White;

            //NUT DANG NHAP
            btnMoDangNhap = new Button { Text = "🔑 Đăng Nhập", Location = new Point(20, 20), Size = new Size(150, 45), Font = new Font("Segoe UI", 10, FontStyle.Bold), BackColor = Color.DodgerBlue, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnMoDangNhap.FlatAppearance.BorderSize = 0;
            btnMoDangNhap.Click += (s, e) => ShowLoginDialog(); // Bấm thì mở cửa sổ Đăng nhập

            //NUT DANG KI
            btnMoDangKy = new Button { Text = "📝 Đăng Ký", Location = new Point(180, 20), Size = new Size(150, 45), Font = new Font("Segoe UI", 10, FontStyle.Bold), BackColor = Color.DarkOrange, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnMoDangKy.FlatAppearance.BorderSize = 0;
            btnMoDangKy.Click += (s, e) => ShowRegisterDialog(); // Bấm thì mở cửa sổ Đăng ký

            //NUT MO MENU
            btnLayMenu = new Button { Text = "📜 Tải Danh Sách Thực Đơn", Location = new Point(340, 20), Size = new Size(420, 45), Font = new Font("Segoe UI", 10, FontStyle.Bold), BackColor = Color.ForestGreen, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnLayMenu.FlatAppearance.BorderSize = 0;
            btnLayMenu.Click += BtnLayMenu_Click;

            //KHUNG HIEN THI KET QUA
            rtbKetQua = new RichTextBox { Location = new Point(20, 80), Size = new Size(740, 450), Font = new Font("Consolas", 10, FontStyle.Regular), BackColor = Color.WhiteSmoke, ReadOnly = true };
            rtbKetQua.Text = "Hệ thống đã sẵn sàng.\nBạn có thể bấm Đăng nhập, Đăng ký hoặc Tải thực đơn...";

            this.Controls.AddRange(new Control[] { btnMoDangNhap, btnMoDangKy, btnLayMenu, rtbKetQua });
        }

        //TAO CUA SO DANG NHAP
        private void ShowLoginDialog()
        {
            //TAO FORM MOI LAM CUA SO LOGIN
            Form frmLogin = new Form { Text = "Xác thực người dùng", Size = new Size(350, 230), StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false };

            Label lblUser = new Label { Text = "Tên đăng nhập:", Location = new Point(20, 30), AutoSize = true };
            TextBox txtUser = new TextBox { Location = new Point(130, 27), Size = new Size(180, 25) };

            Label lblPass = new Label { Text = "Mật khẩu:", Location = new Point(20, 70), Size = new Size(100, 20) };
            TextBox txtPass = new TextBox { Location = new Point(130, 67), Size = new Size(180, 25), UseSystemPasswordChar = true };

            Button btnSubmit = new Button { Text = "Đăng Nhập", Location = new Point(130, 115), Size = new Size(180, 40), BackColor = Color.DodgerBlue, ForeColor = Color.White, Font = new Font("Segoe UI", 9, FontStyle.Bold), FlatStyle = FlatStyle.Flat };
            btnSubmit.FlatAppearance.BorderSize = 0;

            //XY LY LOGIN
            btnSubmit.Click += async (s, e) =>
            {
                if (string.IsNullOrEmpty(txtUser.Text) || string.IsNullOrEmpty(txtPass.Text)) { MessageBox.Show("Vui lòng nhập đủ thông tin!"); return; }

                btnSubmit.Text = "Đang xử lý..."; btnSubmit.Enabled = false;

                using (HttpClient client = new HttpClient())
                {
                    try
                    {
                        client.DefaultRequestHeaders.Add("X-Tunnel-Skip-Antiphishing-Page", "true");
                        var loginData = new { tenDangNhap = txtUser.Text, matKhau = txtPass.Text };
                        var content = new StringContent(JsonSerializer.Serialize(loginData), Encoding.UTF8, "application/json");

                        HttpResponseMessage res = await client.PostAsync($"{API_BASE_URL}/api/TaiKhoan/DangNhap", content);
                        string result = await res.Content.ReadAsStringAsync();
                        FormatJsonOutput(result, (int)res.StatusCode, res.IsSuccessStatusCode);

                        if (res.IsSuccessStatusCode)
                        {
                            MessageBox.Show("Đăng nhập thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            frmLogin.Close();
                        }
                    }
                    catch (Exception ex) { MessageBox.Show("Lỗi: " + ex.Message); }
                    finally { btnSubmit.Text = "Đăng Nhập"; btnSubmit.Enabled = true; }
                }
            };

            frmLogin.Controls.AddRange(new Control[] { lblUser, txtUser, lblPass, txtPass, btnSubmit });
            frmLogin.ShowDialog();
        }

        // TAO CUA SO REGISTER
        private void ShowRegisterDialog()
        {
            Form frmReg = new Form { Text = "Đăng Ký Tài Khoản Mới", Size = new Size(350, 280), StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false };

            Label lblUser = new Label { Text = "Tên đăng nhập:", Location = new Point(20, 30), AutoSize = true };
            TextBox txtUser = new TextBox { Location = new Point(130, 27), Size = new Size(180, 25) };

            Label lblPass = new Label { Text = "Mật khẩu:", Location = new Point(20, 70), Size = new Size(100, 20) };
            TextBox txtPass = new TextBox { Location = new Point(130, 67), Size = new Size(180, 25), UseSystemPasswordChar = true };

            Label lblEmail = new Label { Text = "Email:", Location = new Point(20, 110), Size = new Size(100, 20) };
            TextBox txtEmail = new TextBox { Location = new Point(130, 107), Size = new Size(180, 25) };

            Button btnSubmit = new Button { Text = "Đăng Ký Ngay", Location = new Point(130, 155), Size = new Size(180, 40), BackColor = Color.DarkOrange, ForeColor = Color.White, Font = new Font("Segoe UI", 9, FontStyle.Bold), FlatStyle = FlatStyle.Flat };
            btnSubmit.FlatAppearance.BorderSize = 0;

            btnSubmit.Click += async (s, e) =>
            {
                if (string.IsNullOrEmpty(txtUser.Text) || string.IsNullOrEmpty(txtPass.Text) || string.IsNullOrEmpty(txtEmail.Text)) { MessageBox.Show("Vui lòng nhập đủ thông tin!"); return; }

                btnSubmit.Text = "Đang xử lý..."; btnSubmit.Enabled = false;

                using (HttpClient client = new HttpClient())
                {
                    try
                    {
                        client.DefaultRequestHeaders.Add("X-Tunnel-Skip-Antiphishing-Page", "true");
                        var regData = new { tenDangNhap = txtUser.Text, matKhau = txtPass.Text, email = txtEmail.Text };
                        var content = new StringContent(JsonSerializer.Serialize(regData), Encoding.UTF8, "application/json");

                        HttpResponseMessage res = await client.PostAsync($"{API_BASE_URL}/api/TaiKhoan/DangKy", content);
                        string result = await res.Content.ReadAsStringAsync();
                        FormatJsonOutput(result, (int)res.StatusCode, res.IsSuccessStatusCode);

                        if (res.IsSuccessStatusCode)
                        {
                            MessageBox.Show("Tạo tài khoản thành công! Bạn có thể đăng nhập ngay.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            frmReg.Close(); 
                        }
                    }
                    catch (Exception ex) { MessageBox.Show("Lỗi: " + ex.Message); }
                    finally { btnSubmit.Text = "Đăng Ký Ngay"; btnSubmit.Enabled = true; }
                }
            };

            frmReg.Controls.AddRange(new Control[] { lblUser, txtUser, lblPass, txtPass, lblEmail, txtEmail, btnSubmit });
            frmReg.ShowDialog();
        }

        //API GET: TAI MENU VA HIEN THI MON AN
        private async void BtnLayMenu_Click(object sender, EventArgs e)
        {
            rtbKetQua.Text = "⏳ Đang tải dữ liệu thực đơn từ máy chủ...";
            btnLayMenu.Enabled = false;

            using (HttpClient client = new HttpClient())
            {
                try
                {
                    client.DefaultRequestHeaders.Add("X-Tunnel-Skip-Antiphishing-Page", "true");
                    HttpResponseMessage res = await client.GetAsync($"{API_BASE_URL}/api/Menu");
                    string result = await res.Content.ReadAsStringAsync();
                    FormatJsonOutput(result, (int)res.StatusCode, res.IsSuccessStatusCode);
                }
                catch (Exception ex) { rtbKetQua.Text = $"🔴 Lỗi kết nối: {ex.Message}"; }
                finally { btnLayMenu.Enabled = true; }
            }
        }

        //HAM HIEN THI TIENG VIET
        private void FormatJsonOutput(string rawJson, int statusCode, bool isSuccess)
        {
            string statusText = $"[Trạng thái máy chủ: HTTP {statusCode}]\n";
            string separator = "--------------------------------------------------\n\n";
            try
            {
                var jsonElement = JsonSerializer.Deserialize<JsonElement>(rawJson);
                var options = new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
                string formattedJson = JsonSerializer.Serialize(jsonElement, options);

                rtbKetQua.Text = statusText + separator + formattedJson;
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