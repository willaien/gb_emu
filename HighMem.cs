namespace gb_emu
{
    internal class HighMem : IMMUObject
    {
        internal HighMem()
        {
            _mem[0xFF44] = 0x90;
        }
        private byte[] _mem = new byte[0xFFFF]; // I'm lazy
        public byte ReadValue(int i)
        {
            return _mem[i];
        }

        public void WriteValue(int i, byte value)
        {
            _mem[i] = value;
            if (i == 0xFF01)
            {
                Console.Write(System.Text.Encoding.ASCII.GetString(new[] { value }));
            }
        }
    }
}