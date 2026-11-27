using System.Numerics;
using MotionLink.Models;
using MotionLink.Repositories;
using Microsoft.Extensions.Logging;

namespace MotionLink.Services;

public partial class SwingProcessingService : ISwingProcessingService
{
    private enum Phase { SeekingAddress, Address, Backswing, Downswing, FollowThrough }

    private readonly object _gate = new();
    private readonly List<ImuPacket> _samples = [];
    private readonly Queue<ImuPacket> _stillSamples = new();
    private Phase _phase;
    private int? _sessionId;
    private ImuPacket? _previous;
    private double _elapsedMs;
    private double _addressMs, _takeawayMs, _topMs, _impactMs;
    private double? _quietSinceMs;
    private DateTime _addressTime, _takeawayTime, _topTime, _impactTime;
    private Vector3 _swingAxis;
    private Task _pendingWrites = Task.CompletedTask;
    private int _failedWrites;
    private readonly IMotionLinkRepository _repository;
    private readonly ILogger<SwingProcessingService> _logger;

    public SwingProcessingService(IMotionLinkRepository repository, ILogger<SwingProcessingService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public void StartCapture(int sessionId)
    {
        lock (_gate)
        {
            _sessionId = sessionId;
            _previous = null;
            _elapsedMs = 0;
            ResetDetection();
        }
    }

    public async Task<int> StopCaptureAsync()
    {
        Task writes;
        lock (_gate)
        {
            _sessionId = null;
            ResetDetection();
            writes = _pendingWrites;
        }

        await writes;
        return Interlocked.Exchange(ref _failedWrites, 0);
    }

    public void ProcessPacket(ImuPacket packet)
    {
        lock (_gate)
        {
            if (_sessionId is not int sessionId) return;

            var previous = _previous;
            double deltaMs = 0;
            if (previous is not null)
            {
                if (packet.SensorTimestampMs is long current && previous.SensorTimestampMs is long last)
                    deltaMs = unchecked((uint)current - (uint)last);
                else
                    deltaMs = (packet.TimeStamp - previous.TimeStamp).TotalMilliseconds;

                if (deltaMs <= 0 || deltaMs > int.MaxValue) return;
                if (deltaMs > 30) ResetDetection();

                // Keep the waveform on the sensor clock even if notifications arrive irregularly.
                packet.TimeStamp = previous.TimeStamp.AddMilliseconds(deltaMs);
                _elapsedMs += deltaMs;
            }
            _previous = packet;

            var gyro = new Vector3((float)packet.Gx, (float)packet.Gy, (float)packet.Gz);
            double rotation = gyro.Length();
            if (_phase == Phase.SeekingAddress)
            {
                ObserveAddress(packet, rotation);
                return;
            }

            _samples.Add(packet);
            switch (_phase)
            {
                case Phase.Address:
                    if (rotation >= 65)
                    {
                        _swingAxis = Vector3.Normalize(gyro);
                        _takeawayMs = _elapsedMs;
                        _takeawayTime = packet.TimeStamp;
                        _phase = Phase.Backswing;
                    }
                    else if (rotation <= 20)
                    {
                        _addressMs = _elapsedMs;
                        _addressTime = packet.TimeStamp;
                        while ((packet.TimeStamp - _samples[0].TimeStamp).TotalMilliseconds > 550)
                            _samples.RemoveAt(0);
                    }
                    else if (_elapsedMs - _addressMs > 1000)
                    {
                        ResetDetection();
                        ObserveAddress(packet, rotation);
                    }
                    break;

                case Phase.Backswing:
                    if (_elapsedMs - _takeawayMs > 2000)
                        ResetDetection();
                    else if (_elapsedMs - _takeawayMs >= 150 && Vector3.Dot(gyro, _swingAxis) <= 0)
                    {
                        _topMs = _elapsedMs;
                        _topTime = packet.TimeStamp;
                        _phase = Phase.Downswing;
                    }
                    break;

                case Phase.Downswing:
                    double downswingMs = _elapsedMs - _topMs;
                    if (downswingMs > 350)
                        ResetDetection();
                    else if (downswingMs >= 200 && previous is not null &&
                             (Acceleration(packet) - Acceleration(previous)) / deltaMs >= 0.5 &&
                             Acceleration(packet) >= 4)
                    {
                        packet.Impact = true;
                        _impactMs = _elapsedMs;
                        _impactTime = packet.TimeStamp;
                        _phase = Phase.FollowThrough;
                    }
                    break;

                case Phase.FollowThrough:
                    _quietSinceMs = rotation <= 40 ? _quietSinceMs ?? _elapsedMs : null;
                    if ((_quietSinceMs is double quietSince && _elapsedMs - quietSince >= 100) ||
                        _elapsedMs - _impactMs >= 750)
                        CompleteSwing(sessionId, packet.TimeStamp);
                    break;
            }
        }
    }

    private void ObserveAddress(ImuPacket packet, double rotation)
    {
        if (rotation > 20)
        {
            _stillSamples.Clear();
            return;
        }

        _stillSamples.Enqueue(packet);
        while ((packet.TimeStamp - _stillSamples.Peek().TimeStamp).TotalMilliseconds > 550)
            _stillSamples.Dequeue();

        if ((packet.TimeStamp - _stillSamples.Peek().TimeStamp).TotalMilliseconds < 500)
            return;

        var mean = new Vector3(
            (float)_stillSamples.Average(sample => sample.Ax),
            (float)_stillSamples.Average(sample => sample.Ay),
            (float)_stillSamples.Average(sample => sample.Az));
        double variance = _stillSamples.Average(sample =>
            Vector3.DistanceSquared(mean, new Vector3((float)sample.Ax, (float)sample.Ay, (float)sample.Az)));
        if (variance > 0.0025) return;

        _samples.AddRange(_stillSamples);
        _stillSamples.Clear();
        _addressTime = packet.TimeStamp;
        _addressMs = _elapsedMs;
        _phase = Phase.Address;
    }

    private void CompleteSwing(int sessionId, DateTime finishTime)
    {
        var swing = new Swing
        {
            SessionId = sessionId,
            AddressTime = _addressTime,
            TakeawayTime = _takeawayTime,
            TopTime = _topTime,
            ImpactTime = _impactTime,
            FinishTime = finishTime,
            BackswingDurationMs = _topMs - _takeawayMs,
            DownswingDurationMs = _impactMs - _topMs,
            TempoRatio = (_topMs - _takeawayMs) / (_impactMs - _topMs),
            PeakGForce = _samples.Max(Acceleration),
            PeakRotation = _samples.Max(sample =>
                Math.Sqrt(sample.Gx * sample.Gx + sample.Gy * sample.Gy + sample.Gz * sample.Gz)),
            Data = [.. _samples]
        };
        _pendingWrites = SaveSwingAsync(_pendingWrites, swing);
        ResetDetection();
    }

    private async Task SaveSwingAsync(Task previousWrite, Swing swing)
    {
        try
        {
            await previousWrite.ConfigureAwait(false);
            await _repository.SaveDetectedSwingAsync(swing).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Interlocked.Increment(ref _failedWrites);
            _logger.LogError(ex, "Could not save swing for session {SessionId}", swing.SessionId);
        }
    }

    private void ResetDetection()
    {
        _phase = Phase.SeekingAddress;
        _stillSamples.Clear();
        _samples.Clear();
        _quietSinceMs = null;
    }

    private static double Acceleration(ImuPacket sample)
        => Math.Sqrt(sample.Ax * sample.Ax + sample.Ay * sample.Ay + sample.Az * sample.Az);
}
