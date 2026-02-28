using Bevy;
using UnityEngine;

namespace BevyGraphics
{
    public class BevySkin : BevyValueBehaviour<Skin>
    {
        private GameObject _skinObject;
        private Animator _skinAnimator;
        private Skin _skin;

        private BevySkinProvider _skinProvider;

        private void OnStateChange(string[] state)
        {
            if (!isActiveAndEnabled) return;
            if (!_skinAnimator) return;

            if (state == null || state.Length == 0)
            {
                _skinAnimator.enabled = false;
                return;
            }

            _skinAnimator.enabled = true;

            for (var i = 0; i < state.Length; i++)
            {
                _skinAnimator.Play(state[i], i);
            }
        }

        private void OnEnable()
        {
            if (_skin == null) return;
            OnStateChange(_skin.state);
        }

        protected override void OnValueChanged(Skin skin)
        {
            if (skin == null) return;
            if (_skin != null && skin.id == _skin.id)
            {
                OnStateChange(skin.state);
                return;
            }

            _skinProvider ??= GetComponentInParent<BevySkinProvider>();
            if (!_skinProvider) return;

            if (!_skinObject)
            {
                Destroy(_skinObject);
            }

            _skinAnimator = null;
            _skinObject = null;
            _skin = null;

            var item = _skinProvider.GetSkin(skin.id);

            if (item == null || !item.prefab) return;

            _skinObject = Instantiate(item.prefab, transform);
            var tr = _skinObject.transform;
            tr.localPosition = Vector3.zero;
            tr.localRotation = Quaternion.identity;
            tr.localScale = Vector3.one;
            _skin = skin;
            _skinAnimator = _skinObject.GetComponent<Animator>();
            OnStateChange(skin.state);
        }
    }
}