using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Tilemaps;

namespace BevyGraphics
{
    public class BevySkinProvider : MonoBehaviour
    {
        [FormerlySerializedAs("Skin")] public List<Skin> skins = new();

        [Serializable]
        public class Skin
        {
            public uint id;
            public GameObject prefab;
            public TileBase tile;
        }

        private Dictionary<uint, Skin> _skinSet;

        public Skin GetSkin(uint id)
        {
            if (_skinSet != null) return _skinSet.GetValueOrDefault(id);
            _skinSet = new Dictionary<uint, Skin>();
            foreach (var skin in skins)
            {
                _skinSet[skin.id] = skin;
            }

            return _skinSet.GetValueOrDefault(id);
        }
    }
}