/*
    * Change extention: dotnet run -re '/Users/phantom/Documents/Projects/phantom.MVC.MuOnline/src/wwwroot/images/items/*.jpg' png
*/

using System;
using System.Linq;

class Program
{
    static void Main(string[] args)
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
    }
}