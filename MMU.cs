using System.Runtime.CompilerServices;

namespace gb_emu
{
    public enum MMUAccess
        {
            Read,
            Write
        }
    public class MMU 
    {
        private IMMUObject? _cart;
        private IMMUObject _highMem = new HighMem();
        private IMMUObject _internalRam = new InternalRam();
        //private byte[] _wram = new byte[1024 * 8]; // CGB note, make the last 4k bank switchable
        public void LoadCartridge(string path) // this will be replaced, it should not reside here.
        {
            var contents = File.ReadAllBytes(path);
            _cart = new BasicCartridge(contents);
        }
        private Action<MMUAccess> accessCallback;
        public MMU(Action<MMUAccess> onAccess)
        {
            accessCallback = onAccess;
        }
        public byte this[int i]
        {
            get { return read(i); }
            set { write(i, value); }
        }
        private byte read(int i)
        {
            accessCallback(MMUAccess.Read);
            return map(i).ReadValue(i);
        }
        private void write (int i, byte value)
        {
            accessCallback(MMUAccess.Write);
            map(i).WriteValue(i, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private IMMUObject map(int i)
        {
            switch(i)
            {
                case int n when (n >= 0 && n <= 0x7FFF): // cartridge memory
                    if (_cart == null) throw new Exception("Attempt to read or write to unloaded cartridge");
                    return _cart;
                case int n when (n >= 0x8000 && n <= 0x9FFF): // VRAM 
                    return _highMem;
                case int n when (n>= 0xA000 && n <= 0xBFFF): // external sram
                    return _highMem;
                case int n when (n >= 0xC000 && n <= 0xDFFF): // internal memory
                    return _internalRam;
                case int n when (n >= 0xFE00 && n <= 0xFE9F): // OAM
                    return voidMMU.Instance;
                case int n when (n >= 0xFF00 && n <= 0xFF7F): // I/O
                    return _highMem;
                case int n when (n >= 0xFF80 && n <= 0xFFFE): // High RAM
                    return _highMem;
                case int n when (n == 0xFFFF): // interrupt register
                    return voidMMU.Instance;
                case int _:
                    return voidMMU.Instance;
            }
        }
    }
}