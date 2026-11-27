using SQLite;
using MotionLink.Models;
using Microsoft.Extensions.Logging;

namespace MotionLink.Repositories;

public class MotionLinkRepository : IMotionLinkRepository
{
    private readonly ILogger<MotionLinkRepository> _logger;
    private readonly string _dbPath = Path.Combine(FileSystem.AppDataDirectory, "motionLink.db");
    private SQLiteAsyncConnection? _database;
    private readonly SemaphoreSlim _initializationGate = new(1, 1);

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

    public async Task<int> CreateSwingAsync(int sessionId, double peakGForce, double PeakRotation, List<ImuPacket> data, CancellationToken stoppingToken = default)
    {
        await InitialiseAsync(stoppingToken);
        Swing swing = new() { SessionId = sessionId, PeakGForce = peakGForce, PeakRotation = PeakRotation };
        await _database!.InsertAsync(swing);

        foreach (ImuPacket packet in data)
        {
            packet.SwingId = swing.Id;
        }

        await _database!.InsertAllAsync(data);
        return swing.Id;
    }

    public async Task<List<Club>> GetBagClubsAsync()
    {
        await InitialiseAsync();
        return await _database!.Table<Club>().Where(club => club.IsInBag).OrderBy(club => club.Id).ToListAsync();
    }

    public async Task SaveDetectedSwingAsync(Swing swing)
    {
        await InitialiseAsync();
        await _database!.RunInTransactionAsync(connection =>
        {
            connection.Insert(swing);
            foreach (var packet in swing.Data)
                packet.SwingId = swing.Id;

            connection.InsertAll(swing.Data, runInTransaction: false);
        });
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

    public async Task<List<ImuPacket>> GetRawSwingDataAsync(int swingId, CancellationToken stoppingToken = default)
    {
        await InitialiseAsync(stoppingToken);
        return await _database!.Table<ImuPacket>()
                    .Where(s => s.SwingId == swingId)
                    .OrderBy(s => s.Id)
                    .ToListAsync();
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
        try
        {
            await InitialiseAsync(stoppingToken);
            var sql = @"
                SELECT s.Id, s.Name, s.DateStart, s.DateEnd, COUNT(sw.Id) as SwingCount 
                FROM Sessions s 
                LEFT JOIN Swings sw ON sw.SessionId = s.Id 
                GROUP BY s.Id
                ORDER BY s.DateStart DESC, s.Id DESC";

            return await _database!.QueryAsync<SessionOverview>(sql);

        }
        catch (Exception ex)
        {
            _logger.LogError("SQLITE ERROR: " + ex);
            throw;
        }

    }

    public async Task<Swing?> GetSwingByIdAsync(int id, CancellationToken stoppingToken = default)
    {
        await InitialiseAsync(stoppingToken);
        Swing? swing = await _database!.FindAsync<Swing>(id);
        if (swing == null)
            return null;

        swing.Data = await _database.Table<ImuPacket>()
                              .Where(p => p.SwingId == id)
                              .OrderBy(p => p.Id)
                              .ToListAsync();

        return swing;
    }

    public async Task InitialiseAsync(CancellationToken stoppingToken = default)
    {
        await _initializationGate.WaitAsync(stoppingToken);
        try
        {
            if (_database is not null) return;

            var database = new SQLiteAsyncConnection(_dbPath);
            await database.ExecuteScalarAsync<string>("PRAGMA foreign_keys = ON;");

            await database.CreateTableAsync<Session>();

            await database.ExecuteAsync(@"
                CREATE TABLE IF NOT EXISTS Swings (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    SessionId INTEGER,
                    PeakGForce REAL,
                    PeakRotation REAL,
                    FOREIGN KEY(SessionId) REFERENCES Sessions(Id) ON DELETE CASCADE
                );");

            await database.ExecuteAsync(@"
                CREATE TABLE IF NOT EXISTS ImuPackets (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    SwingId INTEGER,
                    Ax REAL, Ay REAL, Az REAL,
                    Gx REAL, Gy REAL, Gz REAL,
                    Qw REAL, Qx REAL, Qy REAL, Qz REAL,
                    Impact INTEGER,
                    Timestamp DATETIME,
                    FOREIGN KEY(SwingId) REFERENCES Swings(Id) ON DELETE CASCADE
                );");

            // Add the new columns to existing installations and retain the foreign keys above.
            await database.CreateTableAsync<Swing>();
            await database.CreateTableAsync<ImuPacket>();
            await database.CreateTableAsync<Club>();
            await database.RunInTransactionAsync(connection =>
            {
                if (connection.Table<Club>().Count() != 0) return;

                connection.InsertAll(new Club[]
                {
                    new() { Name = "Driver", Category = "Wood", ShaftLengthInches = 45.5 },
                    new() { Name = "3-Wood", Category = "Wood", ShaftLengthInches = 43 },
                    new() { Name = "4-Iron", Category = "Iron", ShaftLengthInches = 38.5 },
                    new() { Name = "5-Iron", Category = "Iron", ShaftLengthInches = 38 },
                    new() { Name = "6-Iron", Category = "Iron", ShaftLengthInches = 37.5 },
                    new() { Name = "7-Iron", Category = "Iron", ShaftLengthInches = 37 },
                    new() { Name = "8-Iron", Category = "Iron", ShaftLengthInches = 36.5 },
                    new() { Name = "9-Iron", Category = "Iron", ShaftLengthInches = 36 },
                    new() { Name = "Pitching Wedge", Category = "Wedge", ShaftLengthInches = 35.75 },
                    new() { Name = "Gap Wedge", Category = "Wedge", ShaftLengthInches = 35.5 },
                    new() { Name = "Sand Wedge", Category = "Wedge", ShaftLengthInches = 35.25 },
                    new() { Name = "Lob Wedge", Category = "Wedge", ShaftLengthInches = 35 },
                    new() { Name = "Putter", Category = "Putter", ShaftLengthInches = 34 }
                }, runInTransaction: false);
            });
            _database = database;
        }
        finally
        {
            _initializationGate.Release();
        }
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
