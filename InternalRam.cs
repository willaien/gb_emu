namespace gb_emu
{
    internal class InternalRam : IMMUObject
    {
        private byte[] _wram = new byte[1024 * 8];
        public byte ReadValue(int i)
        {
            return _wram[i - 0xC000]; // start of WRAM
        }

        public void WriteValue(int i, byte value)
        {
            _wram[i - 0xC000] = value;
        }
    }
}