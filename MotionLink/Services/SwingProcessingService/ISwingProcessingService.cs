using MotionLink.Models;

namespace MotionLink.Services;

public interface ISwingProcessingService
{
    void StartCapture(int sessionId);
    Task<int> StopCaptureAsync();
    void ProcessPacket(ImuPacket packet);
}
