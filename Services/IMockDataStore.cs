using AppManagermentRestaurant.Models;

namespace AppManagermentRestaurant.Services;

public interface IMockDataStore
{
    void Seed(AppContext context);
}
