using System.Text.Json;

namespace QuanAnApp
{
    public partial class MainPage : ContentPage
    {
        HttpClient _client;

        public MainPage()
        {
            InitializeComponent();
            _client = new HttpClient();
        }

        private async void OnLoadDataClicked(object sender, EventArgs e)
        {
            try
            {
                // Link API xịn sò của bạn đây
                string apiUrl = "https://5v3d5pz2-7043.asse.devtunnels.ms/api/Menu";

                var response = await _client.GetStringAsync(apiUrl);

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var menu = JsonSerializer.Deserialize<List<MonAn>>(response, options);

                MenuListView.ItemsSource = menu;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Lỗi", "Không thể tải dữ liệu: " + ex.Message, "OK");
            }
        }
    }
}