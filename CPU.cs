using System.Runtime.CompilerServices;

namespace gb_emu
{

    public class CPU
    {
        public CPU()
        {
            CBActions = populateCBTable();
            Memory = new MMU(_ => CycleDelay++);
        }
        public MMU Memory;
        public long Cycles = 0;
        public int CycleDelay = 0;
        public bool InterruptEnabled = true;
        public Registers CPURegisters = new Registers();
        private Action[] CBActions;
        private HashSet<byte> _opcodes = new HashSet<byte>();
        public void NextCycle() // this method will need some cleanup, it's a fairly naive and unstructured implementation.
        {
            Cycles += 1;
            if (CycleDelay > 0) // skip as many cycles as needed for rough cycle accuracy.
            {
                CycleDelay -= 1;
                return;
            }
            //Console.WriteLine("A: {0:X2} F: {1:X2} B: {2:X2} C: {3:X2} D: {4:X2} E: {5:X2} H: {6:X2} L: {7:X2} SP: {8:X4} PC: 00:{9:X4} ({10:X2} {11:X2} {12:X2} {13:X2})", CPURegisters.A, CPURegisters.F, CPURegisters.B, CPURegisters.C, CPURegisters.D, CPURegisters.E, CPURegisters.H, CPURegisters.L, CPURegisters.SP, CPURegisters.PC, Memory[CPURegisters.PC], Memory[CPURegisters.PC+1], Memory[CPURegisters.PC+2], Memory[CPURegisters.PC+3]);
            var currentOpCode = readNextByte();
            switch (currentOpCode)
            {
                case 0x00: // nop
                    break;
                case 0x01: // ld bc, xx
                    CPURegisters.C = readNextByte();
                    CPURegisters.B = readNextByte();
                    break;
                case 0x02: // ld (bc) a; (bc) indicates the address
                    Memory[CPURegisters.BC] = CPURegisters.A;
                    break;
                case 0x03: // inc bc
                    CPURegisters.BC = inc(CPURegisters.BC);
                    break;
                case 0x04: // inc b
                    CPURegisters.B = inc(CPURegisters.B);
                    break;
                case 0x05: // dec b
                    CPURegisters.B = dec(CPURegisters.B);
                    break;
                case 0x06: // ld b, x
                    CPURegisters.B = readNextByte();
                    break;
                case 0x07: // rlca
                    {
                        var value = CPURegisters.A << 1;
                        if (value > 0xFF)
                        {
                            CPURegisters.SetC();
                            value |= 0b0000_0001;
                        }
                        else
                        {
                            CPURegisters.ResetC();
                        }
                        CPURegisters.ResetZ();
                        CPURegisters.ResetH();
                        CPURegisters.ResetN();
                        CPURegisters.A = (byte)value;
                    }

                    break;
                case 0x08: // ld (xx), SP TODO; double-check this
                    {
                        var address = read_ushort();
                        var bytes = BitConverter.GetBytes(CPURegisters.SP);
                        Memory[address] = bytes[0];
                        Memory[address + 1] = bytes[1];
                        break;
                    }
                case 0x09: // add hl, bc
                    {
                        var (overflow, newValue) = uncheckedAdd(CPURegisters.HL, CPURegisters.BC);
                        CPURegisters.ResetN();
                        CPURegisters.SetOrResetH((CPURegisters.HL & 0xFFF) + (CPURegisters.BC & 0xFFF) > 0xFFF);
                        CPURegisters.SetOrResetC(overflow);
                        CPURegisters.HL = newValue;
                        break;
                    }
                case 0x0A: // ld a, (bc)
                    CPURegisters.A = Memory[CPURegisters.BC];
                    break;
                case 0x0B: // dec bc
                    {
                        CPURegisters.BC = dec(CPURegisters.BC);
                        break;
                    }
                case 0x0C: // inc c
                    CPURegisters.C = inc(CPURegisters.C);
                    break;
                case 0x0D: // dec c
                    CPURegisters.C = dec(CPURegisters.C);
                    break;
                case 0x0E: // ld c, x
                    CPURegisters.C = readNextByte();
                    break;
                case 0x0F: // rrca TODO: double check this
                    {
                        var value = CPURegisters.A >> 1;
                        if ((CPURegisters.A & 1) == 1)
                        {
                            CPURegisters.SetC();
                            value |= 0b1000_0000;
                        }
                        else
                        {
                            CPURegisters.ResetC();
                        }
                        CPURegisters.ResetZ();
                        CPURegisters.ResetH();
                        CPURegisters.ResetN();
                        CPURegisters.A = (byte)value;
                        break;
                    }
                case 0x10: // STOP instruction, wtf is this
                    break;
                case 0x11: // ld de, xx
                    CPURegisters.E = readNextByte();
                    CPURegisters.D = readNextByte();
                    break;
                case 0x12: // ld (de) a
                    Memory[CPURegisters.DE] = CPURegisters.A;
                    break;
                case 0x13: // inc de
                    CPURegisters.DE = inc(CPURegisters.DE);
                    break;
                case 0x14: // inc d
                    CPURegisters.D = inc(CPURegisters.D);
                    break;
                case 0x15: // dec d
                    CPURegisters.D = dec(CPURegisters.D);
                    break;
                case 0x16: // ld d, x
                    CPURegisters.D = readNextByte();
                    break;
                case 0x17: // rla (c flag)
                    {
                        var value = CPURegisters.A << 1;
                        value |= CPURegisters.ReadC() ? 0b0000_0001 : 0;
                        if (value > 0xFF)
                        {
                            CPURegisters.SetC();
                        }
                        else
                        {
                            CPURegisters.ResetC();
                        }
                        CPURegisters.ResetZ();
                        CPURegisters.ResetH();
                        CPURegisters.ResetN();
                        CPURegisters.A = (byte)value;
                    }
                    break;
                case 0x18: // jr x
                    {
                        var relative = (sbyte)readNextByte();
                        CPURegisters.PC = (ushort)(CPURegisters.PC + relative);
                        break;
                    }
                case 0x19: // add hl, de
                    {
                        var (overflow, newValue) = uncheckedAdd(CPURegisters.DE, CPURegisters.HL);
                        CPURegisters.ResetN();
                        CPURegisters.SetOrResetH((CPURegisters.HL & 0xFFF) + (CPURegisters.DE & 0xFFF) > 0xFFF);
                        CPURegisters.SetOrResetC(overflow);
                        CPURegisters.HL = newValue;
                        break;
                    }
                case 0x1A: // ld a, (de)
                    CPURegisters.A = Memory[CPURegisters.DE];
                    break;
                case 0x1B: // dec de
                    CPURegisters.DE = dec(CPURegisters.DE);
                    break;
                case 0x1C: // inc e
                    CPURegisters.E = inc(CPURegisters.E);
                    break;
                case 0x1D: // dec e
                    CPURegisters.E = dec(CPURegisters.E);
                    break;
                case 0x1E: // ld e, x
                    CPURegisters.E = readNextByte();
                    break;
                case 0x1F: // rra ???
                    {
                        var value = CPURegisters.A >> 1;
                        value |= CPURegisters.ReadC() ? 0b1000_0000 : 0x0;
                        if ((CPURegisters.A & 1) == 1)
                        {
                            CPURegisters.SetC();
                        }
                        else
                        {
                            CPURegisters.ResetC();
                        }
                        CPURegisters.ResetZ();
                        CPURegisters.ResetH();
                        CPURegisters.ResetN();
                        CPURegisters.A = (byte)value; 
                    }
                    break;
                case 0x20: // JR NZ, X
                    {
                        var relative = (sbyte)readNextByte();
                        if (!CPURegisters.ReadZ())
                        {
                            CPURegisters.PC = (ushort)(CPURegisters.PC + relative);
                        }
                        break;
                    }
                case 0x21: // ld hl, xx
                    CPURegisters.L = readNextByte();
                    CPURegisters.H = readNextByte();
                    break;
                case 0x22: // ld (hl+), a
                    Memory[CPURegisters.HL++] = CPURegisters.A;
                    break;
                case 0x23: // inc hl
                    CPURegisters.HL = inc(CPURegisters.HL);
                    break;
                case 0x24: // inc h
                    CPURegisters.H = inc(CPURegisters.H);
                    break;
                case 0x25: // dec h
                    CPURegisters.H = dec(CPURegisters.H);
                    break;
                case 0x26: // ld h, x
                    CPURegisters.H = readNextByte();
                    break;
                case 0x27: // DAA ???
                    if (!CPURegisters.ReadN())
                    {  // after an addition, adjust if (half-)carry occurred or if result is out of bounds
                        if (CPURegisters.ReadC() || CPURegisters.A > 0x99)
                        {
                            CPURegisters.A += 0x60;
                            CPURegisters.SetC();
                        }
                        if (CPURegisters.ReadH() || (CPURegisters.A & 0x0f) > 0x09)
                        {
                            CPURegisters.A += 0x6;
                        }
                    }
                    else
                    {  // after a subtraction, only adjust if (half-)carry occurred
                        if (CPURegisters.ReadC())
                        {
                            CPURegisters.A -= 0x60;
                            CPURegisters.SetC();
                        }
                        if (CPURegisters.ReadH())
                        {
                            CPURegisters.A -= 0x6;
                        }
                    }
                    // these flags are always updated
                    CPURegisters.SetOrResetZ(CPURegisters.A); // the usual z flag
                    CPURegisters.ResetH(); // h flag is always cleared
                    break;
                case 0x28: // jr z, x
                    {
                        var relative = (sbyte)readNextByte();
                        if (CPURegisters.ReadZ())
                        {
                            CPURegisters.PC = (ushort)(CPURegisters.PC + relative);
                        }
                        break;
                    }
                case 0x29: // ADD HL, HL; TODO: fix half carry
                    {
                        var (overflow, newValue) = uncheckedAdd(CPURegisters.HL, CPURegisters.HL);
                        CPURegisters.ResetN();
                        CPURegisters.SetOrResetH((CPURegisters.HL & 0xFFF) + (CPURegisters.HL & 0xFFF) > 0xFFF);
                        CPURegisters.SetOrResetC(overflow);
                        CPURegisters.HL = newValue;
                        break;
                    }
                case 0x2A: // ld a, (hl+)
                    CPURegisters.A = Memory[CPURegisters.HL++];
                    break;
                case 0x2B: // dec hl
                    CPURegisters.HL = dec(CPURegisters.HL);
                    break;
                case 0x2C: // inc l
                    CPURegisters.L = inc(CPURegisters.L);
                    break;
                case 0x2D: // dec l
                    CPURegisters.L = dec(CPURegisters.L);
                    break;
                case 0x2E: // ld l, x
                    CPURegisters.L = readNextByte();
                    break;
                case 0x2F: // CPL; Inverts the A register?
                    CPURegisters.A = (byte)~CPURegisters.A;
                    CPURegisters.SetN();
                    CPURegisters.SetH();
                    break;
                case 0x30: // JR NC, X
                    {
                        var relative = (sbyte)readNextByte();
                        if (!CPURegisters.ReadC())
                        {
                            CPURegisters.PC = (ushort)(CPURegisters.PC + relative);
                        }
                        break;
                    }
                case 0x31: // LD SP, XX
                    CPURegisters.SP = BitConverter.ToUInt16(new[] { readNextByte(), readNextByte() });
                    break;
                case 0x32: // ld (hl-), a
                    Memory[CPURegisters.HL--] = CPURegisters.A;
                    break;
                case 0x33: // inc sp
                    CPURegisters.SP = inc(CPURegisters.SP);
                    break;
                case 0x34: // inc (hl)
                    {
                        Memory[CPURegisters.HL] = inc(Memory[CPURegisters.HL]);
                        break;
                    }
                case 0x35: // dec (hl)
                    {
                        Memory[CPURegisters.HL] = dec(Memory[CPURegisters.HL]);
                        break;
                    }
                case 0x36: // ld (hl), x
                    Memory[CPURegisters.HL] = readNextByte();
                    break;
                case 0x37: // scf; set carry flag, clear h and n
                    CPURegisters.SetC();
                    CPURegisters.ResetH();
                    CPURegisters.ResetN();
                    break;
                case 0x38: // jr c, x
                    {
                        var relative = (sbyte)readNextByte();
                        if (CPURegisters.ReadC())
                        {
                            CPURegisters.PC = (ushort)(CPURegisters.PC + relative);
                        }
                    }
                    break;
                case 0x39: // add hl, sp TODO: fix half carry
                    {
                        var (overflow, newValue) = uncheckedAdd(CPURegisters.HL, CPURegisters.SP);
                        CPURegisters.ResetN();
                        CPURegisters.SetOrResetH((CPURegisters.HL & 0xFFF) + (CPURegisters.SP & 0xFFF) > 0xFFF);
                        CPURegisters.SetOrResetC(overflow);
                        CPURegisters.HL = newValue;
                        break;
                    }
                case 0x3A: // ld a, (hl-)
                    CPURegisters.A = Memory[CPURegisters.HL--];
                    break;
                case 0x3B: // dec sp
                    CPURegisters.SP = dec(CPURegisters.SP);
                    break;
                case 0x3C: // inc a
                    CPURegisters.A = inc(CPURegisters.A);
                    break;
                case 0x3D: // dec a
                    CPURegisters.A = dec(CPURegisters.A);
                    break;
                case 0x3E: // ld a, x
                    CPURegisters.A = readNextByte();
                    break;
                case 0x3F: // ccf invert c flag, clear h and n flags
                    CPURegisters.SetOrResetC(!CPURegisters.ReadC());
                    CPURegisters.ResetH();
                    CPURegisters.ResetN();
                    break;
                case 0x40: // ld b, b; this is just a NOP
                    break;
                case 0x41: // ld b, c
                    CPURegisters.B = CPURegisters.C;
                    break;
                case 0x42: // ld b, d
                    CPURegisters.B = CPURegisters.D;
                    break;
                case 0x43: // ld b, e
                    CPURegisters.B = CPURegisters.E;
                    break;
                case 0x44: // ld b, h
                    CPURegisters.B = CPURegisters.H;
                    break;
                case 0x45: // ld b, l
                    CPURegisters.B = CPURegisters.L;
                    break;
                case 0x46: // ld b, (hl)
                    CPURegisters.B = Memory[CPURegisters.HL];
                    break;
                case 0x47: // ld b, a
                    CPURegisters.B = CPURegisters.A;
                    break;
                case 0x48: // ld c, b
                    CPURegisters.C = CPURegisters.B;
                    break;
                case 0x49: // ld c, c
                    break;
                case 0x4A: // ld c, d
                    CPURegisters.C = CPURegisters.D;
                    break;
                case 0x4B: // ld c, e
                    CPURegisters.C = CPURegisters.E;
                    break;
                case 0x4C: // ld c, h
                    CPURegisters.C = CPURegisters.H;
                    break;
                case 0x4D: // ld c, l
                    CPURegisters.C = CPURegisters.L;
                    break;
                case 0x4E: // ld c, (hl)
                    CPURegisters.C = Memory[CPURegisters.HL];
                    break;
                case 0x4F: // ld c, a
                    CPURegisters.C = CPURegisters.A;
                    break;
                case 0x50: // ld d, b
                    CPURegisters.D = CPURegisters.B;
                    break;
                case 0x51: // ld d, c
                    CPURegisters.D = CPURegisters.C;
                    break;
                case 0x52: // ld d, d
                    CPURegisters.D = CPURegisters.D;
                    break;
                case 0x53: // ld d, e
                    CPURegisters.D = CPURegisters.E;
                    break;
                case 0x54: // ld d, h
                    CPURegisters.D = CPURegisters.H;
                    break;
                case 0x55: // ld d, l
                    CPURegisters.D = CPURegisters.L;
                    break;
                case 0x56: // ld d, (hl)
                    CPURegisters.D = Memory[CPURegisters.HL];
                    break;
                case 0x57: // ld d, a
                    CPURegisters.D = CPURegisters.A;
                    break;
                case 0x58: // ld e, b
                    CPURegisters.E = CPURegisters.B;
                    break;
                case 0x59: // ld e, c
                    CPURegisters.E = CPURegisters.C;
                    break;
                case 0x5A: // ld e, d
                    CPURegisters.E = CPURegisters.D;
                    break;
                case 0x5B: // ld e, e
                    CPURegisters.E = CPURegisters.E;
                    break;
                case 0x5C: // ld e, h
                    CPURegisters.E = CPURegisters.H;
                    break;
                case 0x5D: // ld e, l
                    CPURegisters.E = CPURegisters.L;
                    break;
                case 0x5E: // ld e, (hl)
                    CPURegisters.E = Memory[CPURegisters.HL];
                    break;
                case 0x5F: // ld e, a
                    CPURegisters.E = CPURegisters.A;
                    break;
                case 0x60: // ld h, b
                    CPURegisters.H = CPURegisters.B;
                    break;
                case 0x61: // ld h, c
                    CPURegisters.H = CPURegisters.C;
                    break;
                case 0x62: // ld h, d
                    CPURegisters.H = CPURegisters.D;
                    break;
                case 0x63: // ld h, e
                    CPURegisters.H = CPURegisters.E;
                    break;
                case 0x64: // ld h, h
                    CPURegisters.H = CPURegisters.H;
                    break;
                case 0x65: // ld h, l
                    CPURegisters.H = CPURegisters.L;
                    break;
                case 0x66: // ld h, (hl)
                    CPURegisters.H = Memory[CPURegisters.HL];
                    break;
                case 0x67: // ld h, h
                    CPURegisters.H = CPURegisters.A;
                    break;
                case 0x68: // ld l, b
                    CPURegisters.L = CPURegisters.B;
                    break;
                case 0x69: // ld l, c
                    CPURegisters.L = CPURegisters.C;
                    break;
                case 0x6A: // ld l, d
                    CPURegisters.L = CPURegisters.D;
                    break;
                case 0x6B: // ld l, e
                    CPURegisters.L = CPURegisters.E;
                    break;
                case 0x6C: // ld l, h
                    CPURegisters.L = CPURegisters.H;
                    break;
                case 0x6D: // ld l, l
                    CPURegisters.L = CPURegisters.L;
                    break;
                case 0x6E: // ld l, (hl)
                    CPURegisters.L = Memory[CPURegisters.HL];
                    break;
                case 0x6F: // ld l, a
                    CPURegisters.L = CPURegisters.A;
                    break;
                case 0x70: // ld (hl), b
                    Memory[CPURegisters.HL] = CPURegisters.B;
                    break;
                case 0x71: // ld (hl), c
                    Memory[CPURegisters.HL] = CPURegisters.C;
                    break;
                case 0x72: // ld (hl), d
                    Memory[CPURegisters.HL] = CPURegisters.D;
                    break;
                case 0x73: // ld (hl), e
                    Memory[CPURegisters.HL] = CPURegisters.E;
                    break;
                case 0x74: // ld (hl), h
                    Memory[CPURegisters.HL] = CPURegisters.H;
                    break;
                case 0x75: // ld (hl), l
                    Memory[CPURegisters.HL] = CPURegisters.L;
                    break;
                case 0x76: // would be ld (hl), (hl); however, this is the HALT instruction. TODO: implement
                    break;
                case 0x77: // ld (hl), a
                    Memory[CPURegisters.HL] = CPURegisters.A;
                    break;
                case 0x78: // ld a, b
                    CPURegisters.A = CPURegisters.B;
                    break;
                case 0x79: // ld a, c
                    CPURegisters.A = CPURegisters.C;
                    break;
                case 0x7A: // ld a, d
                    CPURegisters.A = CPURegisters.D;
                    break;
                case 0x7B: // ld a, e
                    CPURegisters.A = CPURegisters.E;
                    break;
                case 0x7C: // ld a, h
                    CPURegisters.A = CPURegisters.H;
                    break;
                case 0x7D: // ld a, l
                    CPURegisters.A = CPURegisters.L;
                    break;
                case 0x7E: // ld a, (hl)
                    CPURegisters.A = Memory[CPURegisters.HL];
                    break;
                case 0x7F: // ld a, a
                    CPURegisters.A = CPURegisters.A;
                    break;
                case 0x80: // add a, b
                    add(CPURegisters.A, CPURegisters.B);
                    break;
                case 0x81: // add a, c
                    add(CPURegisters.A, CPURegisters.C);
                    break;
                case 0x82: // add a, d
                    add(CPURegisters.A, CPURegisters.D);
                    break;
                case 0x83: // add a, e
                    add(CPURegisters.A, CPURegisters.E);
                    break;
                case 0x84: // add a, h
                    add(CPURegisters.A, CPURegisters.H);
                    break;
                case 0x85: // add a, l
                    add(CPURegisters.A, CPURegisters.L);
                    break;
                case 0x86: // add a, (hl)
                    add(CPURegisters.A, Memory[CPURegisters.HL]);
                    break;
                case 0x87: // add a, a
                    add(CPURegisters.A, CPURegisters.A);
                    break;
                case 0x88: // adc a, b; add with carry, if carry flag is set, add 1.
                    adc(CPURegisters.A, CPURegisters.B, CPURegisters.ReadC());
                    break;
                case 0x89: // adc a, c; add with carry, if carry flag is set, add 1.
                    adc(CPURegisters.A, CPURegisters.C, CPURegisters.ReadC());
                    break;
                case 0x8A: // adc a, d; add with carry, if carry flag is set, add 1.
                    adc(CPURegisters.A, CPURegisters.D, CPURegisters.ReadC());
                    break;
                case 0x8B: // adc a, e; add with carry, if carry flag is set, add 1.
                    adc(CPURegisters.A, CPURegisters.E, CPURegisters.ReadC());
                    break;
                case 0x8C: // adc a, h; add with carry, if carry flag is set, add 1.
                    adc(CPURegisters.A, CPURegisters.H, CPURegisters.ReadC());
                    break;
                case 0x8D: // adc a, l; add with carry, if carry flag is set, add 1.
                    adc(CPURegisters.A, CPURegisters.L, CPURegisters.ReadC());
                    break;
                case 0x8E: // adc a, (hl); add with carry, if carry flag is set, add 1.
                    adc(CPURegisters.A, Memory[CPURegisters.HL], CPURegisters.ReadC());
                    break;
                case 0x8F: // adc a, a; add with carry, if carry flag is set, add 1.
                    adc(CPURegisters.A, CPURegisters.A, CPURegisters.ReadC());
                    break;
                case 0x90: // sub a, b
                    sub(CPURegisters.A, CPURegisters.B);
                    break;
                case 0x91: // sub a, c
                    sub(CPURegisters.A, CPURegisters.C);
                    break;
                case 0x92: // sub a, d
                    sub(CPURegisters.A, CPURegisters.D);
                    break;
                case 0x93: // sub a, e
                    sub(CPURegisters.A, CPURegisters.E);
                    break;
                case 0x94: // sub a, h
                    sub(CPURegisters.A, CPURegisters.H);
                    break;
                case 0x95: // sub a, l
                    sub(CPURegisters.A, CPURegisters.L);
                    break;
                case 0x96: // sub a, (hl)
                    sub(CPURegisters.A, Memory[CPURegisters.HL]);
                    break;
                case 0x97: // sub a, a
                    sub(CPURegisters.A, CPURegisters.A);
                    break;
                case 0x98: // sbc a, b
                    sbc(CPURegisters.A, CPURegisters.B, CPURegisters.ReadC());
                    break;
                case 0x99: // sbc a, c
                    sbc(CPURegisters.A, CPURegisters.C, CPURegisters.ReadC());
                    break;
                case 0x9A: // sbc a, d
                    sbc(CPURegisters.A, CPURegisters.D, CPURegisters.ReadC());
                    break;
                case 0x9B: // sbc a, e
                    sbc(CPURegisters.A, CPURegisters.E, CPURegisters.ReadC());
                    break;
                case 0x9C: // sbc a, h
                    sbc(CPURegisters.A, CPURegisters.H, CPURegisters.ReadC());
                    break;
                case 0x9D: // sbc a, l
                    sbc(CPURegisters.A, CPURegisters.L, CPURegisters.ReadC());
                    break;
                case 0x9E: // sbc a, (hl)
                    sbc(CPURegisters.A, Memory[CPURegisters.HL], CPURegisters.ReadC());
                    break;
                case 0x9F: // sbc a, a
                    sbc(CPURegisters.A, CPURegisters.A, CPURegisters.ReadC());
                    break;
                case 0xA0: // and a, b
                    and(CPURegisters.A, CPURegisters.B);
                    break;
                case 0xA1: // and a, c
                    and(CPURegisters.A, CPURegisters.C);
                    break;
                case 0xA2: // and a, d
                    and(CPURegisters.A, CPURegisters.D);
                    break;
                case 0xA3: // and a, e
                    and(CPURegisters.A, CPURegisters.E);
                    break;
                case 0xA4: // and a, h
                    and(CPURegisters.A, CPURegisters.H);
                    break;
                case 0xA5: // and a, l
                    and(CPURegisters.A, CPURegisters.L);
                    break;
                case 0xA6: // and a, (hl)
                    and(CPURegisters.A, Memory[CPURegisters.HL]);
                    break;
                case 0xA7: // and a, a
                    and(CPURegisters.A, CPURegisters.A);
                    break;
                case 0xA8: // xor a, b
                    xor(CPURegisters.A, CPURegisters.B);
                    break;
                case 0xA9: // xor a, c
                    xor(CPURegisters.A, CPURegisters.C);
                    break;
                case 0xAA: // xor a, d
                    xor(CPURegisters.A, CPURegisters.D);
                    break;
                case 0xAB: // xor a, e
                    xor(CPURegisters.A, CPURegisters.E);
                    break;
                case 0xAC: // xor a, h
                    xor(CPURegisters.A, CPURegisters.H);
                    break;
                case 0xAD: // xor a, l
                    xor(CPURegisters.A, CPURegisters.L);
                    break;
                case 0xAE: // xor a, (hl)
                    xor(CPURegisters.A, Memory[CPURegisters.HL]);
                    break;
                case 0xAF: // xor a, a
                    xor(CPURegisters.A, CPURegisters.A);
                    break;
                case 0xB0: // or a, b
                    or(CPURegisters.A, CPURegisters.B);
                    break;
                case 0xB1: // or a, c
                    or(CPURegisters.A, CPURegisters.C);
                    break;
                case 0xB2: // or a, d
                    or(CPURegisters.A, CPURegisters.D);
                    break;
                case 0xB3: // or a, e
                    or(CPURegisters.A, CPURegisters.E);
                    break;
                case 0xB4: // or a, h
                    or(CPURegisters.A, CPURegisters.H);
                    break;
                case 0xB5: // or a, l
                    or(CPURegisters.A, CPURegisters.L);
                    break;
                case 0xB6: // or a, (hl)
                    or(CPURegisters.A, Memory[CPURegisters.HL]);
                    break;
                case 0xB7: // or a, a
                    or(CPURegisters.A, CPURegisters.A);
                    break;
                case 0xB8: // cp a, b
                    compare(CPURegisters.A, CPURegisters.B);
                    break;
                case 0xB9: // cp a, c
                    compare(CPURegisters.A, CPURegisters.C);
                    break;
                case 0xBA: // cp a, d
                    compare(CPURegisters.A, CPURegisters.D);
                    break;
                case 0xBB: // cp a, e
                    compare(CPURegisters.A, CPURegisters.E);
                    break;
                case 0xBC: // cp a, h
                    compare(CPURegisters.A, CPURegisters.H);
                    break;
                case 0xBD: // cp a, l
                    compare(CPURegisters.A, CPURegisters.L);
                    break;
                case 0xBE: // cp a, (hl)
                    compare(CPURegisters.A, Memory[CPURegisters.HL]);
                    break;
                case 0xBF: // cp a, a
                    compare(CPURegisters.A, CPURegisters.A);
                    break;
                case 0xC0: // ret nz
                    if (!CPURegisters.ReadZ())
                    {
                        pop_sp();
                        CycleDelay += 1;
                    }
                    CycleDelay += 1;
                    break;
                case 0xC1: // pop bc
                    pop_bc();
                    CycleDelay += 1;
                    break;
                case 0xc2: // jp nz, xx
                    {
                        var address = read_ushort();
                        if (!CPURegisters.ReadZ())
                        {
                            CPURegisters.PC = address;
                            CycleDelay += 1;
                        }
                    }
                    break;
                case 0xC3: // jp xx
                    {
                        var address = read_ushort();
                        CPURegisters.PC = address;
                        break;
                    }
                case 0xC4: // call nz, xx
                    {
                        var address = read_ushort();
                        if (!CPURegisters.ReadZ())
                        {
                            push_sp();
                            CPURegisters.PC = address;
                            CycleDelay += 1;
                        }
                        break;
                    }
                case 0xC5: // push bc
                    push_bc();
                    break;
                case 0xC6: // add a, x
                    add(CPURegisters.A, readNextByte());
                    break;
                case 0xC7: // rst 00h
                    rst(0x00);
                    break;
                case 0xC8: // ret z
                    {
                        if (CPURegisters.ReadZ())
                        {
                            pop_sp();
                            CycleDelay += 1;
                        }
                        CycleDelay += 1;
                        break;
                    }
                case 0xC9: // ret
                    pop_sp();
                    CycleDelay += 1;
                    break;
                case 0xCA: // jp z, xx
                    {
                        var address = read_ushort();
                        if (CPURegisters.ReadZ())
                        {
                            CPURegisters.PC = address;
                            CycleDelay += 1;
                        }
                        break;
                    }
                case 0xCB: // oh shit
                    CBActions[readNextByte()]();
                    break; // lgtm, nothing to see here :^)
                case 0xCC: // call z, xx
                    {
                        var address = read_ushort();
                        if (CPURegisters.ReadZ())
                        {
                            push_sp();
                            CPURegisters.PC = address;
                            CycleDelay += 1;
                        }
                        break;
                    }
                case 0xCD: // call xx
                    {
                        var address = read_ushort();
                        push_sp();
                        CPURegisters.PC = address;
                        CycleDelay += 1;
                        break;
                    }
                case 0xCE: // adc a, x
                    adc(CPURegisters.A, readNextByte(), CPURegisters.ReadC());
                    break;
                case 0xCF: // rst 08h
                    rst(0x08);
                    break;
                case 0xD0: // ret nc
                    if (!CPURegisters.ReadC())
                    {
                        pop_sp();
                        CycleDelay += 1;
                    }
                    CycleDelay += 1;
                    break;
                case 0xD1: // pop de
                    pop_de();
                    CycleDelay += 1;
                    break;
                case 0xD2: // jp nc, xx
                    {
                        var address = read_ushort();
                        if (!CPURegisters.ReadC())
                        {
                            CPURegisters.PC = address;
                            CycleDelay += 1;
                        }
                    }
                    break;
                case 0xD3:
                    throw new Exception("Invalid opcode called");
                case 0xD4: // call nc, xx
                    {
                        var address = read_ushort();
                        if (!CPURegisters.ReadC())
                        {
                            push_sp();
                            CPURegisters.PC = address;
                            CycleDelay += 1;
                        }
                        break;
                    }
                case 0xD5: // push de
                    push_de();
                    CycleDelay += 1;
                    break;
                case 0xD6: // sub a, x
                    sub(CPURegisters.A, readNextByte());
                    break;
                case 0xD7: // rst 10h
                    rst(0x10);
                    break;
                case 0xD8: // ret c
                    {
                        if (CPURegisters.ReadC())
                        {
                            pop_sp();
                            CycleDelay += 1;
                        }
                        CycleDelay += 1;
                        break;
                    }
                case 0xD9: // reti ???
                    pop_sp();
                    InterruptEnabled = true;
                    CycleDelay += 1;
                    break;
                case 0xDA: // jp z, xx
                    {
                        var address = read_ushort();
                        if (CPURegisters.ReadC())
                        {
                            CPURegisters.PC = address;
                            CycleDelay += 1;
                        }
                        break;
                    }
                case 0xDB: // oh shit
                    throw new Exception("Invalid opcode called");
                case 0xDC: // call c, xx
                    {
                        var address = read_ushort();
                        if (CPURegisters.ReadC())
                        {
                            push_sp();
                            CPURegisters.PC = address;
                            CycleDelay += 1;
                        }
                        break;
                    }
                case 0xDD: // call xx
                    throw new Exception("Invalid opcode executed");
                case 0xDE: // sbc a, x
                    sbc(CPURegisters.A, readNextByte(), CPURegisters.ReadC());
                    break;
                case 0xDF: // rst 18h
                    rst(0x18);
                    break;
                case 0xE0: // ld ffxx, a
                    Memory[0xFF00 + readNextByte()] = CPURegisters.A;
                    break;
                case 0xE1: // pop hl
                    pop_hl();
                    CycleDelay += 1;
                    break;
                case 0xE2: // ld FF+C, A
                    Memory[0xFF00 + CPURegisters.C] = CPURegisters.A;
                    break;
                case 0xE3:
                    throw new Exception("Invalid opcode called");
                case 0xE4:
                    throw new Exception("Invalid opcode called");
                case 0xE5: // push hl
                    push_hl();
                    CycleDelay += 1;
                    break;
                case 0xE6: // and a, x
                    and(CPURegisters.A, readNextByte());
                    break;
                case 0xE7: // rst 20h
                    rst(0x20);
                    break;
                case 0xE8: // add sp, x (fix h, c flags)
                    {
                        var n = readNextByte();
                        CPURegisters.ResetZ();
                        CPURegisters.ResetN();

                        if ((byte)(CPURegisters.SP & 0x0F) + (n & 0x0F) > 0x0F)
                        {
                            CPURegisters.SetH();
                        }
                        else 
                        {
                            CPURegisters.ResetH();
                        }
                        if (((ushort)(CPURegisters.SP & 0xFF) + n) > 0xFF)
                        {
                            CPURegisters.SetC();
                        }
                        else
                        {
                            CPURegisters.ResetC();
                        }

                        var sadd = (sbyte)n;
                        if (sadd < 0)
                        {
                            CPURegisters.SP -= (ushort)-sadd;
                        }
                        else
                        {
                            CPURegisters.SP += (ushort)sadd;
                        }
                        break;
                    }
                //
                //     var add = (sbyte)readNextByte();
                //     var seth = false;
                //     var setc = false;
                //     if (add < 0) //subtract
                //     {

                //         if (isHalfCarrySub((byte)(CPURegisters.SP & 0xFF), (byte) -add, false))
                //         {
                //             seth = true;
                //         }
                //         if (CPURegisters.SP + add < 0)
                //         {
                //             setc = false;
                //             seth = false;
                //         }
                //         if (CPURegisters.SP + add == 0)
                //         {
                //             setc = true;
                //             seth = true;
                //         }
                //         if (CPURegisters.SP >= 0xF && CPURegisters.SP + add < 0xF)
                //         {
                //             setc = true;
                //             seth = true;
                //         }
                //     }
                //     else // add
                //     {
                //         if (isHalfCarryAdd((byte)(CPURegisters.SP & 0xFF), (byte)add, false))
                //         {
                //             seth = true;
                //         }
                //         if ((CPURegisters.SP & 0xFF) + (byte)add > 0xFF)
                //         {
                //             seth = true;
                //             setc = true;
                //         }

                //     }
                //     CPURegisters.SetOrResetC(setc);
                //     CPURegisters.SetOrResetH(seth);
                //     CPURegisters.SP = (ushort)(CPURegisters.SP + add);
                //     CPURegisters.ResetZ();
                //     CPURegisters.ResetN();

                //     break;
                // }
                case 0xE9: // jp hl
                    CPURegisters.PC = CPURegisters.HL;
                    break;
                case 0xEA: // ld (xx), a
                    {
                        var address = read_ushort();
                        Memory[address] = CPURegisters.A;
                        break;
                    }
                case 0xEB:
                case 0xEC:
                case 0xED:
                    throw new Exception("Invalid opcode called");
                case 0xEE: // xor a, x
                    xor(CPURegisters.A, readNextByte());
                    break;
                case 0xEF: // rst 28h
                    rst(0x28);
                    break;
                case 0xF0: // ld a, (ffxx)
                    {
                        var address = 0xFF00 + readNextByte();
                        CPURegisters.A = Memory[address];
                        break;
                    }
                case 0xF1: // pop af
                    pop_af();
                    CycleDelay += 1;
                    break;
                case 0xF2: // ld a, ff+c
                    CPURegisters.A = Memory[0xFF00 + CPURegisters.C];
                    break;
                case 0xF3: // di
                    InterruptEnabled = false;
                    break;
                case 0xF4:
                    throw new Exception("Invalid opcode called");
                case 0xF5: // push af
                    push_af();
                    CycleDelay += 1;
                    break;
                case 0xF6: // or a, x
                    or(CPURegisters.A, readNextByte());
                    break;
                case 0xF7: // rst 30h
                    rst(0x30);
                    break;
                case 0xF8: // ld hl, sp + x; handle over/underflow correctly!
                    {
                        var n = readNextByte();
                        CPURegisters.ResetZ();
                        CPURegisters.ResetN();

                        if ((byte)(CPURegisters.SP & 0x0F) + (n & 0x0F) > 0x0F)
                        {
                            CPURegisters.SetH();
                        }
                        else
                        {
                            CPURegisters.ResetH();
                        }
                        if (((ushort)(CPURegisters.SP & 0xFF) + n) > 0xFF)
                        {
                            CPURegisters.SetC();
                        }
                        else
                        {
                            CPURegisters.ResetC();
                        }

                        var sadd = (sbyte)n;
                        if (sadd < 0)
                        {
                            CPURegisters.HL = (ushort)(CPURegisters.SP - ((ushort)-sadd));
                        }
                        else
                        {
                            CPURegisters.HL = (ushort)(CPURegisters.SP + ((ushort)sadd));
                        }
                        break;
                    }
                case 0xF9: // ld sp, hl
                    CPURegisters.SP = CPURegisters.HL;
                    break;
                case 0xFA: // ld a, (xx)
                    {
                        var address = read_ushort();
                        CPURegisters.A = Memory[address];
                        break;
                    }
                case 0xFB:
                    InterruptEnabled = true;
                    break;
                case 0xFC:
                case 0xFD:
                    throw new Exception("Invalid opcode called");
                case 0xFE: // cp a, x
                    compare(CPURegisters.A, readNextByte());
                    break;
                case 0xFF: // rst 38h
                    rst(0x38);
                    break;

            }
        }
        private ushort read_ushort()
        {
            return (ushort)(readNextByte() | readNextByte() << 8);
        }
        private void push_af()
        {
            Memory[--CPURegisters.SP] = (byte)(CPURegisters.A);
            Memory[--CPURegisters.SP] = (byte)(CPURegisters.F);
        }

        private void pop_af()
        {
            CPURegisters.F = Memory[CPURegisters.SP++];
            CPURegisters.A = Memory[CPURegisters.SP++];
        }

        private void push_hl()
        {
            Memory[--CPURegisters.SP] = (byte)(CPURegisters.H);
            Memory[--CPURegisters.SP] = (byte)(CPURegisters.L);
        }

        private void pop_hl()
        {
            CPURegisters.L = Memory[CPURegisters.SP++];
            CPURegisters.H = Memory[CPURegisters.SP++];
        }

        private void push_de()
        {
            Memory[--CPURegisters.SP] = (byte)(CPURegisters.D);
            Memory[--CPURegisters.SP] = (byte)(CPURegisters.E);
        }

        private void pop_de()
        {
            CPURegisters.E = Memory[CPURegisters.SP++];
            CPURegisters.D = Memory[CPURegisters.SP++];
        }

        private void pop_sp()
        {
            ushort address = Memory[CPURegisters.SP++];
            address |= (ushort)(Memory[CPURegisters.SP++] << 8);
            CPURegisters.PC = address;
        }
        private void pop_bc()
        {
            CPURegisters.C = Memory[CPURegisters.SP++];
            CPURegisters.B = Memory[CPURegisters.SP++];
        }
        private void push_sp()
        {
            var address = CPURegisters.PC;
            Memory[--CPURegisters.SP] = (byte)(CPURegisters.PC >> 8);
            Memory[--CPURegisters.SP] = (byte)(CPURegisters.PC);
        }
        private void push_bc()
        {
            Memory[--CPURegisters.SP] = CPURegisters.B;
            Memory[--CPURegisters.SP] = CPURegisters.C;
        }

        private void rst(ushort n)
        {
            Memory[--CPURegisters.SP] = (byte)(CPURegisters.PC >> 8);
            Memory[--CPURegisters.SP] = (byte)(CPURegisters.PC);
            CPURegisters.PC = (ushort)(Memory[n]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private byte readNextByte()
        {
            return Memory[CPURegisters.PC++];
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static (bool, byte) uncheckedAdd(byte a, byte b, bool carry)
        {
            var overflow = (a + b) > byte.MaxValue;
            var output = (byte)(a + b);
            if (carry)
            {
                overflow |= output + 1 > byte.MaxValue;
                unchecked { output += 1; }
            }
            return (overflow, output);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static (bool, ushort) uncheckedAdd(ushort a, ushort b)
        {
            var overflow = (a + b) > ushort.MaxValue;
            var output = (ushort)(a + b); // needs to be tested
            return (overflow, output);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static (bool, byte) uncheckedSub(byte a, byte b, bool carry)
        {
            var overflow = ((int)a - (int)b) < 0;
            var output = (byte)(a - b);
            if (carry)
            {
                overflow |= output - 1 < 0;
                output -= 1;
            }
            return (overflow, output);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static (bool, ushort) uncheckedSub(ushort a, ushort b)
        {
            bool overflow = a - b < 0;
            ushort output = overflow ? (ushort)((a - b) >> 16) : (ushort)(a - b); // needs to be tested
            return (overflow, output);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        private static bool isHalfCarryAdd(byte a, byte b, bool carry)
        {
            var value = (a & 0xf) + (b & 0xf);
            return carry ? value + 1 > 0xf : value > 0xf;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private byte inc(byte n) //
        {
            var (overflow, newValue) = uncheckedAdd(n, 1, false);
            //CPURegisters.SetOrResetC(overflow);
            CPURegisters.SetOrResetH((n & 0x0F) == 0x0F);
            CPURegisters.SetOrResetZ(newValue);
            CPURegisters.ResetN();
            return newValue;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ushort inc(ushort n)
        {
            var (_, newValue) = uncheckedAdd(n, 1); // none of these set the overflow flag
            return newValue;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private byte dec(byte n)
        {
            var (overflow, newValue) = uncheckedSub(n, 1, false);
            CPURegisters.SetOrResetH((n & 0x0F) == 0x00);
            CPURegisters.SetN();
            CPURegisters.SetOrResetZ(newValue);
            return newValue;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ushort dec(ushort n)
        {
            var (overflow, newValue) = uncheckedSub(n, 1);
            CycleDelay += 1;
            return newValue;
        }

        // Adds two values together and stores the value in the accumulator register (A)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void add(byte a, byte b)
        {
            adc(a, b, false);
        }
        // Adds two values together and stores the value in the accumulator register (A). If carry flag was set, increments the result by 1.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        private void adc(byte a, byte b, bool carry)
        {
            var (overflow, newValue) = uncheckedAdd(a, b, carry);
            CPURegisters.A = newValue;
            CPURegisters.SetOrResetC(overflow);
            CPURegisters.ResetN();
            CPURegisters.SetOrResetZ(newValue);
            CPURegisters.SetOrResetH(isHalfCarryAdd(a, b, carry));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        private void sub(byte a, byte b)
        {
            sbc(a, b, false);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        private void sbc(byte a, byte b, bool carry)
        {
            var (overflow, newValue) = uncheckedSub(a, b, carry);
            CPURegisters.A = newValue;
            CPURegisters.SetOrResetC(overflow);
            CPURegisters.SetN();
            CPURegisters.SetOrResetH(isHalfCarrySub(a, b, carry));
            CPURegisters.SetOrResetZ(newValue);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        private static bool isHalfCarrySub(byte a, byte b, bool carry)
        {
            var h = (a & 0x0f) < (b & 0x0f);
            unchecked
            {
                return carry ? h || ((a - b) & 0x0f) == 0 : h;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        private void and(byte a, byte b)
        {
            var output = (byte)(a & b);
            CPURegisters.SetOrResetZ(output);
            CPURegisters.ResetN();
            CPURegisters.SetH();
            CPURegisters.ResetC();
            CPURegisters.A = output;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void xor(byte a, byte b)
        {
            var output = (byte)(a ^ b);
            CPURegisters.SetOrResetZ(output);
            CPURegisters.ResetN();
            CPURegisters.ResetH();
            CPURegisters.ResetC();
            CPURegisters.A = output;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void or(byte a, byte b)
        {
            var output = (byte)(a | b);
            CPURegisters.SetOrResetZ(output);
            CPURegisters.ResetN();
            CPURegisters.ResetH();
            CPURegisters.ResetC();
            CPURegisters.A = output;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void compare(byte a, byte b)
        {
            var (overflow, newValue) = uncheckedSub(a, b, false);
            CPURegisters.SetOrResetC(overflow);
            CPURegisters.SetN();
            CPURegisters.SetOrResetH(isHalfCarrySub(a, b, false));
            CPURegisters.SetOrResetZ(newValue);
        }

        private Action[] populateCBTable()
        {
            var actions = new Action[0x100];
            var functions = new[] {
                    new {getter = (Func<byte>)(() => CPURegisters.B), setter = (Action<byte>)((x) => CPURegisters.B = x)},
                    new {getter = (Func<byte>)(() => CPURegisters.C), setter = (Action<byte>)((x) => CPURegisters.C = x)},
                    new {getter = (Func<byte>)(() => CPURegisters.D), setter = (Action<byte>)((x) => CPURegisters.D = x)},
                    new {getter = (Func<byte>)(() => CPURegisters.E), setter = (Action<byte>)((x) => CPURegisters.E = x)},
                    new {getter = (Func<byte>)(() => CPURegisters.H), setter = (Action<byte>)((x) => CPURegisters.H = x)},
                    new {getter = (Func<byte>)(() => CPURegisters.L), setter = (Action<byte>)((x) => CPURegisters.L = x)},
                    new {getter = (Func<byte>)(() => Memory[CPURegisters.HL]), setter = (Action<byte>)((x) => Memory[CPURegisters.HL] = x)},
                    new {getter = (Func<byte>)(() => CPURegisters.A), setter = (Action<byte>)((x) => CPURegisters.A = x)}
            };
            for (int i = 0; i <= 0x7; i++) // rlc
            {
                var register = functions[i % 8];
                actions[i] = () =>
                {
                    var value = register.getter();
                    var carry = (value & 0b1000_0000) > 0;
                    value <<= 1;
                    if (carry)
                    {
                        CPURegisters.SetC();
                        value |= 0x1;
                    }
                    else
                    {
                        CPURegisters.ResetC();
                    }
                    CPURegisters.SetOrResetZ(value);
                    CPURegisters.ResetN();
                    CPURegisters.ResetH();
                    register.setter(value);
                };
            }
            for (int i = 0x8; i <= 0xF; i++) // rrc
            {
                var register = functions[i % 8];
                actions[i] = () =>
                {
                    var value = register.getter();
                    var carry = (value & 0b0000_0001) == 1;
                    value >>= 1;
                    if (carry)
                    {
                        CPURegisters.SetC();
                        value |= 0b1000_0000;
                    }
                    else
                    {
                        CPURegisters.ResetC();
                    }
                    CPURegisters.SetOrResetZ(value);
                    CPURegisters.ResetN();
                    CPURegisters.ResetH();
                    register.setter(value);
                };
            }
            for (int i = 0x10; i <= 0x17; i++) // rl
            {
                var register = functions[i % 8];
                actions[i] = () =>
                {
                    var value = register.getter();
                    var carry = (value & 0b1000_0000) > 0;
                    value <<= 1;
                    value |= (byte)(CPURegisters.ReadC() ? 0b0000_0001 : 0x0);
                    if (carry)
                    {
                        CPURegisters.SetC();
                    }
                    else
                    {
                        CPURegisters.ResetC();
                    }
                    CPURegisters.SetOrResetZ(value);
                    CPURegisters.ResetN();
                    CPURegisters.ResetH();
                    register.setter(value);
                };
            }
            for (int i = 0x18; i <= 0x1F; i++) // rr
            {
                var register = functions[i % 8];
                actions[i] = () =>
                {
                    var value = register.getter();
                    var carry = (value & 0b0000_0001) > 0;
                    value >>= 1;
                    value |= (byte)(CPURegisters.ReadC() ? 0b1000_0000 : 0b0000_0000);
                    if (carry)
                    {
                        CPURegisters.SetC();
                    }
                    else
                    {
                        CPURegisters.ResetC();
                    }
                    CPURegisters.SetOrResetZ(value);
                    CPURegisters.ResetN();
                    CPURegisters.ResetH();
                    register.setter(value);
                };
            }
            for (int i = 0x20; i <= 0x27; i++) // sla
            {
                var register = functions[i % 8];
                actions[i] = () =>
                {
                    var value = register.getter();
                    var carry = (value & 0b1000_0000) > 0;
                    value <<= 1;
                    if (carry)
                    {
                        CPURegisters.SetC();
                    }
                    else
                    {
                        CPURegisters.ResetC();
                    }
                    CPURegisters.SetOrResetZ(value);
                    CPURegisters.ResetN();
                    CPURegisters.ResetH();
                    register.setter(value);
                };
            }
            for (int i = 0x28; i <= 0x2F; i++) // sra
            {
                var register = functions[i % 8];
                actions[i] = () =>
                {
                    var value = register.getter();
                    var carry = (value & 0b0000_0001) == 1;
                    var msb = value & 0b1000_0000;

                    value >>= 1;
                    if (msb > 0) value |= 0b1000_0000;

                    if (carry)
                    {
                        CPURegisters.SetC();
                    }
                    else
                    {
                        CPURegisters.ResetC();
                    }
                    CPURegisters.SetOrResetZ(value);
                    CPURegisters.ResetN();
                    CPURegisters.ResetH();
                    register.setter(value);
                };
            }
            for (int i = 0x30; i <= 0x37; i++) // swap
            {
                var register = functions[i % 8];
                actions[i] = () =>
                {
                    var value = register.getter();
                    value = (byte)((value & 0x0F) << 4 | (value & 0xF0) >> 4);
                    CPURegisters.SetOrResetZ(value);
                    CPURegisters.ResetC();
                    CPURegisters.ResetN();
                    CPURegisters.ResetH();
                    register.setter(value);
                };
            }
            for (int i = 0x38; i <= 0x3F; i++) // srl
            {
                var register = functions[i % 8];
                actions[i] = () =>
                {
                    var value = register.getter();
                    var carry = (value & 0b0000_0001) == 1;
                    value >>= 1;
                    if (carry)
                    {
                        CPURegisters.SetC();
                    }
                    else
                    {
                        CPURegisters.ResetC();
                    }
                    CPURegisters.SetOrResetZ(value);
                    CPURegisters.ResetN();
                    CPURegisters.ResetH();
                    register.setter(value);
                };
            }
            for (int i = 0x40; i <= 0x7F; i++) // iterate over all possible opcodes for bit
            {
                var register = functions[i % 8];
                var bit = (i - 0x40) / 8;
                var mask = 1 << bit;
                actions[i] = () =>
                {
                    var value = register.getter();
                    if ((value & mask) == 0)
                    {
                        CPURegisters.SetZ();
                    }
                    else
                    {
                        CPURegisters.ResetZ();
                    }
                    CPURegisters.SetH();
                    CPURegisters.ResetN();
                };
            }
            for (int i = 0x80; i <= 0xBF; i++) // iterate over all possible opcodes for reset
            {
                var register = functions[i % 8];
                var bit = (i - 0x80) / 8;
                var mask = ~(1 << bit);
                actions[i] = () => register.setter((byte)(register.getter() & mask));
            }
            for (int i = 0xC0; i <= 0xFF; i++) // iterate over all possible opcodes for set
            {
                var register = functions[i % 8];
                var bit = (i - 0xC0) / 8;
                var mask = 1 << bit;
                actions[i] = () => register.setter((byte)(register.getter() | mask));
            }
            return actions;
        }
    }
}