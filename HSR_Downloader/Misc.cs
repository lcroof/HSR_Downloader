using System;
using System.Text.Json.Serialization;

namespace HSR_DataDownloader;

public class HotfixJson
{
    public string assetBundleUrl = string.Empty;
    public string exResourceUrl = string.Empty;
    public string luaUrl = string.Empty;
    public string ifixUrl = string.Empty;
}

public class M_ArchiveV
{
    public int MajorVersion;
    public int MinorVersion;
    public int PatchVersion;
    public int PrevPatch;
    public string ContentHash = string.Empty;
    public uint FileSize;
    public long TimeStamp;
    public string FileName = string.Empty;
    public string BaseAssetsDownloadUrl = string.Empty;
}

public class BlockV
{
    public int length;
    public List<AsbBlock> asbBlocks = new();

    // Add here a function to read the data from byte[] using EndianBinaryReader
    public void ReadData(byte[] data)
    {
        using (var reader = new EndianBinaryReader(new MemoryStream(data)))
        {
            reader.ReadBytes(20);
            length = reader.ReadInt32();
            reader.ReadInt32();
            for (int i = 0; i < length; i++)
            {
                var block = new AsbBlock();
                block.assetName = reader.ReadHash();
                block.temp = reader.ReadBytes(4);
                block.assetID = BitConverter.ToInt32(block.temp, 0);
                block.size = reader.ReadInt32();
                block.isStart = (block.temp[2] >> 4) > 0;
                asbBlocks.Add(block);
            }
        }
    }
}

public class AsbBlock
{
    public string assetName = string.Empty;
    public int assetID;
    public int size;
    public bool isStart;
    public byte[] temp = new byte[4];
}

public class M_DesignV
{

}