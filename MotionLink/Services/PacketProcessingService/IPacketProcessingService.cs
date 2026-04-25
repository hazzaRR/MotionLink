using MotionLink.Models;

namespace MotionLink.Services;

public interface IPacketProcessingService 
{
    ImuPacket ParseData(byte[] data);
    (double, double) CalculateStats(ImuPacket data);
}
