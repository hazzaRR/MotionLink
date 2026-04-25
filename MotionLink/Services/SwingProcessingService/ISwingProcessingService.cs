using MotionLink.Models;

namespace MotionLink.Services;

public interface ISwingProcessingService
{
    List<ImuPacket> SessionData { get; set; }
    void ProcessPacket(ImuPacket packet);
}