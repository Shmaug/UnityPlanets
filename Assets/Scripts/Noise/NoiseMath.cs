using System.Collections.Generic;
using System.IO;

namespace Noise {
    public enum NoiseOperation {
        Multipy, Add, Average, Max, Min, Power, Invert, Abs, Map01, Map1
    }

    public class NoiseMath : NoiseModule {
        public NoiseOperation operation = NoiseOperation.Multipy;
        public List<int> modules = new List<int>();

        public int usedModules {
            get {
                switch (operation) {
                    case NoiseOperation.Power:
                        return 2;
                    case NoiseOperation.Invert:
                        return 1;
                    case NoiseOperation.Abs:
                        return 1;
                    case NoiseOperation.Map01:
                        return 1;
                    case NoiseOperation.Map1:
                        return 1;
                    default:
                        return modules.Count;
                }
            }
        }
        public bool canAcceptMore {
            get {
                switch (operation) {
                    case NoiseOperation.Power:
                        return modules.Count < 2;
                    case NoiseOperation.Invert:
                        return modules.Count < 1;
                    case NoiseOperation.Abs:
                        return modules.Count < 1;
                    case NoiseOperation.Map01:
                        return modules.Count < 1;
                    case NoiseOperation.Map1:
                        return modules.Count < 1;
                    default:
                        return true;
                }
            }
        }

        public override double Get(double x, double y, double z) {
            if (modules.Count == 0) return 0.0;

            switch (operation) {
                case NoiseOperation.Add:
                    return Add(x, y, z);
                case NoiseOperation.Multipy:
                    return Multiply(x, y, z);
                case NoiseOperation.Average:
                    return Add(x, y, z) / modules.Count;
                case NoiseOperation.Max:
                    return Max(x, y, z);
                case NoiseOperation.Min:
                    return Min(x, y, z);
                case NoiseOperation.Power:
                    return Power(x, y, z);
                case NoiseOperation.Invert:
                    return Invert(x, y, z);
                case NoiseOperation.Abs:
                    return Abs(x, y, z);
                case NoiseOperation.Map01:
                    if (modules == null || modules.Count < 1) return 0.0;
                    return planet.noiseModules[modules[0]].Get(x, y, z) * .5 + .5;
                case NoiseOperation.Map1:
                    if (modules == null || modules.Count < 1) return 0.0;
                    return planet.noiseModules[modules[0]].Get(x, y, z) * 2.0 - 1.0;
                default:
                    return Multiply(x, y, z);
            }
        }

        public override void Serialize(BinaryWriter bw) {
            bw.Write((byte)3);
            base.Serialize(bw);

            bw.Write((int)operation);
            bw.Write(modules.Count);
            foreach (int nm in modules)
                bw.Write(nm);
        }
        public override void Deserialize(BinaryReader br) {
            base.Deserialize(br);
            operation = (NoiseOperation)br.ReadInt32();
            modules = new List<int>();
            int count = br.ReadInt32();
            for (int i = 0; i < count; i++)
                modules.Add(br.ReadInt32());
        }

        double Add(double x, double y, double z) {
            double sum = 0;
            foreach (int nm in modules)
                sum += planet.noiseModules[nm].Get(x, y, z);
            return sum;
        }
        double Multiply(double x, double y, double z) {
            double sum = 1.0;
            foreach (int nm in modules)
                sum *= planet.noiseModules[nm].Get(x, y, z);
            return sum;
        }
        double Max(double x, double y, double z) {
            double max = planet.noiseModules[modules[0]].Get(x, y, z);
            foreach (int nm in modules)
                max = Mathd.Max(max, planet.noiseModules[nm].Get(x, y, z));
            return max;
        }
        double Min(double x, double y, double z) {
            double min = planet.noiseModules[modules[0]].Get(x, y, z);
            foreach (int nm in modules)
                min = Mathd.Min(min, planet.noiseModules[nm].Get(x, y, z));
            return min;
        }
        double Power(double x, double y, double z) {
            if (modules == null || modules.Count < 2) return 0.0;
            return Mathd.Pow(planet.noiseModules[modules[0]].Get(x, y, z), planet.noiseModules[modules[1]].Get(x, y, z));
        }
        double Invert(double x, double y, double z) {
            if (modules == null || modules.Count < 1) return 0.0;
            return -planet.noiseModules[modules[0]].Get(x, y, z);
        }
        double Abs(double x, double y, double z) {
            if (modules == null || modules.Count < 1) return 0.0;
            return Mathd.Abs(planet.noiseModules[modules[0]].Get(x, y, z));
        }
    }
}