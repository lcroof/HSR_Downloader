using System;
using System.Net.Http;
using Newtonsoft.Json;

namespace HSR_DataDownloader
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Please provide the path to hotfix.json");
                return;
            }
    
            HttpClient client = new HttpClient();
            Logger logger = new Logger();
            HotfixJson hotfixJson = JsonConvert.DeserializeObject<HotfixJson>(File.ReadAllText(args[0]))!;
            string platform = "Windows"; // Replace with whatever you need
            BlockV blockV = new BlockV();
            DesignIndex designIndex = new DesignIndex();
            LuaIndex luaIndex = new LuaIndex();

            HotfixParser hotfixParser = new HotfixParser(client, logger, hotfixJson, platform, blockV, designIndex, luaIndex);

            await hotfixParser.ParseAsbDatasAsync();
            await hotfixParser.ParseLuaDatasAsync();
            await hotfixParser.ParseDesignDatasAsync();

            if (!Path.Exists("Asb"))
            {
                Directory.CreateDirectory("Asb");
            }
            if (!Path.Exists("DesignData"))
            {
                Directory.CreateDirectory("DesignData");
            }
            if (!Path.Exists("Lua"))
            {
                Directory.CreateDirectory("Lua");
            }

            Downloader downloader_asb = new(logger, "Asb");
            Downloader downloader_lua = new(logger, "Lua");
            Downloader downloader_designData = new(logger, "DesignData");

            await downloader_asb.DownloadFilesAsync(hotfixParser.asbLinks.Distinct().ToArray());
            await downloader_lua.DownloadFilesAsync(hotfixParser.luaLinks.Distinct().ToArray());
            await downloader_designData.DownloadFilesAsync(hotfixParser.exResourceLinks.Distinct().ToArray());
        }
    }
}