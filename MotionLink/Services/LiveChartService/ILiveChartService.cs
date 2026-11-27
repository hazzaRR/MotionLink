
using LiveChartsCore;
using MotionLink.Models;
namespace MotionLink.Services;
public interface ILiveChartService
{
    ISeries[] AccelSeries { get; }
    ISeries[] GyroSeries { get; }
    ISeries[] QuaternionSeries { get; }
    double PeakRotation {get; set;}
    double PeakGForce {get; set;}
    object Sync { get; }
    void UpdateCharts(ImuPacket packet, double currentG, double currentRot);

}