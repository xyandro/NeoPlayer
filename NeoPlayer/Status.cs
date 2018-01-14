using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using NeoPlayer.Models;

namespace NeoPlayer
{
	public class Status
	{
		readonly SingleRunner broadcastChanges;
		readonly NeoServer neoServer;
		readonly Dictionary<string, object> values = new Dictionary<string, object>();
		readonly Dictionary<string, object> newValues = new Dictionary<string, object>();
		public Status(NeoServer neoServer)
		{
			this.neoServer = neoServer;
			broadcastChanges = new SingleRunner(BroadcastChanges);
		}

		public List<MediaData> Queue { set { UpdateValue(value); } }
		public List<MediaData> Cool { set { UpdateValue(value); } }
		public List<DownloadData> Downloads { set { UpdateValue(value); } }
		public List<MediaData> Movies { set { UpdateValue(value); } }
		public int MediaVolume { set { UpdateValue(value); } }
		public string MediaTitle { set { UpdateValue(value); } }
		public int MediaPosition { set { UpdateValue(value); } }
		public int MediaMaxPosition { set { UpdateValue(value); } }
		public bool MediaPlaying { set { UpdateValue(value); } }
		public string SlidesQuery { set { UpdateValue(value); } }
		public string SlidesSize { set { UpdateValue(value); } }
		public int SlideDisplayTime { set { UpdateValue(value); } }
		public bool SlidesPlaying { set { UpdateValue(value); } }

		void UpdateValue<T>(T value, [CallerMemberName] string name = null)
		{
			newValues[name] = value;
			broadcastChanges.Signal();
		}

		void BroadcastChanges()
		{
			var different = newValues.Keys.Where(name => (!values.ContainsKey(name)) || (!values[name].Equals(newValues[name]))).ToList();
			if (different.Any())
			{
				var message = new Message();
				message.Add(different.Count);
				foreach (var name in different)
				{
					var value = values[name] = newValues[name];
					message.Add(name);
					message.Add(value);
				}
				neoServer.SendToNeoRemotes(message.ToArray());
			}
			newValues.Clear();
		}

		public void SendAll(AsyncQueue<byte[]> queue)
		{
			var all = newValues.Concat(values).DistinctBy(x => x.Key).ToList();
			var message = new Message();
			message.Add(all.Count);
			foreach (var pair in all)
			{
				message.Add(pair.Key);
				message.Add(pair.Value);
			}
			queue.Enqueue(message.ToArray());
		}
	}
}
