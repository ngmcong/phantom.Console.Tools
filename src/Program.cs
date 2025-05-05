/*
    * Change extention: dotnet run -re '/Users/phantom/Documents/Projects/phantom.MVC.MuOnline/src/wwwroot/images/items/*.jpg' png
*/

using System;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

class Program
{
    static async Task Main(string[] args)
    {
        if (args.Any(x => x == "-re"))
        {
            var aParams = args.Skip(args.ToList().IndexOf("-re") + 1).Take(2);
            if (aParams.Count() < 2)
            {
                System.Console.WriteLine("Error: -re requires two parameters.");
                return;
            }
            var first = aParams.ElementAt(0);
            var dir = first.Substring(0, first.LastIndexOf('/'));
            var searchPattern = first.Replace(dir + "/", "");
            var files = System.IO.Directory.GetFiles(dir, searchPattern);
            var orgExt = searchPattern.TrimStart('*');
            var ext = aParams.ElementAt(1);
            foreach (string item in files)
            {
                File.Move(item, item.Replace(orgExt, $".{ext}"));
            }
        }
        //dotnet run -apc "D:\Projects\InfomedHIS"
        else if (args.Any(x => x == "-apc"))
        {
            var dirLocation = args[1];
            Console.WriteLine(dirLocation);
            HttpClient client = new HttpClient();
            // Create the base64 encoded credentials
            string credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"ngmcong:"));
            // Set the Authorization header
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
            var response = await client.GetAsync("https://dev.azure.com/ngmcong/InfomedHIS/_apis/git/repositories?api-version=7.1");
            // Check if the request was successful
            if (response.IsSuccessStatusCode)
            {
                string responseContent = await response.Content.ReadAsStringAsync();
                var responseValue = JsonSerializer.Deserialize<AzureRepositories>(responseContent);
                Console.WriteLine($"Request successful: {responseValue?.value?.Count()} items.");
                if (responseValue == null) return;
                foreach (var item in responseValue!.value!)
                {
                    var folderPath = Path.Combine(dirLocation, item.name!);
                    if (Directory.Exists(folderPath))
                    {
                        Console.WriteLine($"Folder {folderPath} exists");
                        continue;
                    }
                    var command = $"git clone {item.webUrl!}";
                    Console.WriteLine(command);
                    ProcessStartInfo psi = new ProcessStartInfo("cmd.exe");
                    psi.WorkingDirectory = dirLocation;
                    psi.Arguments = $"/c {command}";
                    psi.UseShellExecute = false;
                    psi.RedirectStandardOutput = true;
                    psi.RedirectStandardError = true; // Capture error output as well
                    psi.CreateNoWindow = true;       // Optional: Hide the cmd window
                    var process = new Process()!;
                    process.StartInfo = psi;
                    process.Start();
                    string error = await process.StandardError.ReadToEndAsync();
                    await process.WaitForExitAsync();
                    Console.WriteLine($"{error}");
                    Console.WriteLine($"Git clone {item.name!} with exit code: {process.ExitCode}");
                }
            }
            else
            {
                Console.WriteLine($"Request failed with status code: {response.StatusCode}");
                string errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Error details: {errorContent}");
            }
        }
    }
}
public class AzureRepositories
{
    public IEnumerable<AzureRepository>? value { get; set; }
}
public class AzureRepository
{
    public string? id { get; set; }
    public string? name { get; set; }
    public string? url { get; set; }
    public string? webUrl { get; set; }
}