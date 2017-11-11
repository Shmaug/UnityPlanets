using System.IO;

namespace Noise {
    public enum FractalType {
        FBM, Billow, RidgedMultiFractal, MultiFractal, HybridMultiFractal
    }

    public class Fractal : NoiseModule {
        // based off https://github.com/miketucker/Unity-Accidental-Noise/blob/master/Unity/Assets/Plugins/AccidentalNoise/Fractal.cs

        const int MaxSources = 20;
        
        public FractalType type {
            get { return _type; }
            set {
                _type = value;
                switch (value) {
                    case FractalType.FBM:
                        h = 1.0;
                        gain = 0.0;
                        offst = 0.0;
                        fBm_calcWeights();
                        break;
                    case FractalType.Billow:
                        h = 1.0;
                        gain = 0.0;
                        offst = 0.0;
                        Billow_calcWeights();
                        break;
                    case FractalType.RidgedMultiFractal:
                        h = 0.9;
                        gain = 2.0;
                        offst = 1.0;
                        RidgedMulti_calcWeights();
                        break;
                    case FractalType.MultiFractal:
                        h = 1.0;
                        gain = 0.0;
                        offst = 0.0;
                        Multi_calcWeights();
                        break;
                    case FractalType.HybridMultiFractal:
                        h = 0.25;
                        gain = 1.0;
                        offst = 0.7;
                        HybridMulti_calcWeights();
                        break;
                }
            }
        }
        public int seed {
            get { return _seed; }
            set {
                _seed = value;
                for (int i = 0; i < _octaves; i++) {
                    sources[i] = new Simplex();
                    (sources[i] as Simplex).seed = value + i * 300;
                }
            }
        }
        public int octaves {
            get { return _octaves; }
            set {
                _octaves = value >= MaxSources ? MaxSources - 1 : value;
                for (int i = 0; i < _octaves; i++) {
                    sources[i] = new Simplex();
                    (sources[i] as Simplex).seed = _seed + i * 300;
                }
            }
        }
        
        public double lacunarity = 2.0;
        public double frequency = 1.0;

        public int warpModule = -1;

        private FractalType _type = FractalType.RidgedMultiFractal;
        private int _octaves = 8;
        private int _seed = 123456;

        private double h;
        private double gain;
        private double offst;

        private double[] exparray = new double[MaxSources];
        private double[,] correction = new double[MaxSources, 2];
        private NoiseModule[] sources = new NoiseModule[MaxSources];
        
        public Fractal() {
            type = _type;
            octaves = _octaves;
        }

        public override void Serialize(BinaryWriter bw) {
            bw.Write((byte)1);
            base.Serialize(bw);

            bw.Write(_seed);
            bw.Write(warpModule);
            bw.Write(_octaves);
            bw.Write((int)_type);
            bw.Write(lacunarity);
            bw.Write(frequency);
        }
        public override void Deserialize(BinaryReader br) {
            base.Deserialize(br);
            _seed = br.ReadInt32();
            warpModule = br.ReadInt32();
            octaves = br.ReadInt32();
            type = (FractalType)br.ReadInt32();
            lacunarity = br.ReadDouble();
            frequency = br.ReadDouble();
        }

        #region Weight Calculations
        private void fBm_calcWeights() {
            //std::cout << "Weights: ";
            for (int i = 0; i < MaxSources; ++i) {
                exparray[i] = Mathd.Pow(lacunarity, -i * h);
            }

            // Calculate scale/bias pairs by guessing at minimum and maximum values and remapping to [-1,1]
            double minvalue = 0.0, maxvalue = 0.0;

            for (int i = 0; i < MaxSources; ++i) {
                minvalue += -1.0 * exparray[i];
                maxvalue += 1.0 * exparray[i];

                double A = -1.0, B = 1.0;
                double scale = (B - A) / (maxvalue - minvalue);
                double bias = A - minvalue * scale;
                correction[i, 0] = scale;
                correction[i, 1] = bias;
            }
        }
        private void RidgedMulti_calcWeights() {
            for (int i = 0; i < MaxSources; ++i)
                exparray[i] = Mathd.Pow(lacunarity, -i * h);

            // Calculate scale/bias pairs by guessing at minimum and maximum values and remapping to [-1,1]
            double minvalue = 0.0, maxvalue = 0.0;
            for (int i = 0; i < MaxSources; ++i) {
                minvalue += (offst - 1.0) * (offst - 1.0) * exparray[i];
                maxvalue += (offst) * (offst) * exparray[i];

                double A = -1.0, B = 1.0;
                double scale = (B - A) / (maxvalue - minvalue);
                double bias = A - minvalue * scale;
                correction[i, 0] = scale;
                correction[i, 1] = bias;
            }
        }
        private void Billow_calcWeights() {
            for (int i = 0; i < MaxSources; ++i)
                exparray[i] = Mathd.Pow(lacunarity, -i * h);

            // Calculate scale/bias pairs by guessing at minimum and maximum values and remapping to [-1,1]
            double minvalue = 0.0, maxvalue = 0.0;
            for (int i = 0; i < MaxSources; ++i) {
                minvalue += -1.0 * exparray[i];
                maxvalue += 1.0 * exparray[i];

                double A = -1.0, B = 1.0;
                double scale = (B - A) / (maxvalue - minvalue);
                double bias = A - minvalue * scale;
                correction[i, 0] = scale;
                correction[i, 1] = bias;
            }
        }
        private void Multi_calcWeights() {
            for (int i = 0; i < MaxSources; ++i)
                exparray[i] = Mathd.Pow(lacunarity, -i * h);

            // Calculate scale/bias pairs by guessing at minimum and maximum values and remapping to [-1,1]
            double minvalue = 1.0, maxvalue = 1.0;
            for (int i = 0; i < MaxSources; ++i) {
                minvalue *= -1.0 * exparray[i] + 1.0;
                maxvalue *= 1.0 * exparray[i] + 1.0;

                double A = -1.0, B = 1.0;
                double scale = (B - A) / (maxvalue - minvalue);
                double bias = A - minvalue * scale;
                correction[i, 0] = scale;
                correction[i, 1] = bias;
            }

        }
        private void HybridMulti_calcWeights() {
            for (int i = 0; i < MaxSources; ++i)
                exparray[i] = Mathd.Pow(lacunarity, -i * h);

            // Calculate scale/bias pairs by guessing at minimum and maximum values and remapping to [-1,1]
            double minvalue = 1.0, maxvalue = 1.0;
            double weightmin, weightmax;
            double A = -1.0, B = 1.0, scale, bias;

            minvalue = offst - 1.0;
            maxvalue = offst + 1.0;
            weightmin = gain * minvalue;
            weightmax = gain * maxvalue;

            scale = (B - A) / (maxvalue - minvalue);
            bias = A - minvalue * scale;
            correction[0, 0] = scale;
            correction[0, 1] = bias;

            for (int i = 1; i < MaxSources; ++i) {
                if (weightmin > 1.0) weightmin = 1.0;
                if (weightmax > 1.0) weightmax = 1.0;

                double signal = (offst - 1.0) * exparray[i];
                minvalue += signal * weightmin;
                weightmin *= gain * signal;

                signal = (offst + 1.0) * exparray[i];
                maxvalue += signal * weightmax;
                weightmax *= gain * signal;

                scale = (B - A) / (maxvalue - minvalue);
                bias = A - minvalue * scale;
                correction[i, 0] = scale;
                correction[i, 1] = bias;
            }
        }
        #endregion

        public override double Get(double x, double y, double z) {
            double warp = warpModule == -1 ? 0 : planet.noiseModules[warpModule].Get(x, y, z);
            x = (x + offset + warp) * scale;
            y = (y + offset + warp) * scale;
            z = (z + offset + warp) * scale;

            switch (type) {
                case FractalType.FBM:
                    return FBM(x, y, z);
                case FractalType.Billow:
                    return Billow(x, y, z);
                case FractalType.RidgedMultiFractal:
                    return Ridged(x, y, z);
                case FractalType.MultiFractal:
                    return MultiFractal(x, y, z);
                case FractalType.HybridMultiFractal:
                    return Hybrid(x, y, z);
                default:
                    return FBM(x, y, z);
            }
        }

        double FBM(double x, double y, double z) {
            double value = 0.0;
            double signal = 0;

            x *= frequency;
            y *= frequency;
            z *= frequency;

            for (int i = 0; i < octaves; i++) {
                signal = sources[i].Get(x, y, z) * exparray[i];
                value += signal;
                x *= lacunarity;
                y *= lacunarity;
                z *= lacunarity;
            }

            return value;
        }
        double Billow(double x, double y, double z) {
            double value = 0.0, signal;
            x *= frequency;
            y *= frequency;
            z *= frequency;

            for (uint i = 0; i < octaves; ++i) {
                signal = sources[i].Get(x, y, z);
                signal = 2.0 * Mathd.Abs(signal) - 1.0;
                value += signal * exparray[i];

                x *= lacunarity;
                y *= lacunarity;
                z *= lacunarity;
            }

            value += 0.5;
            return value * correction[octaves - 1, 0] + correction[octaves - 1, 1];
        }
        double Ridged(double x, double y, double z) {
            double result = 0.0, signal;
            x *= frequency;
            y *= frequency;
            z *= frequency;

            for (uint i = 0; i < octaves; ++i) {
                signal = sources[i].Get(x, y, z);
                signal = offst - Mathd.Abs(signal);
                signal *= signal;
                result += signal * exparray[i];

                x *= lacunarity;
                y *= lacunarity;
                z *= lacunarity;
            }

            return result * correction[octaves - 1, 0] + correction[octaves - 1, 1];
        }
        double MultiFractal(double x, double y, double z) {
            double value = 1.0;
            x *= frequency;
            y *= frequency;
            z *= frequency;

            for (int i = 0; i < octaves; i++) {
                value *= sources[i].Get(x, y, z) * exparray[i] + 1.0;
                x *= lacunarity;
                y *= lacunarity;
                z *= lacunarity;
            }

            return value * correction[octaves - 1, 0] + correction[octaves - 1, 1];
        }
        double Hybrid(double x, double y, double z) {
            double value, signal, weight;
            x *= frequency;
            y *= frequency;
            z *= frequency;

            value = sources[0].Get(x, y, z) + offst;
            weight = gain * value;
            x *= lacunarity;
            y *= lacunarity;
            z *= lacunarity;

            for (uint i = 1; i < octaves; ++i) {
                if (weight > 1.0) weight = 1.0;
                signal = (sources[i].Get(x, y, z) + offst) * exparray[i];
                value += weight * signal;
                weight *= gain * signal;
                x *= lacunarity;
                y *= lacunarity;

            }

            return value * correction[octaves - 1, 0] + correction[octaves - 1, 1];
        }
    }
}