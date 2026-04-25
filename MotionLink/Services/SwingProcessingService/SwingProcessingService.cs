using CommunityToolkit.Mvvm.ComponentModel;
using MotionLink.Models;

namespace MotionLink.Services;

public partial class SwingProcessingService : ObservableObject, ISwingProcessingService
{
    private readonly List<ImuPacket> _rollingBuffer = new();
    private bool _isCapturing, _isCooldown;
    private int _postImpactCounter, _cooldownCounter;
    [ObservableProperty] private double _lastSwingMaxSpeed;
    [ObservableProperty] private double _lastSwingMaxRotation;
    public List<ImuPacket> SessionData { get; set; } = [];

    public void ProcessPacket(ImuPacket packet)
    {
        _rollingBuffer.Add(packet);
        SessionData.Add(packet);
        if (_rollingBuffer.Count > 200) _rollingBuffer.RemoveAt(0);

        if (_isCooldown) {
            if (++_cooldownCounter >= 250) _isCooldown = false;
            return;
        }

        if (!_isCapturing && packet.Impact) {
            _isCapturing = true;
            _postImpactCounter = 0;
            // Logic to start saving swing
        }

        if (_isCapturing) {
            _postImpactCounter++;
            if (_postImpactCounter >= 100) {
                CompleteSwing();
            }
        }
    }

    private void CompleteSwing() {
        

        LastSwingMaxRotation = _rollingBuffer.Max(p => Math.Sqrt(p.Gx*p.Gx + p.Gy*p.Gy + p.Gz*p.Gz));
        LastSwingMaxSpeed = _rollingBuffer.Max(p => Math.Sqrt(p.Ax * p.Ax + p.Ay * p.Ay + p.Az * p.Az));
        

        _isCapturing = false;
        _isCooldown = true;
        _cooldownCounter = 0;
    }
}