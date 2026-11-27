using System.Buffers.Binary;
using MotionLink.Models;

namespace MotionLink.Services;

public class PacketProcessingService : IPacketProcessingService
{
    public (double, double) CalculateStats(ImuPacket data)
    {
        return (
            Math.Sqrt(data.Ax * data.Ax + data.Ay * data.Ay + data.Az * data.Az),
            Math.Sqrt(data.Gx * data.Gx + data.Gy * data.Gy + data.Gz * data.Gz));
    }

    public ImuPacket ParseData(byte[] data)
    {
        if (data.Length == 41)
        {
            return new ImuPacket
            {
                TimeStamp = DateTime.UtcNow,
                Qw = BitConverter.ToSingle(data, 0), Qx = BitConverter.ToSingle(data, 4),
                Qy = BitConverter.ToSingle(data, 8), Qz = BitConverter.ToSingle(data, 12),
                Ax = BitConverter.ToSingle(data, 16), Ay = BitConverter.ToSingle(data, 20),
                Az = BitConverter.ToSingle(data, 24), Gx = BitConverter.ToSingle(data, 28),
                Gy = BitConverter.ToSingle(data, 32), Gz = BitConverter.ToSingle(data, 36),
                Impact = data[40] == 1
            };
        }

        if (data.Length != 24)
            throw new FormatException($"Unexpected IMU payload length: {data.Length}.");

        return new ImuPacket
        {
            SensorTimestampMs = BinaryPrimitives.ReadUInt32LittleEndian(data),
            TimeStamp = DateTime.UtcNow,
            Ax = ReadAxis(data, 4, 2048), Ay = ReadAxis(data, 6, 2048), Az = ReadAxis(data, 8, 2048),
            Gx = ReadAxis(data, 10, 16), Gy = ReadAxis(data, 12, 16), Gz = ReadAxis(data, 14, 16),
            Qw = ReadAxis(data, 16, 32767), Qx = ReadAxis(data, 18, 32767),
            Qy = ReadAxis(data, 20, 32767), Qz = ReadAxis(data, 22, 32767)
        };
    }

    private static double ReadAxis(byte[] data, int offset, double scale)
        => BinaryPrimitives.ReadInt16LittleEndian(data.AsSpan(offset)) / scale;
}
