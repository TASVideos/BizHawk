﻿using System.Windows.Forms;
using System.IO;
using BizHawk.Emulation.DiscSystem;

namespace BizHawk.Client.DiscoHawk
{
	public class AudioExtractor
	{
		public static string FFmpegPath;

		public static void Extract(Disc disc, string path, string fileBase)
		{
			var dsr = new DiscSectorReader(disc);

			bool confirmed = false;
			var tracks = disc.Session1.Tracks;
			foreach (var track in tracks)
			{
				if (!track.IsAudio)
					continue;

				int trackLength = track.NextTrack.LBA - track.LBA;
				var waveData = new byte[trackLength * 2352];
				int startLba = track.LBA;
				for (int sector = 0; sector < trackLength; sector++)
				{
					dsr.ReadLBA_2352(startLba + sector, waveData, sector * 2352);
				}

				string mp3Path = $"{Path.Combine(path, fileBase)} - Track {track.Number:D2}.mp3";
				if (File.Exists(mp3Path))
				{
					if (!confirmed)
					{
						var dr = MessageBox.Show("This file already exists. Do you want extraction to proceed overwriting files, or cancel the entire operation immediately?", "File already exists", MessageBoxButtons.OKCancel);
						if (dr == DialogResult.Cancel) return;
						confirmed = true;
					}

					File.Delete(mp3Path);
				}

				string tempfile = Path.GetTempFileName();

				try
				{
					File.WriteAllBytes(tempfile, waveData);
					var ffmpeg = new FFMpeg();
					ffmpeg.Run("-f", "s16le", "-ar", "44100", "-ac", "2", "-i", tempfile, "-f", "mp3", "-ab", "192k", mp3Path);
				}
				finally
				{
					File.Delete(tempfile);
				}
			}
		}
	}
}
