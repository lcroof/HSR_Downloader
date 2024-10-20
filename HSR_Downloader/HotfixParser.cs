using System;
using System.Text;
using Newtonsoft.Json;

namespace HSR_DataDownloader;

public class HotfixParser
{
    private readonly HttpClient _client;
    private readonly Logger _logger;
    private readonly HotfixJson _hotfixJson;
    private readonly string _platform;
    private BlockV _blockV;
    private LuaIndex _luaIndex;
    private DesignIndex _designIndex;

    public List<string> asbLinks = new();
    public List<string> luaLinks = new();
    public List<string> exResourceLinks = new();


    public HotfixParser(HttpClient client, Logger logger, HotfixJson hotfixJson, string platform, BlockV blockV, DesignIndex designIndex, LuaIndex luaIndex)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _hotfixJson = hotfixJson ?? throw new ArgumentNullException(nameof(hotfixJson));
        _platform = platform ?? throw new ArgumentNullException(nameof(platform));
        _blockV = blockV ?? throw new ArgumentNullException(nameof(blockV));
        _luaIndex = luaIndex ?? throw new ArgumentNullException(nameof(luaIndex));
        _designIndex = designIndex ?? throw new ArgumentNullException(nameof(designIndex));
    }

    public async Task ParseAsbDatasAsync()
    {
        if (string.IsNullOrEmpty(_hotfixJson.assetBundleUrl)) return;
        try
        {
            string? baseAssetDownloadURL = null;
            string url = $"{_hotfixJson.assetBundleUrl}/client/{_platform}/Archive/M_ArchiveV.bytes";
            string response = await _client.GetStringAsync(url).ConfigureAwait(false);
            asbLinks.Add(url);

            _logger.LogInfo("Successfully fetched M_ArchiveV data from the URL");

            foreach (string rsp in response.Split('\n'))
            {
                if (string.IsNullOrWhiteSpace(rsp))
                {
                    continue;
                }

                M_ArchiveV? item = JsonConvert.DeserializeObject<M_ArchiveV>(rsp);
                if (item == null || !item.FileName.StartsWith("M_BlockV"))
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(item.BaseAssetsDownloadUrl))
                {
                    baseAssetDownloadURL = item.BaseAssetsDownloadUrl;
                }

                string blockVurl = $"{_hotfixJson.assetBundleUrl}/client/{_platform}/Block/BlockV_{item.ContentHash}.bytes";
                byte[] blockVcontent = await _client.GetByteArrayAsync(blockVurl).ConfigureAwait(false);
                _blockV.ReadData(blockVcontent);
                asbLinks.Add(blockVurl);

                foreach (var block in _blockV.asbBlocks)
                {
                    if (block.isStart || string.IsNullOrEmpty(baseAssetDownloadURL))
                    {
                        asbLinks.Add($"{_hotfixJson.assetBundleUrl}/client/{_platform}/Block/{block.assetName}.block");
                    }
                    else
                    {
                        var link = string.Join("/", url.Split('/').SkipLast(1));
                        asbLinks.Add($"{_hotfixJson.assetBundleUrl}/{baseAssetDownloadURL}/client/{_platform}/Block/{block.assetName}.block");
                    }
                }
            }
        }
        catch (HttpRequestException e)
        {
            _logger.LogError($"Request error: {e.Message}");
        }
        catch (Exception e)
        {
            _logger.LogError($"Unexpected error: {e.Message}");
        }
    }


    public async Task ParseDesignDatasAsync()
    {
        if (string.IsNullOrEmpty(_hotfixJson.exResourceUrl)) return;
        try
        {
            string url = $"{_hotfixJson.exResourceUrl}/client/{_platform}/M_DesignV.bytes";
            byte[] designBytes = await _client.GetByteArrayAsync(url).ConfigureAwait(false);
            exResourceLinks.Add(url);

            _logger.LogInfo("Successfully fetched M_DesignV data from the URL");

            using var ms = new MemoryStream(designBytes);
            using var br = new EndianBinaryReader(ms, Encoding.UTF8);
            var magic = new string(br.ReadChars(4));

            br.ReadInt16();
            var MetadataInfoSize = br.ReadInt32();
            ms.Seek(0xE, SeekOrigin.Current);

            var RemoteRevisionID = br.ReadInt32();

            var IndexHash = br.ReadHash();

            var AssetListFilesize = br.ReadUInt32();
            br.ReadUInt32();
            var AssetListUnixTimestamp = br.ReadUInt64();
            var AssetListRootPath = br.ReadString();

            string indexHashUrl = $"{_hotfixJson.exResourceUrl}/client/{_platform}/DesignV_{IndexHash}.bytes";
            byte[] indexBytes = await _client.GetByteArrayAsync(indexHashUrl).ConfigureAwait(false);
            _designIndex = DesignIndex.Read(indexBytes);
            var entriesNum = _designIndex.Files.Sum(file => file.Entries.Count);
            var filesNum = _designIndex.Files.Count;
            exResourceLinks.Add(indexHashUrl);

            foreach (var file in _designIndex.Files)
            {
                exResourceLinks.Add($"{_hotfixJson.exResourceUrl}/client/{_platform}/{file.FileHash}.bytes");
            }
        }
        catch (HttpRequestException e)
        {
            _logger.LogError($"Request error: {e.Message}");
        }
        catch (Exception e)
        {
            _logger.LogError($"Unexpected error: {e.Message}");
        }

    }

    public async Task ParseLuaDatasAsync()
    {
        if (string.IsNullOrEmpty(_hotfixJson.luaUrl)) return;
        try
        {
            string url = $"{_hotfixJson.luaUrl}/client/{_platform}/M_LuaV.bytes";
            byte[] luaVBytes = await _client.GetByteArrayAsync(url).ConfigureAwait(false);
            luaLinks.Add(url);

            _logger.LogInfo("Successfully fetched M_LuaV data from the URL");

            using var ms = new MemoryStream(luaVBytes);
            using var br = new EndianBinaryReader(ms, Encoding.UTF8);
            var magic = new string(br.ReadChars(4));

            br.ReadInt16();
            var MetadataInfoSize = br.ReadInt32();
            ms.Seek(0xE, SeekOrigin.Current);

            var RemoteRevisionID = br.ReadInt32();

            var IndexHash = br.ReadHash();

            var AssetListFilesize = br.ReadUInt32();
            br.ReadUInt32();
            var AssetListUnixTimestamp = br.ReadUInt64();
            var AssetListRootPath = br.ReadString();

            string indexHashUrl = $"{_hotfixJson.luaUrl}/client/{_platform}/LuaV_{IndexHash}.bytes";
            byte[] indexBytes = await _client.GetByteArrayAsync(indexHashUrl).ConfigureAwait(false);
            _luaIndex = LuaIndex.Read(indexBytes);
            var entriesNum = _luaIndex.Files.Sum(file => file.Entries.Count);
            var filesNum = _luaIndex.Files.Count;
            luaLinks.Add(indexHashUrl);

            foreach (var file in _luaIndex.Files)
            {
                luaLinks.Add($"{_hotfixJson.luaUrl}/client/{_platform}/{file.FileHash}.bytes");
            }
        }
        catch (HttpRequestException e)
        {
            _logger.LogError($"Request error: {e.Message}");
        }
        catch (Exception e)
        {
            _logger.LogError($"Unexpected error: {e.Message}");
        }

    }
}