namespace TextExtraction.Services
{
    public interface IAPIService
    {
        public Task<TResponse?> GetAsync<TResponse>(string url);
        public Task<TResponse?> PostAsync<TRequest, TResponse>(string url, TRequest requestData);
    }
}
