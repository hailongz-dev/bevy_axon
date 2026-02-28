namespace BevyGraphics {
	[Bevy.BevyVariant(2236330566)]
	[System.Serializable]
	public class Position {
		public const uint TypeId = 2236330566;
		[UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.BeforeSceneLoad)]
		private static void Initialize() {
			Bevy.BevyClient.AddType(2236330566,typeof(Position));
		}
		public float x;
		public float y;
		public float z;
	}
	[Bevy.BevyVariant(1603518625)]
	[System.Serializable]
	public class Rotation {
		public const uint TypeId = 1603518625;
		[UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.BeforeSceneLoad)]
		private static void Initialize() {
			Bevy.BevyClient.AddType(1603518625,typeof(Rotation));
		}
		public float x;
		public float y;
		public float z;
	}
	[Bevy.BevyVariant(3503847833)]
	[System.Serializable]
	public class Scale {
		public const uint TypeId = 3503847833;
		[UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.BeforeSceneLoad)]
		private static void Initialize() {
			Bevy.BevyClient.AddType(3503847833,typeof(Scale));
		}
		public float x;
		public float y;
		public float z;
	}
	[Bevy.BevyVariant(974863171)]
	[System.Serializable]
	public class Page {
		public const uint TypeId = 974863171;
		[UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.BeforeSceneLoad)]
		private static void Initialize() {
			Bevy.BevyClient.AddType(974863171,typeof(Page));
		}
		public PageValue[] p;
		[System.Serializable]
		public struct PageValue {
			public string k;
			public string v;
		}
	}
	[Bevy.BevyVariant(3288927234)]
	[System.Serializable]
	public class Tilemap {
		public const uint TypeId = 3288927234;
		[UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.BeforeSceneLoad)]
		private static void Initialize() {
			Bevy.BevyClient.AddType(3288927234,typeof(Tilemap));
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
	[Bevy.BevyVariant(3683072690)]
	[System.Serializable]
	public class Skin {
		public const uint TypeId = 3683072690;
		[UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.BeforeSceneLoad)]
		private static void Initialize() {
			Bevy.BevyClient.AddType(3683072690,typeof(Skin));
		}
		public uint id;
		public string[] state;
	}
	[Bevy.BevyEvent(80005349)]
	[System.Serializable]
	public class PageEvent {
		public const uint TypeId = 80005349;
		[UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.BeforeSceneLoad)]
		private static void Initialize() {
			Bevy.BevyClient.AddType(80005349,typeof(PageEvent));
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
