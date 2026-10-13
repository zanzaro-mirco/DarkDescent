using System;
using System.Globalization;
using System.Text;

namespace DarkDescent.Diagnostics
{
    /// <summary>
    /// I frame di un tratto della misura in build (D6 della M11): durata, tempo del main thread,
    /// byte allocati e contatori del rendering. Gli array sono creati una volta: aggiungere un frame
    /// non alloca. I valori negativi dicono che il contatore non c'è (una build senza Development).
    /// </summary>
    public sealed class FrameStats
    {
        private readonly float[] _frameMs;
        private readonly float[] _cpuMs;
        private readonly float[] _scratch;

        private double _seconds;
        private long _allocTotal;
        private long _allocMax;
        private int _allocatingFrames;
        private int _allocSamples;
        private int _cpuSamples;
        private double _batches;
        private double _setPass;
        private double _triangles;
        private int _renderSamples;

        public FrameStats(int capacity)
        {
            if (capacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity));
            }

            _frameMs = new float[capacity];
            _cpuMs = new float[capacity];
            _scratch = new float[capacity];
        }

        /// <summary>I frame tenuti: oltre la capacità i frame si contano nei secondi ma non nelle durate.</summary>
        public int Count { get; private set; }

        /// <summary>I secondi misurati, tutti i frame compresi.</summary>
        public double Seconds => _seconds;

        public bool HasCpu => _cpuSamples > 0;

        public bool HasAlloc => _allocSamples > 0;

        public bool HasRender => _renderSamples > 0;

        /// <summary>I frame in cui il gioco ha allocato memoria gestita.</summary>
        public int AllocatingFrames => _allocatingFrames;

        public long AllocMax => _allocMax;

        public double AllocAverage => _allocSamples > 0 ? (double)_allocTotal / _allocSamples : 0d;

        public double BatchesAverage => _renderSamples > 0 ? _batches / _renderSamples : 0d;

        public double SetPassAverage => _renderSamples > 0 ? _setPass / _renderSamples : 0d;

        public double TrianglesAverage => _renderSamples > 0 ? _triangles / _renderSamples : 0d;

        public void Reset()
        {
            Count = 0;
            _seconds = 0d;
            _allocTotal = 0;
            _allocMax = 0;
            _allocatingFrames = 0;
            _allocSamples = 0;
            _cpuSamples = 0;
            _batches = 0d;
            _setPass = 0d;
            _triangles = 0d;
            _renderSamples = 0;
        }

        public void Add(float frameMs, float cpuMs, long allocBytes, long batches, long setPass, long triangles)
        {
            _seconds += frameMs / 1000d;
            if (Count < _frameMs.Length)
            {
                _frameMs[Count] = frameMs;
                _cpuMs[Count] = cpuMs;
                Count++;
            }

            if (cpuMs >= 0f)
            {
                _cpuSamples++;
            }

            if (allocBytes >= 0)
            {
                _allocSamples++;
                _allocTotal += allocBytes;
                if (allocBytes > 0)
                {
                    _allocatingFrames++;
                }

                if (allocBytes > _allocMax)
                {
                    _allocMax = allocBytes;
                }
            }

            if (batches >= 0)
            {
                _renderSamples++;
                _batches += batches;
                _setPass += Math.Max(0, setPass);
                _triangles += Math.Max(0, triangles);
            }
        }

        public double FrameAverage => Average(_frameMs, Count);

        public float FrameMax => Percentile(_frameMs, 100f);

        public float FramePercentile(float percent) => Percentile(_frameMs, percent);

        public double CpuAverage => HasCpu ? Average(_cpuMs, Count) : 0d;

        public float CpuPercentile(float percent) => HasCpu ? Percentile(_cpuMs, percent) : 0f;

        /// <summary>Una riga leggibile: la scrive la misura nel log e nel file dei risultati.</summary>
        public string Describe(string label)
        {
            var culture = CultureInfo.InvariantCulture;
            var text = new StringBuilder();
            double average = FrameAverage;
            text.Append(label).Append(": ")
                .Append(Count.ToString(culture)).Append(" frame in ")
                .Append(Seconds.ToString("0.0", culture)).Append(" s, media ")
                .Append(average.ToString("0.00", culture)).Append(" ms (")
                .Append((average > 0d ? 1000d / average : 0d).ToString("0", culture)).Append(" fps), p95 ")
                .Append(FramePercentile(95f).ToString("0.00", culture)).Append(", p99 ")
                .Append(FramePercentile(99f).ToString("0.00", culture)).Append(", max ")
                .Append(FrameMax.ToString("0.00", culture)).Append(" ms");
            if (HasCpu)
            {
                text.Append("; main thread media ").Append(CpuAverage.ToString("0.00", culture))
                    .Append(" ms, p99 ").Append(CpuPercentile(99f).ToString("0.00", culture)).Append(" ms");
            }

            if (HasAlloc)
            {
                text.Append("; allocazioni in ").Append(AllocatingFrames.ToString(culture))
                    .Append(" frame, media ").Append(AllocAverage.ToString("0", culture))
                    .Append(" B, max ").Append(AllocMax.ToString(culture)).Append(" B");
            }
            else
            {
                text.Append("; allocazioni n/d");
            }

            if (HasRender)
            {
                text.Append("; batch ").Append(BatchesAverage.ToString("0", culture))
                    .Append(", SetPass ").Append(SetPassAverage.ToString("0", culture))
                    .Append(", triangoli ").Append((TrianglesAverage / 1000d).ToString("0", culture)).Append("k");
            }

            return text.ToString();
        }

        private static double Average(float[] values, int count)
        {
            if (count == 0)
            {
                return 0d;
            }

            double sum = 0d;
            for (int i = 0; i < count; i++)
            {
                sum += values[i];
            }

            return sum / count;
        }

        // al rango più vicino, su una copia ordinata: si chiama a fine misura, non a ogni frame
        private float Percentile(float[] values, float percent)
        {
            if (Count == 0)
            {
                return 0f;
            }

            Array.Copy(values, _scratch, Count);
            Array.Sort(_scratch, 0, Count);
            int rank = (int)Math.Ceiling(percent / 100d * Count);
            return _scratch[Math.Min(Count, Math.Max(1, rank)) - 1];
        }
    }
}
