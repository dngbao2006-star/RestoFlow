using AppManagermentRestaurant.Models;

namespace AppManagermentRestaurant.Services;

public interface IDataStore
{
    Task SeedAsync(AppContext context);
}
