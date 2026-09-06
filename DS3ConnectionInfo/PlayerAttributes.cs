using System;

namespace DS3ConnectionInfo
{
    /// <summary>A single read of the locally synchronized character attributes.</summary>
    public sealed class PlayerAttributes
    {
        public int Level { get; private set; }
        public int Vigor { get; private set; }
        public int Attunement { get; private set; }
        public int Endurance { get; private set; }
        public int Strength { get; private set; }
        public int Dexterity { get; private set; }
        public int Intelligence { get; private set; }
        public int Faith { get; private set; }
        public int? CurrentHp { get; private set; }
        public int? MaximumHp { get; private set; }
        public string Health => CurrentHp.HasValue && MaximumHp.HasValue
            ? $"{CurrentHp.Value} / {MaximumHp.Value}" : "—";

        public static PlayerAttributes Read(long character, Func<long, int, byte[]> read)
        {
            if (character == 0) throw new UnauthorizedAccessException("Character unavailable");
            long data = Pointer(character + 0x1FA0, read);
            byte[] stats = Bytes(data + 0x44, 0x30, read);
            var result = new PlayerAttributes
            {
                Vigor = BitConverter.ToInt32(stats, 0x00),
                Attunement = BitConverter.ToInt32(stats, 0x04),
                Endurance = BitConverter.ToInt32(stats, 0x08),
                Strength = BitConverter.ToInt32(stats, 0x0C),
                Dexterity = BitConverter.ToInt32(stats, 0x10),
                Intelligence = BitConverter.ToInt32(stats, 0x14),
                Faith = BitConverter.ToInt32(stats, 0x18),
                Level = BitConverter.ToInt32(stats, 0x2C)
            };
            // Live ChrDataModule HP/max HP uses the same context for host and phantom.
            // SessionInfo +1C and +20 contain different HP limits; do not choose by team.
            try
            {
                long modules = Pointer(character + 0x1F90, read);
                long vital = Pointer(modules + 0x18, read);
                byte[] hp = Bytes(vital + 0xD8, 8, read);
                int current = BitConverter.ToInt32(hp, 0);
                int maximum = BitConverter.ToInt32(hp, 4);
                if (current >= 0 && maximum > 0 && current <= maximum)
                {
                    result.CurrentHp = current;
                    result.MaximumHp = maximum;
                }
            }
            catch (UnauthorizedAccessException) { } // Attribute data can outlive live HP data.
            return result;
        }

        private static byte[] Bytes(long address, int count, Func<long, int, byte[]> read)
        {
            byte[] bytes = read(address, count);
            if (bytes == null || bytes.Length != count)
                throw new UnauthorizedAccessException("Incomplete character data");
            return bytes;
        }

        private static long Pointer(long address, Func<long, int, byte[]> read)
        {
            long pointer = BitConverter.ToInt64(Bytes(address, 8, read), 0);
            if (pointer == 0) throw new UnauthorizedAccessException("Character data unavailable");
            return pointer;
        }
    }
}
