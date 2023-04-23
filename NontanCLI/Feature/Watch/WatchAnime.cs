﻿using NontanCLI.API;
using Spectre.Console;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using NontanCLI.Models;
using RestSharp;
using System.Diagnostics;
using System.Threading;
using System.IO;
using System.Net;
using System.Text;
using NontanCLI.Utils;
using System.Threading.Tasks.Sources;

namespace NontanCLI.Feature.Watch
{

    public class WatchAnime
    {

        private RestResponse req;
        private static WatchRoot response;


        private static string vtt_url;

        // Default port for the server
        
        private static string playerFilePath = @"Plyr/index.html";

        [Obsolete]
        public void WatchAnimeInvoke(string episode_id)
        {

    

            try
            {
                string Query = "";
                if (Constant.provider == "zoro")
                {
                    Query = $"anime/zoro/watch?episodeId={episode_id}";
                } else
                {
                    Query = $"anime/{Constant.provider}/watch/{episode_id}";
                }

                req = RestSharpHelper.GetResponse(Query);
                response = JsonConvert.DeserializeObject<WatchRoot>(req.Content);
            } catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }


            // foreach quality

            List<string> quality_selection = new List<string>();

            if (response == null)
            {

                AnsiConsole.MarkupLine("[red]Something wrong, i can feet it [/]");

                Thread.Sleep(10000);
                AnsiConsole.Clear();
                Program.MenuHandlerInvoke();
                return;
            }

            foreach (var item in response.sources)
            {
                quality_selection.Add(item.quality.ToString());   
            }

            quality_selection.Add("Back");

            var _prompt = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("\n[green]Select Quality Anime Video[/]?")
                    .PageSize(10)
                    .MoreChoicesText("[grey](Move up and down to reveal more menu)[/]")
                    .AddChoices(quality_selection.ToArray()));

            if (_prompt == "Back")
            {
                new MenuHandler().MenuHandlerInvoke();
            }
            else
            {

                for (int i = 0; i < response.sources.Count; i++)
                {
                    if (response.sources[i].quality.ToString() == _prompt)
                    {
                        var _selected_player = AnsiConsole.Prompt(
                            new SelectionPrompt<string>()
                                .Title("\n[green]Select video player available[/]?")
                                .PageSize(10)
                                .MoreChoicesText("[grey](Move up and down to reveal more menu)[/]")
                                .AddChoices(new[] { "VLC", "Browser" }));

                        if (_selected_player == "VLC")
                        {
                            PlayOnVLC(response.sources[i].url.ToString());
                        }
                        else if (_selected_player == "Browser")
                        {
                            if (response.subtitles != null)
                            {
                                List<string> subtitles = new List<string>();
                                foreach (var item in response.subtitles)
                                {
                                    subtitles.Add(item.lang.ToString());
                                }
                                var _selectted_subtitles = AnsiConsole.Prompt(
                                    new SelectionPrompt<string>()
                                        .Title("\n[green]Select Subtitle available[/]?")
                                        .PageSize(10)
                                        .MoreChoicesText("[grey](Move up and down to reveal more menu)[/]")
                                        .AddChoices(subtitles.ToArray()));

                                foreach (var sub in response.subtitles)
                                {
                                    if (_selectted_subtitles == sub.lang.ToString())
                                    {
                                        vtt_url = sub.url.ToString();
                                    }
                                }
                            } else
                            {
                                vtt_url = "";
                            }
                            PlayOnBrowser(response.sources[i].url.ToString(),vtt_url);

                        }
                        else
                        {
                            PlayOnBrowser(response.sources[i].url.ToString(), vtt_url);
                        }
                    }
                }
            }
        }

        [Obsolete]
        private void PlayOnVLC(string url)
        {

            string CURRENT_DIR = AppDomain.CurrentDomain.BaseDirectory;

            if (!File.Exists(CURRENT_DIR + "\\vlc\\vlc.exe"))
            {
                AnsiConsole.MarkupLine("[bold red]Cannot play with VLC, VLC is missing !![/]");
                Console.ReadKey();
                return;
            }


            //Process.Start(CURRENT_DIR + "\\vlc\\vlc.exe", url);

            Process vlcProcess = new Process();
            vlcProcess.StartInfo.FileName = CURRENT_DIR + "\\vlc\\vlc.exe";
            vlcProcess.StartInfo.Arguments = $"{url} --sub-file={vtt_url}";
            vlcProcess.Start();

            Thread.Sleep(5000);
            while (true)
            {
                if (Process.GetProcessesByName("vlc").Length == 0)
                {
                    Console.Clear();
                    Program.MenuHandlerInvoke();
                    break;
                }
            }
        }

        [Obsolete]
        private void PlayOnBrowser(string url, string sub_url)
        {

            string CURRENT_DIR = AppDomain.CurrentDomain.BaseDirectory;

            if (!File.Exists(CURRENT_DIR + playerFilePath))
            {
                AnsiConsole.MarkupLine("[bold red]Cannot play with Browser, Player is missing !![/]");
                Console.ReadKey();
                return;
            }



            if (!File.Exists(CURRENT_DIR + @"M3U8Proxy/M3U8Proxy.exe"))
            {
                AnsiConsole.MarkupLine("[bold red]Proxy Is Missing !![/]");
                Console.ReadKey();
                return;
            }


            Process vlcProcess = new Process();
            vlcProcess.StartInfo.FileName = CURRENT_DIR + "\\M3U8Proxy\\M3U8Proxy";
            vlcProcess.Start();

            Thread.Sleep(2000);
            // Bypass CORS
            Thread serverThread = new Thread(() =>
            {
                
                HttpListener listener = new HttpListener();


                listener.Prefixes.Add(Constant.baseAddress);
                listener.Start();
                AnsiConsole.MarkupLine($"Server started at [green]{Constant.baseAddress}.[/] Listening for requests...");
                AnsiConsole.MarkupLine("Press [green] Q [/] to stop the server..");

                while (listener.IsListening)
                {
                    HttpListenerContext ctx = listener.GetContext();
                    HttpListenerRequest request = ctx.Request;
                    HttpListenerResponse resp = ctx.Response;

                    string requestUrl = request.Url.AbsolutePath;

                    if (requestUrl == "/")
                    {
                        // Serve the HTML file
                        string html = File.ReadAllText(playerFilePath);
                        byte[] buffer = Encoding.UTF8.GetBytes(html);
                        resp.ContentType = "text/html";
                        resp.ContentLength64 = buffer.Length;
                        resp.OutputStream.Write(buffer, 0, buffer.Length);
                        resp.OutputStream.Close();
                    }
                    else if (requestUrl == "/hls/source.m3u8") // if you change this, you must change it too on player html 
                    {
                        // Redirect to the online m3u8 URL
                        resp.Redirect(Constant.baseProxyAddress + url);
                        resp.OutputStream.Close();

                    }   
                    else if (requestUrl == "/hls/subtitle") // if you change this, you must change it too on player html
                    {
                        // Redirect to the online m3u8 URL
                        Console.Write(sub_url);
                        resp.Redirect(sub_url);    
                        resp.OutputStream.Close();

                    }
                    else
                    {
                        // Return 404 for any other requests cuz i really lazy to make some feature lol
                        resp.StatusCode = 404;
                        resp.OutputStream.Close();
                    }

                    if (Console.KeyAvailable)
                    {
                        ConsoleKeyInfo keyInfo = Console.ReadKey(intercept: true);

                        if (keyInfo.Key == ConsoleKey.Q)
                        {
                            if (AnsiConsole.Confirm("Are you sure you want to exit the server?"))
                            {
                                listener.Stop();
                                Console.Clear();
                                Console.WriteLine("Server stopped.");
                                string proxyProcess = "M3U8Proxy"; // Specify the process name to kill

                                // Get all running processes with the specified process name
                                Process[] processes = Process.GetProcessesByName(proxyProcess);

                                // Kill each process in the list
                                foreach (Process process in processes)
                                {
                                    process.Kill();
                                }
                                Program.MenuHandlerInvoke();
                                break;
                            }
                            else
                            {
                                Console.WriteLine("Continuing server operation...");
                            }
                        }
                    }

                }

            });

            serverThread.Start();

            // Open the HTML file in the default web browser
            Process.Start(Constant.baseAddress);

            // Wait for the server thread to exit
            serverThread.Join();
        }
    }
}
