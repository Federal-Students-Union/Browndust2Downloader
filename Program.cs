using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("我们欢欣鼓舞翘首以盼的新生，从一开始就注定是残缺的回忆，曾经初识时内心的悸动");
        Console.WriteLine("相知后相依的甜蜜在这春回大地的时节");
        Console.WriteLine("终究还是走到了它原本就不长久的尽头");
        Console.WriteLine("只剩下一片云淡风轻的寂寂新晨唱着别离的那首歌。");
        Console.WriteLine("如果记忆是座方城。那么，为了你，我甘愿画地为牢，将自己困顿其中。");
        Console.WriteLine("BY 北之歌");
        Console.WriteLine("程序启动中...");

        string jsonUrl = "https://cdn.bd2.pmang.cloud/ServerData/StandaloneWindows64/HD/20240730170652/catalog_alpha.json";
        string baseUrl = "https://cdn.bd2.pmang.cloud/ServerData/StandaloneWindows64/HD/20240730170652/";
        string jsonFolderPath = "Kitanojson";
        string bundleFolderPath = "Kitanobundle";
        string jsonFilePath = Path.Combine(jsonFolderPath, "catalog_alpha.json");

        Console.WriteLine("检查文件夹...");
        Directory.CreateDirectory(jsonFolderPath);
        Directory.CreateDirectory(bundleFolderPath);
        Console.WriteLine("文件夹准备好。");

        if (!File.Exists(jsonFilePath))
        {
            Console.WriteLine("开始下载 JSON 文件...");
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    var response = await client.GetAsync(jsonUrl, HttpCompletionOption.ResponseHeadersRead);
                    var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                    var jsonStream = await response.Content.ReadAsStreamAsync();
                    using (var fileStream = new FileStream(jsonFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                    {
                        var buffer = new byte[8192];
                        int bytesRead;
                        long totalRead = 0;
                        while ((bytesRead = await jsonStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await fileStream.WriteAsync(buffer, 0, bytesRead);
                            totalRead += bytesRead;
                            if (totalBytes != -1)
                            {
                                // 使用 int 类型的 totalBytes 来更新进度条
                                UpdateProgressBar((int)totalRead, (int)totalBytes);
                            }
                        }
                    }
                    Console.WriteLine("\nJSON 文件下载成功。");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"下载 JSON 文件时出错: {ex.Message}");
                    return;
                }
            }
        }
        else
        {
            Console.WriteLine("JSON 文件已存在，跳过下载。");
        }

        Console.WriteLine("开始读取 JSON 文件...");
        string jsonContent;
        try
        {
            jsonContent = await File.ReadAllTextAsync(jsonFilePath);
            Console.WriteLine("JSON 文件读取成功。");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"读取 JSON 文件时出错: {ex.Message}");
            return;
        }

        Console.WriteLine("开始解析 JSON 文件...");
        JObject jsonObject;
        try
        {
            jsonObject = JObject.Parse(jsonContent);
            Console.WriteLine("JSON 文件解析成功。");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"解析 JSON 文件时出错: {ex.Message}");
            return;
        }

        // 替换占位符的实际值
        string cdnInfo = jsonObject["BDNetwork.CdnInfo.Info"]?.ToString() ?? "default_cdn_info";
        string resolution = jsonObject["BDNetwork.CdnInfo.Resolution"]?.ToString() ?? "HD";
        string version = jsonObject["BDNetwork.CdnInfo.Version"]?.ToString() ?? "20240730170652";

        Console.WriteLine("提取 .bundle 文件名...");
        List<string> bundleFiles = new List<string>();

        foreach (var property in jsonObject.Properties())
        {
            if (property.Value.Type == JTokenType.Array)
            {
                foreach (var item in property.Value.Children())
                {
                    string bundlePath = item.ToString();
                    if (bundlePath.Contains("{BDNetwork.CdnInfo.Info}"))
                    {
                        string actualPath = bundlePath
                            .Replace("{BDNetwork.CdnInfo.Info}", cdnInfo)
                            .Replace("{BDNetwork.CdnInfo.Resolution}", resolution)
                            .Replace("{BDNetwork.CdnInfo.Version}", version);

                        string bundleName = actualPath.Substring(actualPath.LastIndexOf("\\") + 1);
                        bundleFiles.Add(bundleName);
                        Console.WriteLine($"发现 .bundle 文件: {bundleName}");
                    }
                }
            }
            else if (property.Value.Type == JTokenType.String)
            {
                string bundlePath = property.Value.ToString();
                if (bundlePath.Contains("{BDNetwork.CdnInfo.Info}"))
                {
                    string actualPath = bundlePath
                        .Replace("{BDNetwork.CdnInfo.Info}", cdnInfo)
                        .Replace("{BDNetwork.CdnInfo.Resolution}", resolution)
                        .Replace("{BDNetwork.CdnInfo.Version}", version);

                    string bundleName = actualPath.Substring(actualPath.LastIndexOf("\\") + 1);
                    bundleFiles.Add(bundleName);
                    Console.WriteLine($"发现 .bundle 文件: {bundleName}");
                }
            }
        }

        if (bundleFiles.Count == 0)
        {
            Console.WriteLine("没有找到 .bundle 文件。");
            return;
        }

        Console.WriteLine("开始下载 .bundle 文件...");
        using (HttpClient client = new HttpClient())
        {
            int totalFiles = bundleFiles.Count;
            int completedFiles = 0;
            int skippedFiles = 0;

            foreach (string bundleFile in bundleFiles)
            {
                string bundleUrl = baseUrl + bundleFile;
                string bundleFilePath = Path.Combine(bundleFolderPath, bundleFile);

                if (File.Exists(bundleFilePath))
                {
                    Console.WriteLine($"文件 {bundleFile} 已存在，跳过下载。");
                    skippedFiles++;
                    UpdateProgressBar(completedFiles + skippedFiles, totalFiles);
                    continue;
                }

                try
                {
                    Console.WriteLine($"下载 {bundleFile} 文件...");
                    byte[] bundleData = await client.GetByteArrayAsync(bundleUrl);
                    await File.WriteAllBytesAsync(bundleFilePath, bundleData);
                    completedFiles++;
                    UpdateProgressBar(completedFiles + skippedFiles, totalFiles);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"下载 {bundleFile} 文件时出错: {ex.Message}");
                }
            }

            // 最后更新进度条，确保显示完成状态
            UpdateProgressBar(completedFiles + skippedFiles, totalFiles);
        }

        Console.WriteLine("\n悲しみと苦しみの日々がありました。そしてこれからも、そんな日々があるかもしれません。それでも、忘れません。大切な日々です");
        Console.WriteLine("\nたとえ千件のメールを送っても、心と心の距離は一センチほど縮めたことがありません");
        Console.WriteLine("\nもし叶うなら、あなたを連れて、綺麗な嵐を見に行きたいです。美しい渓谷を見に行きたいです。この気持ちは、人間はどう呼ばれていますか");
        Console.WriteLine("\n世界は美しいです。悲しみと涙を満たしても、目を開けてください。あなたのやりたいことをして、あなたのなりたい人になりたいです。友達を見つけに行きます。焦らずにゆっくり成長してください");
        Console.WriteLine("\nうへへへ～このおじさんはこういう日が苦手なんだよ～みんなキラキラ浮かれてて……まあ、偶にはこういうのも悪くないけど。");
        Console.WriteLine("\n私一人こんなに楽しんでて良いのかな……余計な心配だって？……そうかな？");
        Console.WriteLine("\nあまり肩に力入れなくていいよ～リラックス（Relax）が大事だし。");
        Console.WriteLine("\n所有文件下载完成。");
    }

    static void UpdateProgressBar(int completed, int total)
    {
        const int barWidth = 50;
        double progress = (double)completed / total;
        int progressWidth = (int)(progress * barWidth);

        Console.Clear();  // 清除控制台内容
        Console.Write("[");
        Console.Write(new string('#', progressWidth));
        Console.Write(new string('-', barWidth - progressWidth));
        Console.Write("] ");
        Console.Write($"{completed}/{total} ({(progress * 100):0.00}%)");
    }
}
