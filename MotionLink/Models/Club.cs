using SQLite;

namespace MotionLink.Models;

[Table("Clubs")]
public class Club
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public double ShaftLengthInches { get; set; }
    public bool IsInBag { get; set; } = true;
}
