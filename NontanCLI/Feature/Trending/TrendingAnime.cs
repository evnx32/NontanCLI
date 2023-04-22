﻿using Newtonsoft.Json;
using NontanCLI.API;
using NontanCLI.Feature.Detail;
using NontanCLI.Models;
using RestSharp;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NontanCLI.Feature.Trending
{
    public class TrendingAnime
    {
        private int page = 1;

        private RestResponse req;
        private TrendingRoot response;

        [Obsolete]
        public void TrendingAnimeInvoke()
        {
            Table table = new Table();


            try
            {
                req = RestSharpHelper.GetResponse($"meta/anilist/trending?page={page}");
                response = JsonConvert.DeserializeObject<TrendingRoot>(req.Content);

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }


            if (response != null)
            {

                table.Title = new TableTitle($"\n\n[green]Trending Anime Page {response.currentPage}[/]");
                table.AddColumn("[green]ID[/]");
                table.AddColumn("[green]Title[/]");
                table.AddColumn("[green]Status[/]");
                table.AddColumn("[green]Type[/]");
                table.AddColumn("[green]Rating[/]");

                Regex regex = new Regex(@"[\[\]]");


                List<string> list_name = new List<string>();
                List<TrendingResultModel> popular_list = new List<TrendingResultModel>();
                foreach (var item in response.results)
                {
                    popular_list.Add(item);

                    string id = "";
                    string title = "";
                    string status = "";
                    string type = "";
                    string rating = "";
                    if (item.id != null)
                    {
                        id = item.id.ToString();
                    }
                    if (item.title.romaji != null)
                    {
                        
                        title = regex.Replace(item.title.romaji.ToString(), string.Empty);
                        list_name.Add(regex.Replace(item.title.romaji.ToString(), string.Empty));

                    }
                    else if (item.title.english != null)
                    {
                        title = regex.Replace(item.title.english.ToString(), string.Empty);
                        list_name.Add(regex.Replace(item.title.english.ToString(),  string.Empty));

                    }
                    if (item.status != null)
                    {
                        status = item.status.ToString();
                    }
                    if (item.type != null)
                    {
                        type = item.type.ToString();
                    }
                    if (item.rating != null)
                    {
                        rating = item.rating.ToString();
                    }


                    if (status == "Completed")
                    {
                        table.AddRow(id, title, "[green]" + status + "[/]", type, rating);

                    }
                    else if (status == "Ongoing")
                    {
                        table.AddRow(id, title, "[yellow]" + status + "[/]", type, rating);

                    }
                }

                AnsiConsole.Render(table);

            MenuPrompt:

                string _prompt = "";
                if (page > 1)
                {
                    _prompt = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("[green]Select Menu Available[/]?")
                        .PageSize(10)
                        .MoreChoicesText("[grey](Move up and down to reveal more menu)[/]")
                        .AddChoices(new[] {
                                                        "Select Anime","Next Page","Previous Page","Back"
                        }));
                }
                else
                {


                    _prompt = AnsiConsole.Prompt(
                        new SelectionPrompt<string>()
                            .Title("[green]Select Menu Available[/]?")
                            .PageSize(10)
                            .MoreChoicesText("[grey](Move up and down to reveal more menu)[/]")
                            .AddChoices(new[] {
                                                            "Select Anime","Next Page","Back"
                            }));
                }



                if (_prompt == "Select Anime")
                {
                    list_name.Add("Back");
                    var _selected_anime = AnsiConsole.Prompt(
                        new SelectionPrompt<string>()
                            .Title("[green]Select Anime Available[/]?")
                            .PageSize(10)
                            .MoreChoicesText("[grey](Move up and down to reveal more menu)[/]")
                            .AddChoices(list_name.ToArray()));

                    if (_selected_anime == "Back")
                    {
                        goto MenuPrompt;
                    }
                    Console.WriteLine(_selected_anime);

                    foreach (var i in popular_list)
                    {
                        if (i.title.romaji != null)
                        {
                            Console.WriteLine(i.title.romaji);

                            if (_selected_anime == regex.Replace(i.title.romaji.ToString(), string.Empty))
                            {
                                new DetailAnime().GetDetailParams(i.id);
                            }
                        }
                        else if (i.title.english != null)
                        {
                            if (_selected_anime == regex.Replace(i.title.english.ToString(), string.Empty))
                            {
                                new DetailAnime().GetDetailParams(i.id);
                            }
                        }
                        else
                        {
                            if (_selected_anime == regex.Replace(i.title.english.ToString(), string.Empty))
                            {
                                new DetailAnime().GetDetailParams(i.id);
                            }
                        }
                    }
                }
                else if (_prompt == "Next Page")
                {
                    page++;
                    AnsiConsole.Clear();
                    TrendingAnimeInvoke();
                }
                else if (_prompt == "Previous Page")
                {
                    page--;
                    AnsiConsole.Clear();
                    TrendingAnimeInvoke();
                }
                else
                {
                    AnsiConsole.Clear();
                    Program.MenuHandlerInvoke();
                }
            }
        }
    }
}
