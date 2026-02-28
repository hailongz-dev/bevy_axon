using System.Collections.Generic;
using System.Linq;
using Bevy;
using UnityEngine;

namespace BevyGraphics
{
    public class BevyTilemap : BevyValueBehaviour<Tilemap>
    {
        private BevySkinProvider _skinProvider;
        private Grid _grid;
        private List<UnityEngine.Tilemaps.Tilemap> _layers;

        private UnityEngine.Tilemaps.Tilemap CreateLayer(List<UnityEngine.Tilemaps.Tilemap> vs)
        {
            var v = Instantiate(vs[0].gameObject, transform);
            var tr = v.transform;
            tr.localPosition = Vector3.zero;
            tr.localScale = Vector3.one;
            tr.localRotation = Quaternion.identity;
            var r = v.GetComponent<UnityEngine.Tilemaps.Tilemap>();
            vs.Add(r);
            return r;
        }

        protected override void OnValueChanged(Tilemap value)
        {
            if (value.layers == null || value.layers.Length == 0) return;
            _skinProvider ??= GetComponentInParent<BevySkinProvider>();
            if (!_skinProvider) return;
            _grid ??= GetComponent<Grid>() ?? gameObject.AddComponent<Grid>();
            _layers ??= GetComponentsInChildren<UnityEngine.Tilemaps.Tilemap>(true).ToList();

            if (_layers.Count == 0) return;

            _grid.cellSize = new Vector3(value.size, value.size, 1);

            for (var i = 0; i < value.layers.Length; i++)
            {
                var layer = value.layers[i];
                var view = i < _layers.Count ? _layers[i] : CreateLayer(_layers);
                var viewRenderer = view.GetComponent<UnityEngine.Tilemaps.TilemapRenderer>();
                if (!viewRenderer)
                {
                    viewRenderer.sortingLayerID = layer.index;
                }

                view.ClearAllTiles();

                for (var j = 0; j < layer.tiles.Length; j++)
                {
                    var x = j % value.width;
                    var y = j / value.width;

                    var tileData = layer.tiles[j];

                    var p = new Vector3Int(x, y, 0);

                    if (_skinProvider)
                    {
                        var skin = _skinProvider.GetSkin(tileData.skin);
                        if (skin != null && skin.tile)
                        {
                            view.SetTile(p, skin.tile);
                            continue;
                        }
                    }

                    view.SetTile(p, null);
                }

                view.RefreshAllTiles();
            }
        }
    }
}