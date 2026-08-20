using Hazel;
using System;
using System.IO;
using TONE.Modules.Rpc;
using UnityEngine;

namespace TONE.Modules;

public static class CustomSoundsManager
{
    public static void RPCPlayCustomSound(this PlayerControl pc, string sound, float volume = 1f, float pitch = 1f, bool force = false)
    {
        if (pc == null || PlayerControl.LocalPlayer.PlayerId == pc.PlayerId)
        {
            Play(sound, volume, pitch);
            return;
        }
        if (!force) if (!AmongUsClient.Instance.AmHost || !pc.IsModded()) return;
        long now = Utils.TimeStamp;
        if (now == LastSoundRPCTS) return;
        LastSoundRPCTS = now;
        RpcUtils.LateSpecificSendMessage(new RpcPlayCustomSound(pc.NetId, sound, volume, pitch), pc.GetClientId());
    }

    public static void RPCPlayCustomSoundAll(string sound, float volume = 1f, float pitch = 1f)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        Play(sound);
        long now = Utils.TimeStamp;
        if (now == LastSoundRPCTS) return;
        LastSoundRPCTS = now;
        RpcUtils.LateBroadcastReliableMessage(new RpcPlayCustomSound(PlayerControl.LocalPlayer.NetId, sound, volume, pitch));
    }

    public static void ReceiveRPC(MessageReader reader) => Play(reader.ReadString(), reader.ReadSingle(), reader.ReadSingle());

    private static readonly string SOUNDS_PATH = OperatingSystem.IsAndroid() ? @$"{Main.Path}/TONE-DATA/resources/" : @$"{Environment.CurrentDirectory.Replace(@"\", "/")}/BepInEx/resources/";

    public static long LastSoundRPCTS;

    public static void Play(string sound, float volume = 1f, float pitch = 1f, bool loop = false)
    {
        if (!Constants.ShouldPlaySfx() || !Main.EnableCustomSoundEffect.Value || OperatingSystem.IsAndroid()) return;

        var path = Path.Combine(SOUNDS_PATH, sound + ".wav");

        if (!Directory.Exists(SOUNDS_PATH))
            Directory.CreateDirectory(SOUNDS_PATH);

        DirectoryInfo folder = new(SOUNDS_PATH);
        if ((folder.Attributes & FileAttributes.Hidden) != FileAttributes.Hidden)
            folder.Attributes = FileAttributes.Hidden;

        if (!File.Exists(path))
        {
            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("TONE.Resources.Sounds." + sound + ".wav");
            if (stream == null)
            {
                Logger.Warn($"Sound file missing：{sound}", "CustomSounds");
                return;
            }
            var fs = File.Create(path);
            stream.CopyTo(fs);
            fs.Close();
        }

        StartPlay(path, volume, pitch, loop);
        Logger.Msg($"play sound：{sound}", "CustomSounds");
    }

    private static readonly Dictionary<string, AudioClip> audioCache = [];

    private static void StartPlay(string path, float volume = 1f, float pitch = 1f, bool loop = false)
    {
        if (!audioCache.TryGetValue(path, out var clip) || !clip)
        {
            if (audioCache.ContainsKey(path)) audioCache.Remove(path);
            clip = LoadWav(path);
            audioCache[path] = clip;
        }

        if (clip)
        {
            if (loop) SoundManager.Instance.PlaySound(clip, true, 25f);
            else SoundManager.Instance.PlaySoundImmediate(clip, false, volume, pitch);
        }
    }

    public static AudioClip LoadWav(string path)
    {
        byte[] fileData = File.ReadAllBytes(path);

        WAV wav = new(fileData);

        AudioClip clip = AudioClip.Create(Path.GetFileNameWithoutExtension(path), wav.SampleCount, 1, wav.Frequency, false, false); 
        clip.SetData(wav.LeftChannel, 0);

        return clip;
    }

    public class WAV  {

		// convert two bytes to one float in the range -1 to 1
		static float BytesToFloat(byte firstByte, byte secondByte) {
			// convert two bytes to one short (little endian)
			short s = (short)((secondByte << 8) | firstByte);
			// convert to range from -1 to (just below) 1
			return s / 32768.0F;
		}

		static int BytesToInt(byte[] bytes, int offset = 0){
			int value=0;
			for (int i=0;i<4;i++)
            {
				value |= bytes[offset + i] << (i*8);
			}
			return value;
		}

		private static byte[] GetBytes(string filename){
			return File.ReadAllBytes(filename);
		}
		// properties
		public float[] LeftChannel{get; internal set;}
		public float[] RightChannel{get; internal set;}
		public int ChannelCount {get; internal set;}
		public int SampleCount {get; internal set;}
		public int Frequency {get; internal set;}
        public int BitsPerSample { get; internal set; }
		
		// Returns left and right double arrays. 'right' will be null if sound is mono.
		public WAV(string filename):
			this(GetBytes(filename)) {}

		public WAV(byte[] wav){
			// Determine if mono or stereo
			ChannelCount = wav[22];     // Forget byte 23 as 99.999% of WAVs are 1 or 2 channels

			// Get the frequency
			Frequency = BytesToInt(wav, 24);
			
            BitsPerSample = wav[34] + (wav[35] << 8);

            int bytesPerSample = BitsPerSample / 8;

			// Get past all the other sub chunks to get to the data subchunk:
			int pos = 12;   // First Subchunk ID from 12 to 16
			
			// Keep iterating until we find the data chunk (i.e. 64 61 74 61 ...... (i.e. 100 97 116 97 in decimal))
			while (!(wav[pos] == 100 && wav[pos+1] == 97 && wav[pos+2] == 116 && wav[pos+3] == 97)) 
            {
				pos += 4;
				int chunkSize = wav[pos] + wav[pos + 1] * 256 + wav[pos + 2] * 65536 + wav[pos + 3] * 16777216;
				pos += 4 + chunkSize;
			}
			pos += 4;                     // skip "data"
            int dataSize = BytesToInt(wav, pos);
            pos += 4;                     // now at PCM data
			
			// Pos is now positioned to start of actual sound data.
			SampleCount = dataSize / bytesPerSample / ChannelCount;
			
			// Allocate memory (right will be null if only mono sound)
			LeftChannel = new float[SampleCount];
			if (ChannelCount == 2) RightChannel = new float[SampleCount];
			else RightChannel = null;

            int end = pos + dataSize;
			
			// Write to double array/s:
			int i = 0;
			while (pos + (ChannelCount * bytesPerSample) <= end && i < SampleCount) {
				LeftChannel[i] = ReadSample(wav, pos, BitsPerSample);

     		    pos += bytesPerSample;

     		    if (ChannelCount == 2) 
                {
                    RightChannel[i] = ReadSample(wav, pos, BitsPerSample);
                    pos += bytesPerSample;
                }
                i++;
            }
        }

        public float ReadSample(byte[] bytes, int offset, int bits)
        {
            int sample = 0;
            int bytesPer = bits / 8;
            for (int j = 0; j < bytesPer; j++)
                sample |= bytes[offset + j] << (8 * j);

            int maxVal = 1 << (bits - 1);
            if (sample >= maxVal)
                sample -= 1 << bits;

            return (float)sample / maxVal;
        }

        public float[] GetStereoData()
        {
            if (RightChannel == null) return LeftChannel;

            float[] stereoData = new float[SampleCount * 2];

            for (int i = 0; i < SampleCount; i++)
            {
                stereoData[i * 2] = LeftChannel[i]; // Left channel data
                stereoData[i * 2 + 1] = RightChannel[i]; // Right channel data
            }

            return stereoData;
        }

        public override string ToString()
        {
            return $"[WAV: LeftChannel={LeftChannel}, RightChannel={RightChannel}, ChannelCount={ChannelCount}, SampleCount={SampleCount}, Frequency={Frequency}]";
        }
    }
}