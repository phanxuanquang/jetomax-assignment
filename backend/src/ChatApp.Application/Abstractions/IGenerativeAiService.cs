namespace ChatApp.Application.Abstractions;

public interface IGenerativeAiService
{
    Task<int> CountTokensAsync(string text, CancellationToken cancellationToken = default);
    Task<T> GenerateContentAsync<T>(string prompt, double temp = 1.0, CancellationToken cancellationToken = default);
    Task<T> GenerateContentFromImageAsync<T>(string prompt, byte[] imageAsBytes, double temp = 1.0, CancellationToken cancellationToken = default);
    Task<T> GenerateContentFromImageAsync<T>(string prompt, string imageUrl, double temp = 1.0, CancellationToken cancellationToken = default);
}