using SQLite;

namespace MotionLink.Models;

[Table("Sessions")]
public class Session
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime DateStart { get; set; }
    public DateTime? DateEnd { get; set; }
    [Ignore]
    public List<Swing> Swings { get; set; } = [];
}
