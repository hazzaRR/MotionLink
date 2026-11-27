using System.Reactive.Linq;
using MotionLink.Models;

namespace MotionLink.Services;

public class PacketProcessingService : IPacketProcessingService
{
    private readonly SimpleMovingAverage _filterAx = new(5);
    private readonly SimpleMovingAverage _filterAy = new(5);
    private readonly SimpleMovingAverage _filterAz = new(5);

    public (double, double) CalculateStats(ImuPacket data)
    {
        double currentG = Math.Sqrt(data.Ax * data.Ax + data.Ay * data.Ay + data.Az * data.Az);
        double currentRot = Math.Sqrt(data.Gx * data.Gx + data.Gy * data.Gy + data.Gz * data.Gz);

        // // Convert Degrees/Second to Radians/Second
        // double radiansPerSecond = currentRot * (Math.PI / 180);

        // // Calculate Linear Speed (Meters per Second)
        // double metersPerSecond = radiansPerSecond * 1.14;

        // // Convert to MPH
        // double mph = metersPerSecond * 2.237;

        // // Update a new property
        // PeakMph = mph;

        return (currentG, currentRot);
    }

    public ImuPacket ParseData(byte[] data)
    {
        
        double qW = BitConverter.ToSingle(data, 0);
        double qX = BitConverter.ToSingle(data, 4);
        double qY = BitConverter.ToSingle(data, 8);
        double qZ = BitConverter.ToSingle(data, 12);
        double rawAx = BitConverter.ToSingle(data, 16);
        double rawAy = BitConverter.ToSingle(data, 20);
        double rawAz = BitConverter.ToSingle(data, 24);
        double rawGx = BitConverter.ToSingle(data, 28);
        double rawGy = BitConverter.ToSingle(data, 32);
        double rawGz = BitConverter.ToSingle(data, 36);
        bool impactDetected = BitConverter.ToBoolean(data, 38);
                
        double cleanAx = _filterAx.Compute(rawAx);
        double cleanAy = _filterAy.Compute(rawAy);
        double cleanAz = _filterAz.Compute(rawAz);

        return new ImuPacket { TimeStamp = DateTime.Now, Ax = cleanAx, Ay = cleanAy, Az = cleanAz, Gx = rawGx, Gy = rawGy, Gz = rawGz, Qw = qW, Qx = qX, Qy = qY, Qz = qZ, Impact = impactDetected };

    }
}
