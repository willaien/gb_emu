namespace gb_emu
{
    public interface IMMUObject
    {
        byte ReadValue(int i);
        void WriteValue(int i, byte value);
    }
}