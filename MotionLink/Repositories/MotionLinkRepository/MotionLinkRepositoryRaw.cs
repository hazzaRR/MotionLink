// using Microsoft.Data.Sqlite;
// using Microsoft.Extensions.Logging;
// using MotionLink.Models;

// namespace MotionLink.Repositories;

// public class MotionLinkRepositoryRaw : IMotionLinkRepository
// {

//     private readonly ILogger<MotionLinkRepositoryRaw> _logger;
//     private readonly string _dbPath = Path.Combine(FileSystem.AppDataDirectory, "motionLink.db");
//     private bool _isInitialised;
//     public MotionLinkRepository(ILogger<MotionLinkRepositoryRaw> logger)
//     {
//         _logger = logger;
//     }
//     public async Task<int> CreateSessionAsync(DateTime dateStart, CancellationToken stoppingToken = default)
//     {
//         await using var connection = new SqliteConnection($"Data Source={_dbPath}");
//         await connection.OpenAsync(stoppingToken);

//         var sql = "INSERT INTO Sessions (DateStart) VALUES ($dateStart); SELECT last_insert_rowid();";

//         await using var command = new SqliteCommand(sql, connection);
//         command.Parameters.AddWithValue("$dateStart", dateStart);

//         var result = await command.ExecuteScalarAsync(stoppingToken);

//         return Convert.ToInt32(result);
//     }

//     public async Task<Session?> UpdateSessionAsync(int id, string name, DateTime dateEnd, CancellationToken stoppingToken = default)
//     {
//         Session? session = null;
//         try
//         {
//             await using var connection = new SqliteConnection($"Data Source={_dbPath}");
//             await connection.OpenAsync(stoppingToken);

//             var sessionQuery = "UPDATE Sessions s" +
//                                "SET s.Name = $Name, s.DateEnd = $DateEnd" +
//                                "WHERE s.Id = $Id";
//             await using var command = new SqliteCommand(sessionQuery, connection);
//             command.Parameters.AddWithValue("$Id", id);
//             command.Parameters.AddWithValue("$Name", name);
//             command.Parameters.AddWithValue("$DateEnd", dateEnd);
//              var result = await command.ExecuteScalarAsync(stoppingToken);
//         }
//         catch (Exception ex)
//         {
//             _logger.LogError($"Unable to connect to database and fetch sessions {ex}");
//         }

//         return session;
//     }

//     public async Task<Session?> GetSessionByIdAsync(int id, CancellationToken stoppingToken = default)
//     {
//         Session? session = null;
//         try
//         {
//             await using var connection = new SqliteConnection($"Data Source={_dbPath}");
//             await connection.OpenAsync(stoppingToken);

//             var sessionQuery = "SELECT s.Id, s.Name, s.DateStart, s.DateEnd, sw.Id, sw.PeakRotation, sw.PeakGForce, FROM Sessions s" +
//                                "LEFT JOIN Swings sw" +
//                                "ON sw.SessionId = s.Id" +
//                                "WHERE s.Id = $Id";
//             await using var command = new SqliteCommand(sessionQuery, connection);
//             command.Parameters.AddWithValue("$Id", id);
//             await using var reader = await command.ExecuteReaderAsync(stoppingToken);
//             while (await reader.ReadAsync(stoppingToken))
//             {
//                 int sessionId = reader.GetInt32(reader.GetOrdinal("s.Id"));
//                 string name = reader.GetString(reader.GetOrdinal("s.Name"));
//                 DateTime dateStart = reader.GetDateTime(reader.GetOrdinal("s.DateStart"));
//                 DateTime dateEnd = reader.GetDateTime(reader.GetOrdinal("s.DateEnd"));

//                 int swingId = reader.GetInt32(reader.GetOrdinal("sw.Id"));
//                 double peakGForce = reader.GetDouble(reader.GetOrdinal("sw.PeakGForce"));
//                 double peakRotation = reader.GetDouble(reader.GetOrdinal("sw.PeakRotation"));

//                 if (session == null)
//                 {
//                     session = new Session()
//                     {
//                         Id = sessionId, 
//                         Name = name,
//                         DateStart = dateStart
//                         , DateEnd = dateEnd
//                     };
//                 }

//                 Swing newSwing = new Swing()
//                 {
//                     Id = swingId,
//                     PeakGForce = peakGForce,
//                     PeakRotation = peakRotation
//                 };

//                 session.Swings.Add(newSwing);
//             }
//         }
//         catch (Exception ex)
//         {
//             _logger.LogError($"Unable to connect to database and fetch sessions {ex}");
//         }

//         return session;
//     }

//     public async Task<List<SessionOverview>> GetSessionsAsync(CancellationToken stoppingToken = default)
//     {
//         List<SessionOverview> sessions = [];
//         try
//         {
//             await using var connection = new SqliteConnection($"Data Source={_dbPath}");
//             await connection.OpenAsync(stoppingToken);

//             var sessionQuery = "SELECT Id, Name, DateStart, DateEnd, Count(sw.Id) as SwingCount FROM Sessions s" +
//                                "LEFT JOIN Swings sw" +
//                                "ON sw.SessionId = s.id" +
//                                "GROUP BY s.Id";
//             await using var command = new SqliteCommand(sessionQuery, connection);
//             await using var reader = await command.ExecuteReaderAsync(stoppingToken);
//             while (await reader.ReadAsync(stoppingToken))
//             {
//                 sessions.Add(new SessionOverview()
//                 {
//                     Id = reader.GetInt32(reader.GetOrdinal("Id")),
//                     Name = reader.GetString(reader.GetOrdinal("name")),
//                     DateStart = reader.GetDateTime(reader.GetOrdinal("DateStart")),
//                     DateEnd = reader.GetDateTime(reader.GetOrdinal("DateStart")),
//                     SwingCount = reader.GetInt32(reader.GetOrdinal("SwingCount")),
//                 });
//             }
//         }
//         catch (Exception ex)
//         {
//             _logger.LogError($"Unable to connect to database and fetch endpoints {ex}");
//         }

//         return sessions;
//     }

//     public async Task InitialiseAsync(CancellationToken stoppingToken = default)
//     {
//         if (_isInitialised) return;

//         await using var connection = new SqliteConnection($"Data Source={_dbPath}");
//         await connection.OpenAsync(stoppingToken);

//         var createTableSql = @"
//             CREATE TABLE IF NOT EXISTS Sessions (
//                 Id INTEGER PRIMARY KEY AUTOINCREMENT,
//                 Name TEXT NULL,
//                 DateStart DATETIME,
//                 DateEnd DATETIME NULL
//             );

//             CREATE TABLE IF NOT EXISTS Swings (
//                 Id INTEGER PRIMARY KEY AUTOINCREMENT,
//                 SessionId INTEGER,
//                 Timestamp DATETIME,
//                 FOREIGN KEY(SessionId) REFERENCES Sessions(Id)
//             );

//             CREATE TABLE IF NOT EXISTS ImuPackets (
//                 Id INTEGER PRIMARY KEY AUTOINCREMENT,
//                 SwingId INTEGER,
//                 Ax REAL,
//                 Ay REAL,
//                 Az REAL,
//                 Gx REAL,
//                 Gy REAL,
//                 Gz REAL,
//                 Timestamp DATETIME,
//                 FOREIGN KEY(SwingId) REFERENCES Swings(Id)
//             );";

//         using var command = new SqliteCommand(createTableSql, connection);
//         await command.ExecuteNonQueryAsync(stoppingToken);

//         _isInitialised = true;
//     }

//     public async Task<Swing?> GetSwingByIdAsync(int id, CancellationToken stoppingToken = default)
//     {
//         Swing? swing = null;
//         try
//         {
//             await using var connection = new SqliteConnection($"Data Source={_dbPath}");
//             await connection.OpenAsync(stoppingToken);

//             var sessionQuery = "SELECT sw.Id, sw.PeakRotation, sw.PeakGForce, d.Ax, d.Ay, d.Az, d.Gx, d.Gy, d.Gz, d.TimeStamp FROM Swings sw" +
//                                "LEFT JOIN Imupackets d" +
//                                "ON d.swingId = sw.Id" +
//                                "WHERE s.Id = $Id";
//             await using var command = new SqliteCommand(sessionQuery, connection);
//             command.Parameters.AddWithValue("$Id", id);
//             await using var reader = await command.ExecuteReaderAsync(stoppingToken);
//             while (await reader.ReadAsync(stoppingToken))
//             {
//                 int swingId = reader.GetInt32(reader.GetOrdinal("sw.Id"));
//                 double peakGForce = reader.GetDouble(reader.GetOrdinal("sw.PeakGForce"));
//                 double peakRotation = reader.GetDouble(reader.GetOrdinal("sw.PeakRotation"));

//                 DateTime timeStamp = reader.GetDateTime(reader.GetOrdinal("d.Timestamp"));
//                 double Ax = reader.GetDouble(reader.GetOrdinal("d.Ax"));
//                 double Ay = reader.GetDouble(reader.GetOrdinal("d.Ay"));
//                 double Az = reader.GetDouble(reader.GetOrdinal("d.Az"));
//                 double Gx = reader.GetDouble(reader.GetOrdinal("d.Gx"));
//                 double Gy = reader.GetDouble(reader.GetOrdinal("d.Gy"));
//                 double Gz = reader.GetDouble(reader.GetOrdinal("d.Gz"));

//                 if (swing == null)
//                 {
//                     swing = new Swing()
//                     {
//                         Id = swingId,
//                         PeakGForce = peakGForce,
//                         PeakRotation = peakRotation
//                     };
//                 }

//                 ImuPacket imuPacket = new ImuPacket()
//                 {
//                     Ax = Ax,
//                     Ay = Ay,
//                     Az = Az,
//                     Gx = Gx,
//                     Gy = Gy,
//                     Gz = Gz,
//                     TimeStamp = timeStamp
//                 };


//                 swing.Data.Add(imuPacket);
//             }
//         }
//         catch (Exception ex)
//         {
//             _logger.LogError($"Unable to connect to database and fetch swing data {ex}");
//         }

//         return swing;
//     }
// }