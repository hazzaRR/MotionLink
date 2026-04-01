namespace MotionLink.Models;
public class ROMPacket
{    public DateTimeOffset TimeStamp {get; set; }
    public float Ax {get; set; }
    public float Ay {get; set; }
    public float Az {get; set; }
    public float Gx {get; set; }
    public float Gy {get; set; }
    public float Gz {get; set; }
}
