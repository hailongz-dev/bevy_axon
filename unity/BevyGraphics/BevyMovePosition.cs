using System;
using System.Collections.Generic;
using Bevy;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.Serialization;

namespace BevyGraphics
{
    public class BevyMovePosition : BevyValueBehaviour<MovePosition>
    {
        [FormerlySerializedAs("InterpolationDelay")]
        public float interpolationDelay = 0.06f;

        private readonly Queue<Item> _queue = new();
        private readonly ObjectPool<Item> _pool = new(() => new Item());


        [Serializable]
        private class Item
        {
            public Vector2 position;
            public float serverTime;
        }

        private MovePosition _position;

        protected override void OnValueChanged(MovePosition value)
        {
            if (value == null) return;

            if (_position == null)
            {
                transform.localPosition = new Vector3(value.x, value.y, 0);
            }

            _position = value;

            var item = _pool.Get();
            item.position = new Vector2(value.x, value.y);
            item.serverTime = Time.time;

            _queue.Enqueue(item);

            // 避免队列无限增长
            while (_queue.Count > 10)
            {
                var old = _queue.Dequeue();
                _pool.Release(old);
            }
        }

        private void Update()
        {
            if (_queue.Count < 2)
                return;

            var renderTime = Time.time - interpolationDelay;

            // 找到 renderTime 前后的两个点
            Item from = null;
            Item to = null;

            foreach (var item in _queue)
            {
                if (item.serverTime <= renderTime)
                    from = item;

                if (!(item.serverTime > renderTime)) continue;
                to = item;
                break;
            }

            if (from == null || to == null)
                return;

            var total = to.serverTime - from.serverTime;
            if (total <= 0.0001f)
                return;

            var t = (renderTime - from.serverTime) / total;
            t = Mathf.Clamp01(t);

            transform.localPosition = Vector2.Lerp(from.position, to.position, t);

            // 清理已经过去的点
            while (_queue.Count > 0 && _queue.Peek().serverTime < renderTime - 0.2f)
            {
                var old = _queue.Dequeue();
                _pool.Release(old);
            }
        }
    }
}