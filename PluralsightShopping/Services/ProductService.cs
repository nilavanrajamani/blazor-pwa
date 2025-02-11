using PluralsightShopping.Shared;
using System.Net.Http.Json;

namespace PluralsightShopping.Services
{
    public class ProductService
    {
        private HttpClient _httpClient;

        public ProductService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<IEnumerable<Product>> GetProducts()
        {
            return await _httpClient.GetFromJsonAsync<Product[]>("products");
        }
    }
}
