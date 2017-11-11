using System.Collections.Generic;
using System.IO;

namespace Noise {
    [System.Serializable]
    public enum NoiseValueType {
        Number, DirectionX, DirectionY, DirectionZ
    }

    public class NoiseValue : NoiseModule {
        public NoiseValueType type;
        public double value;

        public override double Get(double x, double y, double z) {
            switch (type) {
                case NoiseValueType.Number:
                    return value;
                case NoiseValueType.DirectionX:
                    return x;
                case NoiseValueType.DirectionY:
                    return y;
                case NoiseValueType.DirectionZ:
                    return z;
                default:
                    return value;
            }
        }

        public override void Serialize(BinaryWriter bw) {
            bw.Write((byte)4);
            base.Serialize(bw);

            bw.Write((int)type);
            bw.Write(value);
        }
        public override void Deserialize(BinaryReader br) {
            base.Deserialize(br);
            type = (NoiseValueType)br.ReadInt32();
            value = br.ReadDouble();
        }
    }
}