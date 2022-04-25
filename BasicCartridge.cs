namespace gb_emu
{
    internal class BasicCartridge : IMMUObject
    {
        private byte[] _contents = new byte[0x8000];
        internal BasicCartridge(byte[] contents)
        {
            contents.CopyTo(_contents, 0);
        }
        public byte ReadValue(int i)
        {
            return _contents[i];
        }

        public void WriteValue(int i, byte value)
        {
            // do nothing
        }
    }
}