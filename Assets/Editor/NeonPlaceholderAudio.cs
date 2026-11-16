using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace NeonVeil.Editor
{
    // Original synthesized placeholders. No recorded or third-party sound was sampled.
    internal static class NeonPlaceholderAudio
    {
        private const string Folder = "Assets/Audio/Chapter1";
        private const int SampleRate = 44100;
        private const float Tau = Mathf.PI * 2f;

        public static AudioClip GetCue(NeonAudio.Cue cue)
        {
            float duration = cue switch
            {
                NeonAudio.Cue.Jump => 0.21f,
                NeonAudio.Cue.Land => 0.16f,
                NeonAudio.Cue.Collect => 0.31f,
                NeonAudio.Cue.Hurt => 0.28f,
                NeonAudio.Cue.Win => 0.96f,
                NeonAudio.Cue.Attack => 0.20f,
                NeonAudio.Cue.Phase => 0.34f,
                _ => 0.2f
            };
            return GetClip(cue + "_Placeholder", duration, time => CueSample(cue, time, duration));
        }

        public static AudioClip GetAmbience()
        {
            return GetClip("DeadDistrict_Ambience_Placeholder", 16f, AmbienceSample);
        }

        private static AudioClip GetClip(string name, float duration, Func<float, float> sample)
        {
            string path = Folder + "/" + name + ".wav";
            if (!File.Exists(path))
            {
                Directory.CreateDirectory(Folder);
                WriteWave(path, duration, sample);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                AudioImporter importer = (AudioImporter)AssetImporter.GetAtPath(path);
                AudioImporterSampleSettings settings = importer.defaultSampleSettings;
                settings.compressionFormat = AudioCompressionFormat.PCM;
                settings.loadType = AudioClipLoadType.DecompressOnLoad;
                settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
                importer.defaultSampleSettings = settings;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }

        private static void WriteWave(string path, float duration, Func<float, float> sample)
        {
            int count = Mathf.RoundToInt(duration * SampleRate);
            int bytes = count * sizeof(short);
            using BinaryWriter wave = new BinaryWriter(File.Create(path));
            wave.Write(new[] { 'R', 'I', 'F', 'F' });
            wave.Write(36 + bytes);
            wave.Write(new[] { 'W', 'A', 'V', 'E', 'f', 'm', 't', ' ' });
            wave.Write(16);
            wave.Write((short)1); // PCM
            wave.Write((short)1); // Mono
            wave.Write(SampleRate);
            wave.Write(SampleRate * sizeof(short));
            wave.Write((short)sizeof(short));
            wave.Write((short)16);
            wave.Write(new[] { 'd', 'a', 't', 'a' });
            wave.Write(bytes);
            for (int i = 0; i < count; i++)
            {
                float value = Mathf.Clamp(sample(i / (float)SampleRate), -1f, 1f);
                wave.Write((short)Mathf.RoundToInt(value * short.MaxValue));
            }
        }

        private static float CueSample(NeonAudio.Cue cue, float time, float duration)
        {
            float progress = time / duration;
            float envelope = Mathf.Min(1f, time / 0.011f) * (1f - progress) * (1f - progress);
            float texture = Mathf.Sin(14175f * time + 4.4f * Mathf.Sin(2454f * time));
            float signal = cue switch
            {
                NeonAudio.Cue.Jump => 0.52f * Sweep(time, 245f, 1020f) + 0.13f * Sweep(time, 490f, 1960f),
                NeonAudio.Cue.Land => 0.43f * Sweep(time, 142f, -570f) + 0.21f * texture,
                NeonAudio.Cue.Collect => 0.41f * Note(time, 0f, 659f, 0.18f) +
                    0.46f * Note(time, 0.09f, 988f, 0.21f),
                NeonAudio.Cue.Hurt => 0.37f * Sweep(time, 247f, -655f) +
                    0.17f * Sweep(time, 318f, -522f) + 0.13f * texture,
                NeonAudio.Cue.Win => 0.32f * Note(time, 0f, 440f, 0.34f) +
                    0.32f * Note(time, 0.13f, 554.37f, 0.35f) +
                    0.35f * Note(time, 0.26f, 659.25f, 0.48f) +
                    0.23f * Note(time, 0.39f, 880f, 0.56f),
                NeonAudio.Cue.Attack => 0.37f * Sweep(time, 740f, -2380f) +
                    0.17f * Sweep(time, 1280f, -3880f) + 0.20f * texture,
                NeonAudio.Cue.Phase => 0.41f * Sweep(time, 116f, 1990f) +
                    0.28f * Sweep(time, 680f, -1550f) + 0.12f * texture,
                _ => 0f
            };
            if (cue == NeonAudio.Cue.Collect || cue == NeonAudio.Cue.Win) return signal;
            return envelope * signal;
        }

        private static float Sweep(float time, float frequency, float changePerSecond)
        {
            return Mathf.Sin(Tau * time * (frequency + 0.5f * changePerSecond * time));
        }

        private static float Note(float time, float start, float frequency, float duration)
        {
            float age = time - start;
            if (age < 0f || age >= duration) return 0f;
            float envelope = Mathf.Min(1f, age / 0.012f) * (1f - age / duration);
            return envelope * (0.83f * Sweep(age, frequency, 0f) + 0.17f * Sweep(age, frequency * 2f, 0f));
        }

        private static float AmbienceSample(float time)
        {
            // Integer cycles over 16 seconds align the waveform and modulation at the seam.
            double turn = Math.PI * 2d * time / 16d;
            double slow = Math.Sin(turn * 2d);
            double pad = Math.Sin(turn * 880d + 0.19d * slow) * (0.17d + 0.045d * Math.Sin(turn));
            pad += Math.Sin(turn * 1320d + 0.12d * slow) * (0.075d + 0.038d * Math.Sin(turn * 3d));
            pad += Math.Sin(turn * 1760d + 0.15d * Math.Sin(turn * 3d)) * 0.036d;
            pad += Math.Sin(turn * 1759d) * 0.020d;
            pad += Math.Sin(turn * 2093d) * (0.027d + 0.014d * Math.Cos(turn * 2d));
            pad += Math.Sin(turn * 2637d) * (0.019d - 0.010d * Math.Cos(turn * 2d));
            double pulse = 0.5d + 0.5d * Math.Cos(turn * 12d);
            pad += Math.Sin(turn * 440d) * 0.065d * pulse * pulse * pulse;
            return (float)pad;
        }
    }
}
