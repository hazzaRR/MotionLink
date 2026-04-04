using System.Collections.Generic;

namespace MotionLink.Services;
public class SimpleMovingAverage
{
    private readonly int _windowSize;
    private readonly Queue<double> _values = new();
    private double _sum = 0;

    public SimpleMovingAverage(int windowSize = 5) => _windowSize = windowSize;

    public double Compute(double newValue)
    {
        _values.Enqueue(newValue);
        _sum += newValue;
        if (_values.Count > _windowSize) _sum -= _values.Dequeue();
        return _sum / _values.Count;
    }
}