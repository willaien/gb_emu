namespace gb_emu
{
    public class Registers
    {
        private const byte Z_Flag = 0b1000_0000;
        private const byte N_Flag = 0b0100_0000;
        private const byte H_Flag = 0b0010_0000;
        private const byte C_Flag = 0b0001_0000;
        private byte a = 0x01;
        private byte b;
        private byte c = 0x13;
        private byte d;
        private byte e = 0xD8;
        private byte f = 0xB0;
        private byte h = 0x01;
        private byte l = 0x4D;
        public byte A { get => a; set => a = value; }
        public byte B { get => b; set => b = value; }
        public byte C { get => c; set => c = value; }
        public byte D { get => d; set => d = value; }
        public byte E { get => e; set => e = value; }
        public byte F { get => f; set => f = (byte)(value & 0xF0); } // Flags register, holds 8 distinct values. Methods to read and reset these flags are supplied.
        public byte H { get => h; set => h = value; }
        public byte L { get => l; set => l = value; }
        public ushort IX { get; set; } // these ushort values can be standard properties with hidden backing values since no composite registers exist for them.
        public ushort IY { get; set; }
        public ushort SP { 
            get; 
            set; } = 0xFFFE;
        public ushort PC { get; set; } = 0x100; // This is the program counter, whatever value is here is the next value that will be read and executed.
        public byte I { get; set; } // Interrupt vector
        public byte R { get; set; } // Refresh counter, should be reset to 0 whenever memory is read.
        public ushort BC
        {
            get
            {
                ushort bc = 0;
                bc |= b;
                bc <<= 8;
                bc |= c;
                return bc;
            }
            set
            {
                ushort bc = value;
                c = (byte)(bc & 0xff);
                b = (byte)((bc >> 8) & 0xff);
            }
        } // this is a composite register that treats the B and C registers as a single unsigned 16-bit integer. Same for the following registers.
        public ushort DE
        {
            get
            {
                ushort de = 0;
                de |= d;
                de <<= 8;
                de |= e;
                return de;
            }
            set
            {
                ushort de = value;
                e = (byte)(de & 0xff);
                d = (byte)((de >> 8) & 0xff);
            }
        }
               public ushort AF
        {
            get
            {
                ushort af = 0;
                af |= a;
                af <<= 8;
                af |= f;
                return af;
            }
            set
            {
                ushort af = value;
                f = (byte)(af & 0xff);
                a = (byte)((af >> 8) & 0xff);
            }
        }
        public ushort HL
        {
            get
            {
                ushort hl = 0;
                hl |= h;
                hl <<= 8;
                hl |= l;
                return hl;
            }
            set
            {
                ushort hl = value;
                l = (byte)(hl & 0xff);
                h = (byte)((hl >> 8) & 0xff);
            }
        }
        #region Flag methods
        public void SetH(){
            f |= H_Flag;
        }
        public void ResetH(){
            f &= invertFlag(H_Flag); // invert, mask, or to f to set the value to 0 while preserving all other values
        }
        public void SetOrResetH(bool overflow)
        {
            if (overflow)
            {
                SetH();
            }
            else
            {
                ResetH();
            }
        }
        public bool ReadH(){
            return (f & H_Flag) > 0;
        }
        public void SetN(){
            f |= N_Flag;
        }
        public void ResetN(){
            f &= invertFlag(N_Flag); // invert, mask, or to f to set the value to 0 while preserving all other values
        }
        public bool ReadN(){
            return (f & N_Flag) > 0;
        }
        public void SetC(){
            f |= C_Flag;
        }
        public void ResetC(){
            f &= invertFlag(C_Flag); // invert, mask, or to f to set the value to 0 while preserving all other values
        }
        public void SetOrResetC(bool overflow)
        {
            if (overflow)
            {
                SetC();
            }
            else
            {
                ResetC();
            }
        }
        public bool ReadC(){
            return (f & C_Flag) > 0;
        }
        public void SetOrResetZ(byte b)
        {
            if (b == 0)
            {
                SetZ();
            }
            else
            {
                ResetZ();
            }
        }
        public void SetOrResetZ(ushort b)
        {
            if (b == 0)
            {
                SetZ();
            }
            else
            {
                ResetZ();
            }
        }
        public bool ReadZ()
        {
            return (f & Z_Flag) > 0;
        }
        public void SetZ()
        {
            f |= Z_Flag;
        }
        public void ResetZ(){
            f &= invertFlag(Z_Flag); // invert, mask, or to f to set the value to 0 while preserving all other values
        }
        private static byte invertFlag(byte b)
        {
            return (byte)(~b);
        }
        #endregion Flag methods
    }
}