using HSR_DataDownloader;
using System.Text;

namespace HSR_DataDownloader;

public class DesignIndex
{
    public long Unk1;
    public int FileCount;
    public int Unk2;
    public List<FileEntry> Files = new();

    public class FileEntry
    {
        public int NameHash;
        public string? FileHash;
        public ulong Size => (ulong)Entries.Sum(x => x.Size);

        public ulong ReadSize;
        public uint Count => (uint)Entries.Count;
        public List<DataEntry> Entries = new();
        public byte Unk;

        public class DataEntry
        {
            public int NameHash;
            public uint Size;
            public uint Offset;

            public static DataEntry Read(EndianBinaryReader br)
            {
                var entry = new DataEntry();
                entry.NameHash = br.ReadInt32BE();
                entry.Size = br.ReadUInt32BE();
                entry.Offset = br.ReadUInt32BE();
                return entry;
            }
        }

        public static FileEntry Read(EndianBinaryReader br)
        {
            var entry = new FileEntry();

            entry.NameHash = br.ReadInt32BE();
            entry.FileHash = br.ReadStraightHash();
            entry.ReadSize = br.ReadUInt64BE();
            var cnt = br.ReadUInt32BE();

            // logger.LogInfo($"- {entry.NameHash} {entry.FileHash} {entry.ReadSize} {cnt}");

            for (var i = 0; i < cnt; i++)
                entry.Entries.Add(DataEntry.Read(br));

            var offset = 0u;
            foreach (var ientry in entry.Entries)
            {
                if (offset != ientry.Offset)
                    throw new Exception($"Offset mismatch");
                offset += ientry.Size;
            }
            entry.Unk = br.ReadByte();

            if (entry.ReadSize != entry.Size)
                throw new Exception($"Size mismatch in filehash {entry.FileHash}: read {entry.ReadSize}, calc {entry.Size} (diff {entry.ReadSize - entry.Size})");
            return entry;
        }
    }

    public static DesignIndex Read(byte[] indexBytes)
    {
        using var msi = new MemoryStream(indexBytes);
        using var bri = new EndianBinaryReader(msi, Encoding.UTF8);

        return Read(bri);
    }

    public static DesignIndex Read(EndianBinaryReader br)
    {
        var index = new DesignIndex();
        index.Unk1 = br.ReadInt64();
        index.FileCount = br.ReadInt32BE();
        index.Unk2 = br.ReadInt32();
        // logger.LogInfo($"{index.Unk1} {index.FileCount} {index.Unk2}");

        for (var i = 0; i < index.FileCount; i++)
            index.Files.Add(FileEntry.Read(br));

        return index;
    }

    public void RecalcSizeOffsets()
    {
        foreach (var file in Files)
        {
            var offset = 0u;
            foreach (var entry in file.Entries)
            {
                entry.Offset = offset;
                offset += entry.Size;
            }
        }
    }
}
