using MotionLink.Models;

namespace MotionLink.Repositories;

public interface IMotionLinkRepository
{
    Task InitialiseAsync(CancellationToken stoppingToken = default);
    Task<List<SessionOverview>> GetSessionsAsync(CancellationToken stoppingToken = default);
    Task<Swing?> GetSwingByIdAsync(int id, CancellationToken stoppingToken = default);
    Task<int> CreateSessionAsync(DateTime dateStart, CancellationToken stoppingToken = default);
    Task<Session?> GetSessionByIdAsync(int id, CancellationToken stoppingToken = default);
    Task<Session?> UpdateSessionAsync(int id, string name, DateTime dateEnd, CancellationToken stoppingToken = default);
    Task<int> DeleteSessionAsync(int sessionId, CancellationToken stoppingToken = default);
    Task<int> DeleteSwingAsync(int swingId, CancellationToken stoppingToken = default);
}