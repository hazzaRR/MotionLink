using SQLite;
using MotionLink.Models;
using Microsoft.Extensions.Logging;

namespace MotionLink.Repositories;

public class MotionLinkRepository : IMotionLinkRepository
{
    private readonly ILogger<MotionLinkRepository> _logger;
    private readonly string _dbPath = Path.Combine(FileSystem.AppDataDirectory, "motionLink.db");
    private SQLiteAsyncConnection? _database;

    public MotionLinkRepository(ILogger<MotionLinkRepository> logger)
    {
        _logger = logger;
    }

    public async Task<int> CreateSessionAsync(DateTime dateStart, CancellationToken stoppingToken = default)
    {
        await InitialiseAsync(stoppingToken);
        var session = new Session { DateStart = dateStart };
        await _database!.InsertAsync(session);
        return session.Id;
    }

    public async Task<int> DeleteSessionAsync(int sessionId, CancellationToken stoppingToken = default)
    {
        await InitialiseAsync(stoppingToken);
        return await _database!.DeleteAsync<Session>(sessionId);
    }

    public async Task<int> DeleteSwingAsync(int swingId, CancellationToken stoppingToken = default)
    {
        await InitialiseAsync(stoppingToken);
        return await _database!.DeleteAsync<Swing>(swingId);
    }

    public async Task<Session?> GetSessionByIdAsync(int id, CancellationToken stoppingToken = default)
    {
        await InitialiseAsync(stoppingToken);
        var session = await _database!.Table<Session>().FirstOrDefaultAsync(s => s.Id == id);
        if (session != null)
        {
            session.Swings = await _database.Table<Swing>()
                                            .Where(s => s.SessionId == id)
                                            .ToListAsync();
        }
        return session;
    }

    public async Task<List<SessionOverview>> GetSessionsAsync(CancellationToken stoppingToken = default)
    {
        await InitialiseAsync(stoppingToken);
        var sql = @"
            SELECT s.Id, s.Name, s.DateStart, COUNT(sw.Id) as SwingCount 
            FROM Session s 
            LEFT JOIN Swing sw ON sw.SessionId = s.Id 
            GROUP BY s.Id";

        return await _database!.QueryAsync<SessionOverview>(sql);
    }

    public async Task<Swing?> GetSwingByIdAsync(int id, CancellationToken stoppingToken = default)
    {
        await InitialiseAsync(stoppingToken);
        Swing? swing = await _database!.FindAsync<Swing>(id);
        if (swing == null)
            return null;

        swing.Data = await _database.Table<ImuPacket>()
                              .Where(p => p.SwingId == id)
                              .ToListAsync();

        return swing;
    }

    public async Task InitialiseAsync(CancellationToken stoppingToken = default)
    {
        if (_database is not null) return;

        _database = new SQLiteAsyncConnection(_dbPath);
        await _database.ExecuteScalarAsync<string>("PRAGMA foreign_keys = ON;");

        await _database.CreateTableAsync<Session>();

        await _database.ExecuteAsync(@"
            CREATE TABLE IF NOT EXISTS Swing (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                SessionId INTEGER,
                PeakGForce REAL,
                PeakRotation REAL,
                FOREIGN KEY(SessionId) REFERENCES Session(Id) ON DELETE CASCADE
            );");

        await _database.ExecuteAsync(@"
            CREATE TABLE IF NOT EXISTS ImuPacket (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                SwingId INTEGER,
                Ax REAL, Ay REAL, Az REAL,
                Gx REAL, Gy REAL, Gz REAL,
                Timestamp DATETIME,
                FOREIGN KEY(SwingId) REFERENCES Swing(Id) ON DELETE CASCADE
            );");
    }

    public async Task<Session?> UpdateSessionAsync(int id, string name, DateTime dateEnd, CancellationToken stoppingToken = default)
    {
        await InitialiseAsync(stoppingToken);
        Session? session = await _database!.FindAsync<Session>(id);
        if (session == null)
        {
            return null;            
        }

        session.Name = name;
        session.DateEnd = dateEnd;

        await _database!.UpdateAsync(session);
        return session;
    }
}