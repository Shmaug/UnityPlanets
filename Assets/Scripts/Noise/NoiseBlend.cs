using System.IO;

namespace Noise {
    public enum NoiseBlendType {
        Lerp
    }

    public class NoiseBlend : NoiseModule {
        public NoiseBlendType type = NoiseBlendType.Lerp;
        public int control = -1, module1 = -1, module2 = -1;

        public override void Serialize(BinaryWriter bw) {
            bw.Write((byte)2);
            base.Serialize(bw);

            bw.Write((int)type);

            bw.Write(control);
            bw.Write(module1);
            bw.Write(module2);
        }
        public override void Deserialize(BinaryReader br) {
            base.Deserialize(br);
            type = (NoiseBlendType)br.ReadInt32();
            control = br.ReadInt32();
            module1 = br.ReadInt32();
            module2 = br.ReadInt32();
        }

        public override double Get(double x, double y, double z) {
            double m1 = 0.0, m2 = 0.0, c = 0.0;

            if (control != -1)
                c = planet.noiseModules[control].Get(x, y, z);
            if (module1 != -1)
                m1 = planet.noiseModules[module1].Get(x, y, z);
            if (module2 != -1)
                m2 = planet.noiseModules[module2].Get(x, y, z);

            switch (type) {
                case NoiseBlendType.Lerp:
                    return Mathd.Lerp(m1, m2, c * .5 + .5);

                default:
                    return Mathd.Lerp(m1, m2, c * .5 + .5);
            }
        }
    }
}