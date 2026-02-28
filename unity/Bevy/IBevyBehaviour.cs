namespace Bevy
{
    public interface IBevyBehaviour
    {
        void SetValue(uint type, object data);
        void Invoke(uint type, object data);
    }
}