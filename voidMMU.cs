namespace gb_emu
{
    internal class voidMMU : IMMUObject
    {
        private byte _sb = 0;
        public byte ReadValue(int i) 
        {
            if (i == 0xFF01){
                return _sb;
            }
            else return 0;
        }

    public void WriteValue(int i, byte value) 
        { 
            if (i == 0xFF01)
            {
                _sb = value;
            }
            if (i == 0xFF02)
            {
                Console.Write(System.Text.Encoding.ASCII.GetString(new[] { value }));
            }
        }

        public static voidMMU Instance = new voidMMU();
    }
}