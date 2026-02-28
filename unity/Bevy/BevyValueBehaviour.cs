namespace Bevy
{
    public abstract class BevyValueBehaviour<T> : BevyBehaviour
    {
        protected abstract void OnValueChanged(T value);

        public override void SetValue(uint type, object data)
        {
            if (data is T v)
            {
                OnValueChanged(v);
            }
        }
    }
}