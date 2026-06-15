// ============================================================================
// Licensed under the MIT License. See LICENSE file for details.
// ============================================================================
using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

namespace SIA.Audio
{
	public static class SaveWav
	{
		private const int HeaderSize = 44;

		public static byte[] Save(string filename, AudioClip clip)
		{
			if (!filename.ToLower().EndsWith(".wav"))
			{
				filename += ".wav";
			}

			var filepath = Path.Combine(Application.persistentDataPath, filename);

			Directory.CreateDirectory(Path.GetDirectoryName(filepath) ?? string.Empty);

			using (var memoryStream = CreateEmpty())
			{
				ConvertAndWrite(memoryStream, clip);
				WriteHeader(memoryStream, clip);

				return memoryStream.GetBuffer();
			}
		}

		public static AudioClip TrimSilence(AudioClip clip, float min)
		{
			var samples = new float[clip.samples];

			clip.GetData(samples, 0);

			return TrimSilence(new List<float>(samples), min, clip.channels, clip.frequency);
		}

		public static AudioClip TrimSilence(List<float> samples, float min, int channels, int hz, bool stream = false)
		{
			int i;

			for (i = 0; i < samples.Count; i++)
			{
				if (Mathf.Abs(samples[i]) > min)
				{
					break;
				}
			}

			samples.RemoveRange(0, i);

			for (i = samples.Count - 1; i > 0; i--)
			{
				if (Mathf.Abs(samples[i]) > min)
				{
					break;
				}
			}

			samples.RemoveRange(i, samples.Count - i);

			var clip = AudioClip.Create("TempClip", samples.Count, channels, hz, stream);

			clip.SetData(samples.ToArray(), 0);

			return clip;
		}

		static MemoryStream CreateEmpty()
		{
			var memoryStream = new MemoryStream();
			byte emptyByte = new byte();

			for (int i = 0; i < HeaderSize; i++)
			{
				memoryStream.WriteByte(emptyByte);
			}

			return memoryStream;
		}

		static void ConvertAndWrite(MemoryStream memoryStream, AudioClip clip)
		{
			var samples = new float[clip.samples];

			clip.GetData(samples, 0);

			Int16[] intData = new Int16[samples.Length];
			Byte[] bytesData = new Byte[samples.Length * 2];
			int rescaleFactor = 32767;

			for (int i = 0; i < samples.Length; i++)
			{
				intData[i] = (short)(samples[i] * rescaleFactor);
				Byte[] byteArr = BitConverter.GetBytes(intData[i]);
				byteArr.CopyTo(bytesData, i * 2);
			}

			memoryStream.Write(bytesData, 0, bytesData.Length);
		}

		static void WriteHeader(MemoryStream memoryStream, AudioClip clip)
		{
			var hz = clip.frequency;
			var channels = clip.channels;
			var samples = clip.samples;

			memoryStream.Seek(0, SeekOrigin.Begin);

			Byte[] riff = System.Text.Encoding.UTF8.GetBytes("RIFF");
			memoryStream.Write(riff, 0, 4);

			Byte[] chunkSize = BitConverter.GetBytes(memoryStream.Length - 8);
			memoryStream.Write(chunkSize, 0, 4);

			Byte[] wave = System.Text.Encoding.UTF8.GetBytes("WAVE");
			memoryStream.Write(wave, 0, 4);

			Byte[] fmt = System.Text.Encoding.UTF8.GetBytes("fmt ");
			memoryStream.Write(fmt, 0, 4);

			Byte[] subChunk1 = BitConverter.GetBytes(16);
			memoryStream.Write(subChunk1, 0, 4);

			UInt16 one = 1;

			Byte[] audioFormat = BitConverter.GetBytes(one);
			memoryStream.Write(audioFormat, 0, 2);

			Byte[] numChannels = BitConverter.GetBytes(channels);
			memoryStream.Write(numChannels, 0, 2);

			Byte[] sampleRate = BitConverter.GetBytes(hz);
			memoryStream.Write(sampleRate, 0, 4);

			Byte[] byteRate = BitConverter.GetBytes(hz * channels * 2);
			memoryStream.Write(byteRate, 0, 4);

			UInt16 blockAlign = (ushort)(channels * 2);
			memoryStream.Write(BitConverter.GetBytes(blockAlign), 0, 2);

			UInt16 bps = 16;
			Byte[] bitsPerSample = BitConverter.GetBytes(bps);
			memoryStream.Write(bitsPerSample, 0, 2);

			Byte[] datastring = System.Text.Encoding.UTF8.GetBytes("data");
			memoryStream.Write(datastring, 0, 4);

			Byte[] subChunk2 = BitConverter.GetBytes(samples * channels * 2);
			memoryStream.Write(subChunk2, 0, 4);
		}
	}
}