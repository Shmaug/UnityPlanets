using System.IO;

namespace Noise {
    public abstract class NoiseModule {
        public Planet planet;
        public double offset;
        public double scale = 1.0;
        public UnityEngine.Vector2 editorPos;
        public int index = -1;
        public byte displayGroup = 0;
        public byte flags = 0;

        public virtual double Get(double x, double y, double z) { return 0.0; }

        public virtual void Serialize(BinaryWriter bw) {
            bw.Write(offset);
            bw.Write(scale);
            bw.Write(editorPos.x);
            bw.Write(editorPos.y);
            bw.Write(displayGroup);
            bw.Write(flags);
        }
        public virtual void Deserialize(BinaryReader br) {
            offset = br.ReadDouble();
            scale = br.ReadDouble();
            editorPos.x = br.ReadSingle();
            editorPos.y = br.ReadSingle();
            displayGroup = br.ReadByte();
            flags = br.ReadByte();
        }

        public static NoiseModule DeserializeModule(BinaryReader br) {
            byte type = br.ReadByte();
            switch (type) {
                case 0: // simplex
                    Simplex s = new Simplex();
                    s.Deserialize(br);
                    return s;

                case 1: // fractal
                    Fractal f = new Fractal();
                    f.Deserialize(br);
                    return f;

                case 2: // blend
                    NoiseBlend nb = new NoiseBlend();
                    nb.Deserialize(br);
                    return nb;

                case 3: // math
                    NoiseMath nc = new NoiseMath();
                    nc.Deserialize(br);
                    return nc;

                case 4: // value
                    NoiseValue mn = new NoiseValue();
                    mn.Deserialize(br);
                    return mn;
            }
            
            return null;
        }
    }
}
