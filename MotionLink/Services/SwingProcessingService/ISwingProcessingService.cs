using MotionLink.Models;

namespace MotionLink.Services;

public interface ISwingProcessingService
{
    event Action<Swing>? SwingSaved;
    void StartCapture(int sessionId);
    void StartCapture(int sessionId, Club club, bool isRightHanded);
    Task<int> StopCaptureAsync();
    void ProcessPacket(ImuPacket packet);
}
