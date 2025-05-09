/*
    * Change extention: dotnet run -re '/Users/phantom/Documents/Projects/phantom.MVC.MuOnline/src/wwwroot/images/items/*.jpg' png
*/

using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Util.Store;

class Program
{
    static readonly string[] Scopes = {
        "email", // Request the user's email address
        "profile", // Request basic profile information
        "https://www.googleapis.com/auth/drive",
        "https://www.googleapis.com/auth/drive.file",
        "https://www.googleapis.com/auth/drive.readonly",
        "https://www.googleapis.com/auth/spreadsheets",
        "https://www.googleapis.com/auth/spreadsheets.readonly",
        // Add other scopes your app needs, e.g., for Google Drive, Sheets, etc.
    };
    static async Task ClearTokenAsync(string userId, string applicationName)
    {
        var fileDataStore = new FileDataStore(applicationName);
        string folderPath = fileDataStore.FolderPath;
        string searchPattern = $"Google.Apis.Auth.OAuth2.Responses.TokenResponse-{userId}*";

        if (Directory.Exists(folderPath))
        {
            Console.WriteLine($"Token storage folder found: {folderPath}");
            string[] filesToDelete = Directory.GetFiles(folderPath, searchPattern);
            foreach (string file in filesToDelete)
            {
                try
                {
                    File.Delete(file);
                    Console.WriteLine($"Deleted token file: {file}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error deleting {file}: {ex.Message}");
                }
            }
        }
        else
        {
            Console.WriteLine($"Token storage folder not found: {folderPath}");
        }
        // Optionally, you can clear all files in the store
        await fileDataStore.ClearAsync();
    }
    public static async Task RevokeTokenAsync(UserCredential credential)
    {
        HttpClient _httpClient = new HttpClient();
        if (credential?.Token?.AccessToken != null)
        {
            string accessToken = credential.Token.AccessToken;
            string revokeEndpoint = "https://oauth2.googleapis.com/revoke";
            var content = new StringContent($"token={accessToken}", System.Text.Encoding.UTF8, "application/x-www-form-urlencoded");

            try
            {
                var response = await _httpClient.PostAsync(revokeEndpoint, content);
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Access token revoked successfully.");
                }
                else
                {
                    Console.WriteLine($"Failed to revoke access token. Status code: {response.StatusCode}");
                    string error = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Error details: {error}");
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Error revoking access token: {ex.Message}");
            }

            // Optionally revoke the refresh token as well for more complete "logout"
            if (credential.Token.RefreshToken != null)
            {
                var refreshContent = new StringContent($"token={credential.Token.RefreshToken}", System.Text.Encoding.UTF8, "application/x-www-form-urlencoded");
                try
                {
                    var response = await _httpClient.PostAsync(revokeEndpoint, refreshContent);
                    if (response.IsSuccessStatusCode)
                    {
                        Console.WriteLine("Refresh token revoked successfully.");
                    }
                    else
                    {
                        Console.WriteLine($"Failed to revoke refresh token. Status code: {response.StatusCode}");
                        string error = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"Error details: {error}");
                    }
                }
                catch (HttpRequestException ex)
                {
                    Console.WriteLine($"Error revoking refresh token: {ex.Message}");
                }
            }
        }
        else
        {
            Console.WriteLine("No access token available to revoke.");
        }
    }
    static async Task Main(string[] args)
    {
        //Change file extention
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
        //Clone Azure repositories from project
        //dotnet run -apc "D:\Projects\InfomedHIS"
        else if (args.Any(x => x == "-apc"))
        {
            var dirLocation = args[1];
            if (Directory.Exists(dirLocation) == false)
            {
                Console.WriteLine($"Directory {dirLocation} not found");
                return;
            }

            var azureToken = string.Empty;
            HttpResponseMessage response;
            // await ClearTokenAsync("ngmcong", "phantom.Console.Tools");
            UserCredential credential;
            await using (var stream = new FileStream("credentials.json", FileMode.Open, FileAccess.Read))
            {
                credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.FromStream(stream).Secrets,
                    Scopes,
                    "ngmcong",
                    CancellationToken.None,
                    new FileDataStore(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData), true)
                );
                // await RevokeTokenAsync(credential);
                if (credential != null && credential.Token != null)
                {
                    string accessToken = credential.Token.AccessToken;
                    Console.WriteLine($"Bearer Token (Access Token): {accessToken}");
                    // You can now use this accessToken in the Authorization header of your HTTP requests
                    // For example:
                    // client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                    HttpClient httpClient = new HttpClient();
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                    response = await httpClient.GetAsync("https://sheets.googleapis.com/v4/spreadsheets/1hS_38TjKFEu5dbY65zcaruMeASZ3KVqq1E5fpNOgpY4/values/A:B");
                    if (response.IsSuccessStatusCode)
                    {
                        string responseContent = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"responseContent: {responseContent.Substring(0, 200)}");
                        var responseValue = JsonSerializer.Deserialize<GoogleSheetAPI>(responseContent);
                        azureToken = responseValue?.values?.FirstOrDefault(x => x.First() == "Azure")?.ElementAt(1);
                        Console.WriteLine($"azureToken: {azureToken}");
                    }
                    else
                    {
                        Console.WriteLine($"Request failed with status code: {response.StatusCode}");
                        string errorContent = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"Error details: {errorContent}");
                    }
                }
                else
                {
                    Console.WriteLine("Could not retrieve the access token.");
                }
            }

            Console.WriteLine(dirLocation);
            HttpClient client = new HttpClient();
            // Create the base64 encoded credentials
            string credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"ngmcong:{azureToken}"));
            // Set the Authorization header
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
            response = await client.GetAsync("https://dev.azure.com/ngmcong/InfomedHIS/_apis/git/repositories?api-version=7.1");
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
                    var retVal = await RunCommandLine(command, dirLocation);
                    Console.WriteLine($"Git clone {item.name!} with exit code: {retVal.exitCode}");
                }
            }
            else
            {
                Console.WriteLine($"Request failed with status code: {response.StatusCode}");
                string errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Error details: {errorContent}");
            }
        }
        //dotnet run -rn "/Volumes/SSD" " | noname"
        else if (args.Any(x => x == "-rn"))
        {
            var aParams = args.Skip(args.ToList().IndexOf("-rn") + 1).Take(3);
            if (aParams.Count() < 2)
            {
                System.Console.WriteLine("Error: -rn requires three parameters.");
                return;
            }
            var dir = aParams.ElementAt(0);
            if (Directory.Exists(dir) == false)
            {
                System.Console.WriteLine($"Directory {dir} not found.");
                return;
            }
            var org = aParams.ElementAt(1);
            var rpc = aParams.Count() == 3 ? aParams.ElementAt(2) : "";
            var files = System.IO.Directory.GetFiles(dir, "*.*");
            foreach (string item in files)
            {
                if (File.Exists(item) == false) continue;
                File.Move(item, item.Replace(org, rpc));
            }
        }
        //Create docker image
        //dotnet run -cdi "D:\Projects\InfomedHIS" "1.0"
        else if (args.Any(x => x == "-cdi"))
        {
            var dirLocation = args[1];
            var version = args[2];
            var directories = Directory.GetDirectories(dirLocation);
            var startPort = 8080;
            foreach (var dir in directories)
            {
                if (dir.Contains(".API.") == false) continue;
                if (File.Exists(Path.Combine(dir, "Dockerfile")) == false) continue;
                startPort++;
                var apiName = dir.Split(@"\").Last();
                var filter = await RunCommandLine($"docker images --filter \"reference={apiName.ToLower()}:{version}\"", null);
                if (filter.exitCode == 0 && filter.outputString.Split('\n').Count() == 3) goto dockerrun;
                var retVal = await RunCommandLine($"docker build --network=host --build-arg NUGET_USER=ngmcong --build-arg NUGET_PASS=3cTMJtDH7gpfBmnvaLYldW9l307UBgB4F0ginuib6hjZOgg7D8Q6JQQJ99BCACAAAAAAAAAAAAASAZDO124W -t {apiName.ToLower()}:lastest -t {apiName.ToLower()}:{version} .", dir);
                Console.WriteLine($"Docker build in {apiName} with exit code: {retVal.exitCode}");
                if (retVal.exitCode != 0) break;
            dockerrun:
                filter = await RunCommandLine($"docker ps -a --filter \"name={apiName}\"", null);
                if (filter.exitCode == 0 && filter.outputString.Split('\n').Count() == 3) continue;
                retVal = await RunCommandLine($"docker run -d -p {startPort}:80 --name {apiName} {apiName.ToLower()}:lastest", dir);
                Console.WriteLine($"Docker run in {apiName} with exit code: {retVal.exitCode}");
            }
        }
    }
    private static async Task<(string errorString, string outputString, int exitCode)> RunCommandLine(string command
        , string? workingDirectory)
    {
        Console.WriteLine(command);
        ProcessStartInfo psi = new ProcessStartInfo("cmd.exe");
        if (string.IsNullOrEmpty(workingDirectory) == false) psi.WorkingDirectory = workingDirectory;
        psi.Arguments = $"/c {command}";
        psi.UseShellExecute = false;
        psi.RedirectStandardOutput = true;
        psi.RedirectStandardError = true; // Capture error output as well
        psi.CreateNoWindow = true;       // Optional: Hide the cmd window
        var process = new Process()!;
        process.StartInfo = psi;
        string errorString = string.Empty;
        process.ErrorDataReceived += (s, e) =>
        {
            Console.WriteLine(e.Data);
            errorString += e.Data;
        };
        process.Start();
        process.BeginErrorReadLine();
        string outputString = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();
        return (errorString, outputString, process.ExitCode);
    }
}
public class GoogleSheetAPI
{
    public IEnumerable<IEnumerable<string>>? values { get; set; }
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