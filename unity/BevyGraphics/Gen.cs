namespace BevyGraphics {
	[Bevy.BevyVariant(1000)]
	[System.Serializable]
	public class Position {
		public const uint TypeId = 1000;
		[UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.BeforeSceneLoad)]
		private static void Initialize() {
			Bevy.BevyClient.AddType(1000,typeof(Position));
		}
		public float x;
		public float y;
		public float z;
	}
	[Bevy.BevyVariant(1001)]
	[System.Serializable]
	public class Rotation {
		public const uint TypeId = 1001;
		[UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.BeforeSceneLoad)]
		private static void Initialize() {
			Bevy.BevyClient.AddType(1001,typeof(Rotation));
		}
		public float x;
		public float y;
		public float z;
	}
	[Bevy.BevyVariant(1002)]
	[System.Serializable]
	public class Scale {
		public const uint TypeId = 1002;
		[UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.BeforeSceneLoad)]
		private static void Initialize() {
			Bevy.BevyClient.AddType(1002,typeof(Scale));
		}
		public float x;
		public float y;
		public float z;
	}
	[Bevy.BevyVariant(1300)]
	[System.Serializable]
	public class Page {
		public const uint TypeId = 1300;
		[UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.BeforeSceneLoad)]
		private static void Initialize() {
			Bevy.BevyClient.AddType(1300,typeof(Page));
		}
		public PageValue[] p;
		[System.Serializable]
		public struct PageValue {
			public string k;
			public string v;
		}
	}
	[Bevy.BevyVariant(1200)]
	[System.Serializable]
	public class Tilemap {
		public const uint TypeId = 1200;
		[UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.BeforeSceneLoad)]
		private static void Initialize() {
			Bevy.BevyClient.AddType(1200,typeof(Tilemap));
		}
		public int width;
		public int height;
		public float size;
		public Layer[] layers;
		[System.Serializable]
		public struct Layer {
			public int index;
			public Tile[] tiles;
			[System.Serializable]
			public struct Tile {
				public uint skin;
				public uint flags;
			}
		}
	}
	[Bevy.BevyVariant(1100)]
	[System.Serializable]
	public class Skin {
		public const uint TypeId = 1100;
		[UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.BeforeSceneLoad)]
		private static void Initialize() {
			Bevy.BevyClient.AddType(1100,typeof(Skin));
		}
		public uint id;
		public string[] state;
	}
	[Bevy.BevyEvent(1301)]
	[System.Serializable]
	public class PageEvent {
		public const uint TypeId = 1301;
		[UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.BeforeSceneLoad)]
		private static void Initialize() {
			Bevy.BevyClient.AddType(1301,typeof(PageEvent));
		}
		public ulong client_id;
		public ulong id;
		public string n;
		public PageValue[] p;
		[System.Serializable]
		public struct PageValue {
			public string k;
			public string v;
		}
	}
}
