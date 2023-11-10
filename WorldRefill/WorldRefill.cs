﻿using Microsoft.Data.Sqlite;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Modules;
using Terraria.UI;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.DB;
using TShockAPI.Hooks;
using TShockAPI.Net;
using Terraria.GameContent.Biomes;
using Terraria.WorldBuilding;
using System.Security.Cryptography;
using Terraria.GameContent.Generation;
using System.Threading.Tasks;
using OTAPI;
using System.Diagnostics.Tracing;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Net;
using ReLogic.Threading;
using ReLogic.Utilities;
using Terraria.IO;
using Terraria.GameContent.UI.States;
using System.Text;
using System.IO;
using WorldRefill.MicroBiomeExtensions;
using Terraria.GameContent.Biomes.CaveHouse;

namespace WorldRefill
{
    [ApiVersion(2, 1)]
    public class WorldRefill : TerrariaPlugin
    {

        public static Config config;

        public WorldRefill(Main game)
            : base(game)
        {
            Order = +2;
        }
        #region Plugin Info
        public override Version Version
        {
            get { return new Version(2, 1, 3); }
        }
        public override string Name
        {
            get { return "World Refill"; }
        }
        public override string Author
        {
            get { return "k0rd, IcyPhoenix and Enerdy (Updated by Matheis)"; }
        }
        public override string Description
        {
            get { return "Refill your world!"; }
        }



        public static bool isTaskRunning { get; set; }
        public static int realcount { get; set; }



        #endregion

        #region Initialize
        public override void Initialize()
        {
            #region Gen Commands
            Commands.ChatCommands.Add(new Command("worldrefill.generate", Generate, "generate", "gen")
            );


            #region Commands to Add

            //Commands.ChatCommands.Add(new Command("tshock.world.causeevents", DoLake, "genlake"));			//lake
            //Commands.ChatCommands.Add(new Command("tshock.world.causeevents", DoMountain, "genmountain"));	//mountain

            // Any other additions to 1.4
            #endregion
            #endregion
            
            ConfigFunctions.ReadConfig();

            GeneralHooks.ReloadEvent += OnReload;






        }
        #endregion

        #region Dispose
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                GeneralHooks.ReloadEvent -= OnReload;
            }
            base.Dispose(disposing);
        }
        #endregion






        #region Reload Command
        // Config Reload
        private async void OnReload(ReloadEventArgs args)
        {
            if (await Task.Run(() => ConfigFunctions.ReadConfig()))
            {
                if (!args.Player.Active) args.Player.SendSuccessMessage($"[{Name}] {Name} Config reloaded.");

                else args.Player.SendSuccessMessage($"[[c/FFFFFF:{Name}]] {Name} Config reloaded.");
            }
            else
                if (!args.Player.Active) args.Player.SendErrorMessage($"[{Name}] Error reading config. Attempting to generate a new one...");

                else args.Player.SendErrorMessage($"[[c/FFFFFF:{Name}]] Error reading config. Attempting to generate a new one...");

            return;
        }
        #endregion








        readonly IReadOnlyList<string> optionslist = new List<string>
            {
            "crystals",
                "pots",
                "orbs",
                "altars",
                "cavetraps",
                "templetraps",
                "lavatraps",
                "sandtraps",
                "statues",
                "ores",
                "webs",
                "shrooms",
                "chests"



            };
        readonly IReadOnlyList<string> structslist = new List<string>
        {
            "trees",
            "dungeon",
            //"temple"
            "livingtree",
            "cavehouse",
            "pyramid",
            "hellevator",
            "island",
            "world",






        };
        readonly IReadOnlyList<string> biomelist = new List<string>
        {
            "hive",
            "granite",
            "marble",
            "terragrim"
            
        };
        private async void PrintOptions(CommandArgs args)
        {
            await Task.Run(() =>
            {
                string options = "";
                string structs = "";
                string biomes = "";
                args.Player.SendErrorMessage("Resources:");
                foreach (string option in optionslist)
                {
                    options += $"| {option} | ";
                }
                args.Player.SendErrorMessage(options);
                args.Player.SendErrorMessage("Structures:");
                foreach (string structure in structslist)
                {
                    structs += $"| {structure} | ";
                }
                args.Player.SendErrorMessage(structs);
                args.Player.SendErrorMessage("Biomes:");
                foreach (string biome in biomelist)
                {
                    biomes += $"| {biome} | ";
                }
                args.Player.SendErrorMessage(biomes);
            });

        }




        #region Generate Command
        private async void Generate(CommandArgs args)
        {
            
            List<string> lines;

            if (args.Parameters.Count < 1)
            {
                args.Player.SendErrorMessage($"[[c/FFFFFF:{Name}]] Invalid Syntax! " +
                      "Please refer to the following options...");
                PrintOptions(args);

                return;
            }

            short amount;
            if (structslist.Contains(args.Parameters[0].ToLowerInvariant()) || biomelist.Contains(args.Parameters[0].ToLowerInvariant()))
            {
                amount = 0;
               
            }
            else
            {



                var amountregex = new Regex(@"\b(?<amount>[1-9]\d*){1}$");
                Match amountmatch;
                try
                {

                    amountmatch = amountregex.Match(args.Parameters[1]);
                }
                catch (ArgumentOutOfRangeException)
                {
                    amountmatch = amountregex.Match("0");
                }



                if (!amountmatch.Success || double.Parse(args.Parameters[1]) < 1)
                {
                    bool found = false;
                    foreach (string option in optionslist)
                    {
                        if (option == args.Parameters[0].ToLowerInvariant())
                        {
                            found = true;
                            if (option == "statues" || option == "ores")
                            {
                                string singular = option.Substring(0, option.LastIndexOf('s'));
                                args.Player.SendErrorMessage($"[[c/FFFFFF:{Name}]] Invalid Syntax! Use {Commands.Specifier}gen {option} <Amount of {option}> [Specified {singular}]");
                                if (option == "statues")
                                {
                                    args.Player.SendInfoMessage($"[[c/FFFFFF:{Name}]] Full list of statues :");

                                    lines = PaginationTools.BuildLinesFromTerms(StoredLists.getStatueList().ToArray());
                                    lines.ForEach(args.Player.SendInfoMessage);
                                }
                                else if (option == "ores")
                                {
                                    args.Player.SendInfoMessage($"[[c/FFFFFF:{Name}]] Full list of ores :");

                                    lines = PaginationTools.BuildLinesFromTerms(StoredLists.ores.Keys.ToArray());
                                    lines.ForEach(args.Player.SendInfoMessage);
                                }

                            }
                            else args.Player.SendErrorMessage($"[[c/FFFFFF:{Name}]] Invalid Syntax! Use {Commands.Specifier}gen {option} <Amount of {option}>");
                            break;
                        }
                    }
                    if (!found)
                    {
                        args.Player.SendErrorMessage($"[[c/FFFFFF:{Name}]] Invalid Syntax! " +
                        "Please refer to the following options...");
                        PrintOptions(args);

                    }
                    return;
                }

                try
                {
                    amount = Int16.Parse(amountmatch.Value);
                }
                catch (OverflowException)
                {
                    amount = Int16.MaxValue;
                }

            }



            //maxtries = retry amounts if generation of object fails (this is used to prevent lag issues)


            int tryX;
            int tryY;
            //realcount = actual amount of objects generated




            #region Case Options
            switch (args.Parameters[0].ToLowerInvariant())
            {
                #region Life Crystals

                case "crystals":


                    //Attempting to generate Objects


                    if (!isTaskRunning)
                    {


                        await Regen.AsyncGenLifeCrystals(amount);



                        //Notify user on success

                        args.Player.SendSuccessMessage($"[[c/FFFFFF:{Name}]] Generated and hid {realcount} Life Crystals");
                        if (realcount != 0) TSPlayer.All.SendMessage($"[[c/FFFFFF:{Name}]] [c/BCFF00:{args.Player.Name}] has generated and hid [c/BCFF00:{realcount}] Life Crystals", 71, 8, 185); InformPlayers(args.Player.Name, args.Parameters[0]);

                    }
                    else args.Player.SendErrorMessage($"[[c/FFFFFF:{Name}]] Another Generation is in Progress, Please try again later!");

                    break;



                #endregion
                #region Pots
                case "pots":

                    if (!isTaskRunning)
                    {


                        await Regen.AsyncGenPots(amount);



                        //Notify user on success

                        args.Player.SendSuccessMessage($"[[c/FFFFFF:{Name}]] Generated and hid {realcount} Pots.");
                        if (realcount != 0) TSPlayer.All.SendMessage($"[[c/FFFFFF:{Name}]] [c/BCFF00:{args.Player.Name}] has generated and hid [c/BCFF00:{realcount}] Pots.", 71, 8, 185);
                        InformPlayers(args.Player.Name, args.Parameters[0]);

                    }
                    else args.Player.SendErrorMessage($"[[c/FFFFFF:{Name}]] Another Generation is in Progress, Please try again later!");

                    break;



                #endregion
                #region Orbs
                case "orbs":



                    if (!isTaskRunning)
                    {


                        await Regen.AsyncGenerateOrbs(amount);



                        //Notify user on success

                        if (!WorldGen.crimson)
                        {
                            args.Player.SendSuccessMessage($"[[c/FFFFFF:{Name}]] Generated and hid {realcount} Shadow Orbs.");
                            if (realcount != 0) TSPlayer.All.SendMessage($"[[c/FFFFFF:{Name}]] [c/BCFF00:{args.Player.Name}] has generated and hid [c/BCFF00:{realcount}] Shadow Orbs.", 71, 8, 185);
                        }
                        else
                        {
                            args.Player.SendSuccessMessage($"[[c/FFFFFF:{Name}]] Generated and hid {realcount} Crimson Hearts.");
                            if (realcount != 0) TSPlayer.All.SendMessage($"[[c/FFFFFF:{Name}]] [c/BCFF00:{args.Player.Name}] has generated and hid [c/BCFF00:{realcount}] Crimson Hearts.", 71, 8, 185);
                        }
                        InformPlayers(args.Player.Name, args.Parameters[0]);

                    }
                    else args.Player.SendErrorMessage($"[[c/FFFFFF:{Name}]] Another Generation is in Progress, Please try again later!");

                    break;



                #endregion
                #region Altars
                case "altars":




                    if (!isTaskRunning)
                    {


                        await Regen.AsyncGenerateAltars(amount);



                        //Notify user on success

                        if (!WorldGen.crimson)
                        {
                            args.Player.SendSuccessMessage($"[[c/FFFFFF:{Name}]] Generated and hid {realcount} Demon Altars.");
                            if (realcount != 0) TSPlayer.All.SendMessage($"[[c/FFFFFF:{Name}]] [c/BCFF00:{args.Player.Name}] has generated and hid [c/BCFF00:{realcount}] Demon Altars.", 71, 8, 185);
                        }
                        else
                        {
                            args.Player.SendSuccessMessage($"[[c/FFFFFF:{Name}]] Generated and hid {realcount} Crimson Altars.");
                            if (realcount != 0) TSPlayer.All.SendMessage($"[[c/FFFFFF:{Name}]] [c/BCFF00:{args.Player.Name}] has generated and hid [c/BCFF00:{realcount}] Crimson Altars.", 71, 8, 185);
                        }
                        InformPlayers(args.Player.Name, args.Parameters[0]);

                    }
                    else args.Player.SendErrorMessage($"[[c/FFFFFF:{Name}]] Another Generation is in Progress, Please try again later!");


                    break;


                #endregion
                #region Cave Traps
                case "cavetraps":


                    if (!isTaskRunning)
                    {


                        await Regen.AsyncGenerateCavetraps(amount);



                        //Notify user on success

                        args.Player.SendSuccessMessage($"[[c/FFFFFF:{Name}]] Generated and hid {realcount} Randomized Cave Traps.");
                        if (realcount != 0) TSPlayer.All.SendMessage($"[[c/FFFFFF:{Name}]] [c/BCFF00:{args.Player.Name}] has generated and hid [c/BCFF00:{realcount}] Randomized Cave Traps.", 71, 8, 185);
                        InformPlayers(args.Player.Name, args.Parameters[0]);

                    }
                    else args.Player.SendErrorMessage($"[[c/FFFFFF:{Name}]] Another Generation is in Progress, Please try again later!");
                    break;



                #endregion
                #region Temple Traps
                case "templetraps":

                    if (!isTaskRunning)
                    {


                        await Regen.AsyncGenerateTempletraps(amount);



                        //Notify user on success

                        args.Player.SendSuccessMessage($"[[c/FFFFFF:{Name}]] Generated and hid {realcount} Randomized Temple Traps.");
                        if (realcount != 0) TSPlayer.All.SendMessage($"[[c/FFFFFF:{Name}]] [c/BCFF00:{args.Player.Name}] has generated and hid [c/BCFF00:{realcount}] Randomized Temple Traps.", 71, 8, 185);
                        InformPlayers(args.Player.Name, args.Parameters[0]);

                    }
                    else args.Player.SendErrorMessage($"[[c/FFFFFF:{Name}]] Another Generation is in Progress, Please try again later!");
                    break;




                #endregion

                #region Lava Traps
                case "lavatraps":


                    if (!isTaskRunning)
                    {


                        await Regen.AsyncGenerateLavatraps(amount);



                        //Notify user on success

                        args.Player.SendSuccessMessage($"[[c/FFFFFF:{Name}]] Generated and hid {realcount} Lava Traps.");
                        if (realcount != 0) TSPlayer.All.SendMessage($"[[c/FFFFFF:{Name}]] [c/BCFF00:{args.Player.Name}] has generated and hid [c/BCFF00:{realcount}] Lava Traps.", 71, 8, 185);
                        InformPlayers(args.Player.Name, args.Parameters[0]);

                    }
                    else args.Player.SendErrorMessage($"[[c/FFFFFF:{Name}]] Another Generation is in Progress, Please try again later!");
                    break;

                #endregion
                #region Sand Traps
                case "sandtraps":


                    if (!isTaskRunning)
                    {


                        await Regen.AsyncGenerateSandtraps(amount);



                        //Notify user on success

                        args.Player.SendSuccessMessage($"[[c/FFFFFF:{Name}]] Generated and hid {realcount} Sand Traps.");
                        if (realcount != 0) TSPlayer.All.SendMessage($"[[c/FFFFFF:{Name}]] [c/BCFF00:{args.Player.Name}] has generated and hid [c/BCFF00:{realcount}] Sand Traps.", 71, 8, 185);
                        InformPlayers(args.Player.Name, args.Parameters[0]);

                    }
                    else args.Player.SendErrorMessage($"[[c/FFFFFF:{Name}]] Another Generation is in Progress, Please try again later!");

                    break;


                #endregion
                #region Statues
                case "statues":
                    if (!isTaskRunning)
                    {


                        WorldGen.SetupStatueList();

                        List<Point16> statuelist = GenVars.statueList.ToList(); //re-writing statuelist to include turtle and owl statues.
                        statuelist.Add(new Point16(TileID.Statues, 76));
                        statuelist.Add(new Point16(TileID.Statues, 79));
                        statuelist.Add(new Point16(TileID.BoulderStatue, 0));
                        GenVars.statueList = statuelist.ToArray();





                        if (args.Parameters.Count == 2)
                        {

                            await Regen.AsyncGenerateRandStatues(amount);

                            //Notify user on success

                            args.Player.SendSuccessMessage($"[[c/FFFFFF:{Name}]] Generated and hid {realcount} Statues.");
                            if (realcount != 0) TSPlayer.All.SendMessage($"[[c/FFFFFF:{Name}]] [c/BCFF00:{args.Player.Name}] has generated and hid [c/BCFF00:{realcount}] Statues.", 71, 8, 185);
                            InformPlayers(args.Player.Name, args.Parameters[0]);

                        }

                        else
                        {

                            int stindex = 0;

                            Dictionary<string, Point16> statues = new Dictionary<string, Point16>();
                            foreach (Point16 statue in GenVars.statueList)
                            {

                                statues.Add(StoredLists.getStatueList()[stindex], statue);


                                stindex++;

                            }








                            string selStatue = args.Parameters[2].ToLowerInvariant();
                            List<string> stlist = statues.Keys.ToList<string>();
                            var findStatue = TileValidation.FindMatches(selStatue, stlist);


                            if (findStatue.Count > 1)
                            {
                                args.Player.SendErrorMessage($"[[c/FFFFFF:{Name}]] More than one match found: ");

                                lines = PaginationTools.BuildLinesFromTerms(findStatue.ToArray());
                                lines.ForEach(args.Player.SendInfoMessage);
                            }
                            else if (findStatue.Count < 1)
                            {
                                args.Player.SendErrorMessage($"[[c/FFFFFF:{Name}]] No matches found for statue '{selStatue}'");
                                args.Player.SendInfoMessage($"[[c/FFFFFF:{Name}]] Full list of statues :");

                                lines = PaginationTools.BuildLinesFromTerms(stlist.ToArray());
                                lines.ForEach(args.Player.SendInfoMessage);
                            }
                            else
                            {
                                statues.TryGetValue(findStatue[0], out Point16 statue);


                                await (Regen.AsyncGenerateStatues(amount, statue.X, statue.Y));

                                //Notify user on success

                                args.Player.SendSuccessMessage($"[[c/FFFFFF:{Name}]] Generated and hid {realcount} {findStatue[0]} Statues.");
                                if (realcount != 0) TSPlayer.All.SendMessage($"[[c/FFFFFF:{Name}]] [c/BCFF00:{args.Player.Name}] has generated and hid [c/BCFF00:{realcount}] {findStatue[0]} Statues.", 71, 8, 185);
                                InformPlayers(args.Player.Name, args.Parameters[0]);







                            }

                        }
                    }
                    else args.Player.SendErrorMessage($"[[c/FFFFFF:{Name}]] Another Generation is in Progress, Please try again later!");
                    break;

                #endregion
                #region Ores
                case "ores":
                    if (!isTaskRunning)
                    {

                        ushort oreID;

                        List<ushort[]> oreTiers = new List<ushort[]> {
                        new ushort[] {TileID.Copper,TileID.Tin, TileID.Iron, TileID.Lead },
                        new ushort[] {TileID.Silver, TileID.Tungsten, TileID.Gold, TileID.Platinum,TileID.Sapphire,TileID.Ruby,TileID.Topaz,TileID.Emerald,TileID.Amethyst,TileID.Diamond},
                        new ushort[] {TileID.Demonite, TileID.Crimtane},
                        new ushort[] {TileID.Hellstone},
                        new ushort[] {TileID.Cobalt,TileID.Palladium},
                        new ushort[] {TileID.Mythril, TileID.Orichalcum},
                        new ushort[] {TileID.Adamantite, TileID.Titanium},
                        new ushort[] {TileID.Chlorophyte}


                    };




                        if (amount > 1000) args.Player.SendInfoMessage($"[[c/FFFFFF:{Name}]] This one may take a while to load. Please wait...");

                        if (args.Parameters.Count == 2)
                        {



                            await (Regen.AsyncGenerateRandOres(amount, oreTiers));



                            //Notify user on success

                            args.Player.SendSuccessMessage($"[[c/FFFFFF:{Name}]] Generated and hid {realcount} randomized ores.");
                            if (realcount != 0) TSPlayer.All.SendMessage($"[[c/FFFFFF:{Name}]] [c/BCFF00:{args.Player.Name}] has generated and hid [c/BCFF00:{realcount}] randomized ores.", 71, 8, 185);
                            InformPlayers(args.Player.Name, args.Parameters[0]);





                        }



                        else
                        {
                            string selore = args.Parameters[2].ToLowerInvariant();
                            List<string> searchlist = StoredLists.ores.Keys.ToList();
                            List<string> findore = TileValidation.FindMatches(selore, searchlist);


                            if (findore.Count > 1)
                            {
                                args.Player.SendErrorMessage($"[[c/FFFFFF:{Name}]] More than one match found: ");

                                lines = PaginationTools.BuildLinesFromTerms(findore.ToArray());
                                lines.ForEach(args.Player.SendInfoMessage);

                            }
                            else if (findore.Count < 1)
                            {
                                args.Player.SendErrorMessage($"[[c/FFFFFF:{Name}]] No matches found for ore '{selore}'");
                                args.Player.SendInfoMessage($"[[c/FFFFFF:{Name}]] Full list of ores :");

                                lines = PaginationTools.BuildLinesFromTerms(StoredLists.ores.Keys.ToArray());
                                lines.ForEach(args.Player.SendInfoMessage);

                            }
                            else
                            {
                                StoredLists.ores.TryGetValue(findore[0], out oreID);






                                await (Regen.AsyncGenerateOres(amount, oreID, oreTiers));



                                //Notify user on success

                                args.Player.SendSuccessMessage($"[[c/FFFFFF:{Name}]] Generated and hid {realcount} {findore[0]} ores.");
                                if (realcount != 0) TSPlayer.All.SendMessage($"[[c/FFFFFF:{Name}]] [c/BCFF00:{args.Player.Name}] has generated and hid [c/BCFF00:{realcount}] {findore[0]} ores.", 71, 8, 185);
                                InformPlayers(args.Player.Name, args.Parameters[0]);





                            }
                        }
                    }
                    else args.Player.SendErrorMessage($"[[c/FFFFFF:{Name}]] Another Generation is in Progress, Please try again later!");
                    break;
                #endregion
                #region Webs
                case "webs":



                    if (!isTaskRunning)
                    {
                        List<ushort> SpiderWalls = new List<ushort>
                        {
                            WallID.Spider,
                            WallID.SpiderEcho,
                            WallID.SpiderUnsafe
                        };

                        await (Regen.AsyncGenerateWebs(amount, SpiderWalls));



                        //Notify user on success

                        args.Player.SendSuccessMessage($"[[c/FFFFFF:{Name}]] Generated and hid {realcount} webs.");
                        if (realcount != 0) TSPlayer.All.SendMessage($"[[c/FFFFFF:{Name}]] [c/BCFF00:{args.Player.Name}] has generated and hid [c/BCFF00:{realcount}] webs.", 71, 8, 185);
                        InformPlayers(args.Player.Name, args.Parameters[0]);

                    }
                    else args.Player.SendErrorMessage($"[[c/FFFFFF:{Name}]] Another Generation is in Progress, Please try again later!");

                    break;



                #endregion
                #region Trees
                case "trees":

                    if (!isTaskRunning)
                    {


                        await (Regen.AsyncGenerateTrees());



                        //Notify user on success

                        args.Player.SendSuccessMessage($"[[c/FFFFFF:{Name}]] Generated trees on the surface!.");
                        TSPlayer.All.SendMessage($"[[c/FFFFFF:{Name}]] [c/BCFF00:{args.Player.Name}] has generated trees on the surface!", 71, 8, 185);
                        InformPlayers(args.Player.Name, args.Parameters[0]);

                    }
                    else args.Player.SendErrorMessage($"[[c/FFFFFF:{Name}]] Another Generation is in Progress, Please try again later!");

                    break;



                #endregion
                #region Shrooms
                case "shrooms":
                    if (!isTaskRunning)
                    {
                        await Regen.AsyncGenerateShrooms(amount);




                        args.Player.SendSuccessMessage($"[[c/FFFFFF:{Name}]] Generated and hid {realcount} mushrooms in their biomes!");
                        if (realcount != 0) TSPlayer.All.SendMessage($"[[c/FFFFFF:{Name}]] [c/BCFF00:{args.Player.Name}] has generated and hid [c/BCFF00:{realcount}] mushrooms in their biomes!", 71, 8, 185);
                        InformPlayers(args.Player.Name, args.Parameters[0]);

                    }
                    else args.Player.SendErrorMessage($"[[c/FFFFFF:{Name}]] Another Generation is in Progress, Please try again later!");

                    break;



                #endregion
                #region Dungeon
                case "dungeon":
                    if (!isTaskRunning)
                    {

                        tryX = args.Player.TileX; //Gets X tile from the right of the character
                        tryY = args.Player.TileY; //Gets Y Tile from the head tile of the character


                        if (TileValidation.inWorld(tryX, tryY) && TileValidation.onSurface(tryX, tryY)) // checks if player is standing on tile and the dungeon is not by the world border
                        {



                            await Regen.AsyncGenerateDungeon(tryX, tryY);



                            //Notify user on success

                            args.Player.SendSuccessMessage($"[[c/FFFFFF:{Name}]] Generated a Dungeon at your location!");
                            TSPlayer.All.SendMessage($"[[c/FFFFFF:{Name}]] [c/BCFF00:{args.Player.Name}] has generated a Dungeon in the world!", 71, 8, 185);
                            InformPlayers(args.Player.Name, args.Parameters[0]);







                        }
                        else
                        {
                            args.Player.SendErrorMessage($"[[c/FFFFFF:{Name}]] Failed to create a Dungeon at this location! You have to be standing on the surface and away from the edges of the world!");

                        }
                    }
                    else args.Player.SendErrorMessage($"[[c/FFFFFF:{Name}]] Another Generation is in Progress, Please try again later!");
                    break;
                #endregion

                /*
                #region Temple
            case "temple":
                TempleBuilder.makeTemple((int)args.Player.X, (int)args.Player.Y);

                InformPlayers(args.Player.Name,args.Parameters[0]);
                break;
            #endregion
                */

                #region Living Tree
                 
                case "livingtree":
                    if (!isTaskRunning)
                    {
                        tryX = args.Player.TileX;
                        tryY = args.Player.TileY;
                        if (TileValidation.inWorld(tryX,tryY) && TileValidation.onSurface(tryX,tryY))
                        {

                            
                            await Regen.AsyncGenerateLivingTree(tryX, tryY);

                            args.Player.SendSuccessMessage($"[[c/FFFFFF:{Name}]] Generated a Living Tree on the surface!");
                            if (realcount != 0) TSPlayer.All.SendMessage($"[[c/FFFFFF:{Name}]] [c/BCFF00:{args.Player.Name}] has Generated a Living Tree on the surface!", 71, 8, 185); 
                            InformPlayers(args.Player.Name, args.Parameters[0]);
                           
                        }
                        else args.Player.SendErrorMessage($"[[c/FFFFFF:{Name}]] Failed to create a Living Tree at this location! You are too close to the world borders and need to be on the surface!");
                    }
                    else args.Player.SendErrorMessage($"[[c/FFFFFF:{Name}]] Another Generation is in Progress, Please try again later!");
                    break;

            #endregion
                
                #region Pyramid
                case "pyramid":
                    if (!isTaskRunning)
                    {
                        tryX = args.Player.TileX;
                        tryY = args.Player.TileY + 5;


                        if (TileValidation.inWorld(tryX, tryY) && TileValidation.onSurface(tryX, tryY))



                            if (await Regen.AsyncGeneratePyramid(tryX, tryY))
                            {
                                args.Player.SendSuccessMessage($"[[c/FFFFFF:{Name}]] A Pyramid was Successfully Generated.");
                                TSPlayer.All.SendMessage($"[[c/FFFFFF:{Name}]] [c/BCFF00:{args.Player.Name}] has generated a Pyramid in the world!", 71, 8, 185);
                                InformPlayers(args.Player.Name, args.Parameters[0]);

                            }
                            else
                            {
                                args.Player.SendErrorMessage($"[[c/FFFFFF:{Name}]] Unexpected Error Occurred, Please contact the administrator!");
                            }

                        else
                        {
                            args.Player.SendErrorMessage($"[[c/FFFFFF:{Name}]] Failed to create a Pyramid at this location! You have to be standing on the surface and away from the edges of the world!");

                        }
                    }
                    else args.Player.SendErrorMessage($"[[c/FFFFFF:{Name}]] Another Generation is in Progress, Please try again later!");


                    //Notify user on success



                    break;
                #endregion
                
                #region Hellevator
                case "hellevator":
                    if (!isTaskRunning)
                    {
                        List<ushort> trees = new List<ushort>
                    {
                        TileID.Trees,
                        TileID.TreeAmber,
                        TileID.TreeAmethyst,
                        TileID.TreeDiamond,
                        TileID.TreeEmerald,
                        TileID.TreeRuby,
                        TileID.TreeSapphire,
                        TileID.TreeTopaz,
                        TileID.MushroomTrees,
                        TileID.PalmTree,
                        TileID.VanityTreeYellowWillow,
                        TileID.VanityTreeSakura
                    };
                        tryX = args.Player.TileX;
                        tryY = args.Player.TileY;

                        if (TileValidation.onSurface(tryX, tryY) && TileValidation.inWorld(tryX, tryY))
                        {

                            await (Regen.AsyncGenerateHellevator(tryX, tryY, trees));
                            
                            args.Player.SendSuccessMessage($"[[c/FFFFFF:{Name}]] A Hellevator was Successfully Generated.");
                            TSPlayer.All.SendMessage($"[[c/FFFFFF:{Name}]] [c/BCFF00:{args.Player.Name}] has generated a Hellevator at X: [c/BCFF00:{tryX}], Y: [c/BCFF00:{tryY}] !", 71, 8, 185);
                            InformPlayers(args.Player.Name, args.Parameters[0]);

                        }

                        else args.Player.SendErrorMessage($"[[c/FFFFFF:{Name}]] Failed to create a Hellevator at this location! You have to be standing on the surface and away from the edges of the world!");
                    }
                    else args.Player.SendErrorMessage($"[[c/FFFFFF:{Name}]] Another Generation is in Progress, Please try again later!");
                    break;
                #endregion
                #region World
                case "world":
                    if (!isTaskRunning)
                    {
                        if (args.Parameters.Count < 2)
                        {
                            args.Player.SendInfoMessage($"[[c/FFFFFF:{Name}]] This will delete the world and replace it with a new one are you sure?");
                            args.Player.SendInfoMessage($"[[c/FFFFFF:{Name}]] To confirm please put {Commands.Specifier}gen world true");
                            args.Player.SendErrorMessage($"[[c/FFFFFF:{Name}]] Invalid Syntax! Use {Commands.Specifier}gen world true [seed]");
                            args.Player.SendErrorMessage($"[[c/FFFFFF:{Name}]] Leave Seed Blank for Same seed as what is currently generated");
                            break;
                        }
                        if (bool.TryParse(args.Parameters[1], out bool confirm))
                        {



                            if (confirm == false)
                            {
                                args.Player.SendInfoMessage($"[[c/FFFFFF:{Name}]] This will delete the world and replace it with a new one are you sure?");
                                args.Player.SendInfoMessage($"[[c/FFFFFF:{Name}]] To confirm please put {Commands.Specifier}gen world true");
                                args.Player.SendErrorMessage($"[[c/FFFFFF:{Name}]] Invalid Syntax! Use {Commands.Specifier}gen world true [seed]");
                                args.Player.SendErrorMessage($"[[c/FFFFFF:{Name}]] Leave Seed Blank for Same seed as what is currently generated");
                            }
                            else
                            {
                                TSPlayer.All.SendInfoMessage($"[[c/FFFFFF:{Name}]] The World is Generating, Please Wait!");
                                if (args.Parameters.Count == 2)
                                {
                                    await Regen.AsyncGenerateWorld(WorldGen._lastSeed);
                                    args.Player.SendSuccessMessage($"[[c/FFFFFF:{Name}]] The World was Successfully Regenerated!");
                                    TSPlayer.All.SendMessage($"[[c/FFFFFF:{Name}]] The World was Successfully Regenerated!", 71, 8, 185);
                                }
                                else
                                {


                                    Main.ActiveWorldFileData.SetSeed(AdvancedRegen.ProcessSeed(args.Parameters[2]));
                                    await Regen.AsyncGenerateWorld(Main.ActiveWorldFileData._seed);
                                    args.Player.SendSuccessMessage($"[[c/FFFFFF:{Name}]] The World was Successfully Regenerated with Seed {args.Parameters[2]}");
                                    TSPlayer.All.SendMessage($"[[c/FFFFFF:{Name}]] The World was Successfully Regenerated", 71, 8, 185);

                                }






                                foreach (TSPlayer plr in TShock.Players)
                                {
                                    if (plr != null && Main.tile[plr.TileX, plr.TileY].active())
                                    {
                                        plr.Teleport(Main.spawnTileX * 16, (Main.spawnTileY * 16) - 48);
                                    }

                                }
                                InformPlayers(args.Player.Name, args.Parameters[0]);




                            }
                        }
                        else
                        {
                            args.Player.SendInfoMessage($"[[c/FFFFFF:{Name}]] This will delete the world and replace it with a new one are you sure?");
                            args.Player.SendInfoMessage($"[[c/FFFFFF:{Name}]] To confirm please put {Commands.Specifier}gen world true");
                            args.Player.SendErrorMessage($"[[c/FFFFFF:{Name}]] Invalid Syntax! Use {Commands.Specifier}gen world true [seed]");
                            args.Player.SendErrorMessage($"[[c/FFFFFF:{Name}]] Leave Seed Blank for Same seed as what is currently generated");
                        }
                    }
                    else args.Player.SendErrorMessage($"[[c/FFFFFF:{Name}]] Another Generation is in Progress, Please try again later!");


                    break;
                #endregion
                #region Floating Island

                case "island":
                    if (!isTaskRunning)
                    {

                        tryX = args.Player.TileX;
                        tryY = args.Player.TileY;



                        if (args.Parameters.Count == 2)
                        {



                            string selisland = args.Parameters[1].ToLowerInvariant();
                            List<string> islands = StoredLists.IslandList.Keys.ToList();
                            List<string> foundisland = TileValidation.FindMatches(selisland, islands);

                            if (foundisland.Count > 1)
                            {
                                args.Player.SendErrorMessage($"[[c/FFFFFF:{Name}]] More than one match found: ");

                                lines = PaginationTools.BuildLinesFromTerms(foundisland.ToArray());
                                lines.ForEach(args.Player.SendInfoMessage);
                            }
                            else if (foundisland.Count < 1)
                            {
                                args.Player.SendErrorMessage($"[[c/FFFFFF:{Name}]] No matches found for island '{selisland}'");
                                args.Player.SendInfoMessage($"[[c/FFFFFF:{Name}]] Full list of islands :");

                                lines = PaginationTools.BuildLinesFromTerms(StoredLists.IslandList.Keys.ToArray());
                                lines.ForEach(args.Player.SendInfoMessage);
                            }
                            else
                            {
                                if (TileValidation.islandTileValidation(tryX, tryY))
                                {
                                    StoredLists.IslandList.TryGetValue(foundisland[0], out int island);


                                    await Regen.AsyncGenerateIsland(island, tryX, tryY);
                                    
                                    args.Player.SendSuccessMessage($"[[c/FFFFFF:{Name}]] Generated and hid a {foundisland[0]} island.");
                                    TSPlayer.All.SendMessage($"[[c/FFFFFF:{Name}]] [c/BCFF00:{args.Player.Name}] has generated and hid {foundisland[0]} island.", 71, 8, 185);
                                    InformPlayers(args.Player.Name, args.Parameters[0]);
                                }
                                else args.Player.SendErrorMessage($"[[c/FFFFFF:{Name}]] Failed to create an Island at this location! You have to be in the sky! or you are too close to the height border!");


                            }

                        }
                        else
                        {
                            args.Player.SendErrorMessage($"[[c/FFFFFF:{Name}]] Invalid Syntax! Use {Commands.Specifier}gen island <Specified island>");
                            args.Player.SendInfoMessage($"[[c/FFFFFF:{Name}]] Full list of islands :");

                            lines = PaginationTools.BuildLinesFromTerms(StoredLists.IslandList.Keys.ToArray());
                            lines.ForEach(args.Player.SendInfoMessage);
                        }

                    }
                    else args.Player.SendErrorMessage($"[[c/FFFFFF:{Name}]] Another Generation is in Progress, Please try again later!");
                    break;

                #endregion
                #region Chests
                case "chests": // Will get back to this at some point to allow for custom regen? However i want to work on something else so i rushed this.
                    if (!isTaskRunning)
                    {
                        await Regen.AsyncGenerateChests(amount);

                        args.Player.SendSuccessMessage($"[[c/FFFFFF:{Name}]] Generated and hid {realcount} Chests");
                        if (realcount != 0) TSPlayer.All.SendMessage($"[[c/FFFFFF:{Name}]] [c/BCFF00:{args.Player.Name}] has generated and hid [c/BCFF00:{realcount}] Chests", 71, 8, 185);
                        InformPlayers(args.Player.Name, args.Parameters[0]);
                    }

                    else args.Player.SendErrorMessage($"[[c/FFFFFF:{Name}]] Another Generation is in Progress, Please try again later!");
                    break;


                #endregion
                #region Hive
                case "hive":
                    if (!isTaskRunning)
                    {
                        tryX = args.Player.TileX;
                        tryY = args.Player.TileY;
                        if (TileValidation.inWorld(tryX, tryY))
                        {

                            await Regen.AsyncGenerateHive(tryX, tryY);


                            args.Player.SendSuccessMessage($"[[c/FFFFFF:{Name}]] A Hive was Successfully Generated.");
                            TSPlayer.All.SendMessage($"[[c/FFFFFF:{Name}]] [c/BCFF00:{args.Player.Name}] has Generated a Hive biome in the world!", 71, 8, 185);
                            InformPlayers(args.Player.Name, args.Parameters[0]);
                        }
                        else args.Player.SendErrorMessage($"[[c/FFFFFF:{Name}]] Failed to create a Hive at this location! You are too close to the world borders!");
                    }
                    else args.Player.SendErrorMessage($"[[c/FFFFFF:{Name}]] Another Generation is in Progress, Please try again later!");
                    
                    break;
                #endregion
                #region CaveHouse
                case "cavehouse":
                    if (!isTaskRunning)
                    {
                        tryX = args.Player.TileX;
                        tryY = args.Player.TileY;
                        if (TileValidation.inWorld(tryX, tryY) && tryY >= Main.rockLayer)
                        {

                            await Regen.AsyncGenerateHouse(tryX, tryY);

                            args.Player.SendSuccessMessage($"[[c/FFFFFF:{Name}]] A Cavehouse was Successfully Generated.");
                            TSPlayer.All.SendMessage($"[[c/FFFFFF:{Name}]] [c/BCFF00:{args.Player.Name}] has Generated a Cavehouse in the world!", 71, 8, 185);
                            InformPlayers(args.Player.Name, args.Parameters[0]);
                        }
                        else args.Player.SendErrorMessage($"[[c/FFFFFF:{Name}]] Failed to create a Cavehouse in this location! You have to be away from the world borders and in the cavern layer to generate this structure!");
                    }
                    else args.Player.SendErrorMessage($"[[c/FFFFFF:{Name}]] Another Generation is in Progress, Please try again later!");
                    break;
                #endregion
                case "granite":
                    if (!isTaskRunning)
                    {
                        tryX = args.Player.TileX;
                        tryY = args.Player.TileY;
                        if (TileValidation.inWorld(tryX, tryY) && tryY >= Main.rockLayer )
                        {
                            await Regen.AsyncGenerateGranite(tryX, tryY);

                            args.Player.SendSuccessMessage($"[[c/FFFFFF:{Name}]] A Granite Biome was Successfully Generated.");
                            TSPlayer.All.SendMessage($"[[c/FFFFFF:{Name}]] [c/BCFF00:{args.Player.Name}] has Generated a Granite Biome in the world!", 71, 8, 185);
                            InformPlayers(args.Player.Name, args.Parameters[0]);
                        }
                        else args.Player.SendErrorMessage($"[[c/FFFFFF:{Name}]] Failed to create a Granite Biome in this location! You have to be away from the world borders and in the cavern layer to generate this biome!");
                    }
                    else args.Player.SendErrorMessage($"[[c/FFFFFF:{Name}]] Another Generation is in Progress, Please try again later!");
                    break;
                case "marble":
                    if (!isTaskRunning)
                    {
                        tryX = args.Player.TileX;
                        tryY = args.Player.TileY;
                        if (TileValidation.inWorld(tryX, tryY) && tryY >= Main.rockLayer)
                        {
                            await Regen.AsyncGenerateMarble(tryX, tryY);

                            args.Player.SendSuccessMessage($"[[c/FFFFFF:{Name}]] A Marble Biome was Successfully Generated.");
                            TSPlayer.All.SendMessage($"[[c/FFFFFF:{Name}]] [c/BCFF00:{args.Player.Name}] has Generated a Marble Biome in the world!", 71, 8, 185);
                            InformPlayers(args.Player.Name, args.Parameters[0]);
                        }
                        else args.Player.SendErrorMessage($"[[c/FFFFFF:{Name}]] Failed to create a Marble Biome in this location! You have to be away from the world borders and in the cavern layer to generate this biome!");
                    }
                    else args.Player.SendErrorMessage($"[[c/FFFFFF:{Name}]] Another Generation is in Progress, Please try again later!");
                    break;
                case "terragrim":
                    if(!isTaskRunning)
                    {
                        tryX = args.Player.TileX;
                        tryY = args.Player.TileY;
                        if (TileValidation.inWorld(tryX, tryY) && TileValidation.onSurface(tryX,tryY))
                        {
                            await Regen.AsyncGenerateSword(tryX, WorldGen.genRand.Next(tryY + 40, tryY + 100));

                            args.Player.SendSuccessMessage($"[[c/FFFFFF:{Name}]] A Terragrim Biome was Successfully Generated underneath You!");
                            TSPlayer.All.SendMessage($"[[c/FFFFFF:{Name}]] [c/BCFF00:{args.Player.Name}] has Generated a Terragrim Biome in the world!", 71, 8, 185);
                            InformPlayers(args.Player.Name, args.Parameters[0]);
                        }
                        else args.Player.SendErrorMessage($"[[c/FFFFFF:{Name}]] Failed to create a Terragrim Biome in this location! You have to be away from the world borders and in the cavern layer to generate this biome!");
                    }
                    else args.Player.SendErrorMessage($"[[c/FFFFFF:{Name}]] Another Generation is in Progress, Please try again later!");
                    break;











                #region Default case
                default:
                    args.Player.SendErrorMessage($"[[c/FFFFFF:{Name}]] Invalid Syntax! Please refer to the following options...");
                    PrintOptions(args);

                    break;

                    #endregion
            }


        }

        #endregion








        #endregion


        #region Utils
        #region InformPlayers
        //Updating all players Reloads Tile Sections
        public static void InformPlayers(string player, string generator, bool hard = false)
        {
            File.AppendAllText(Path.Combine(ConfigFunctions.savepath, "WRLog.txt"), $"[{DateTime.UtcNow}] / [WR Success] User {player} has Generated {realcount} {generator} on World {Main.worldID}, Seed {WorldGen._lastSeed}\n");
            foreach (TSPlayer person in TShock.Players)
            {
                if ((person != null) && (person.Active))
                {
                    for (int i = 0; i < 255; i++)
                    {
                        for (int j = 0; j < Main.maxSectionsX; j++)
                        {
                            for (int k = 0; k < Main.maxSectionsY; k++)
                            {
                                Netplay.Clients[i].TileSections[j, k] = false;
                            }
                        }
                    }
                }
            }

        }
        #endregion
        #region IsProtected
        public static bool IsProtected(int x, int y)
        {
            var regions = TShock.Regions.InAreaRegion(x, y);
            if (regions.Count() > 0)
            {
                return true;
            }
            else return false;
        }

        #endregion


        private List<int> ItemList
        {
            get
            {
                List<int> list = new List<int>();
                var items = typeof(Terraria.ID.ItemID).GetFields();
                for (int i = 0; i < items.Length; i++)
                {
                    list.Add((int)items[i].GetValue(items[i]));
                }
                list.Sort();
                return list;
            }
        }
        #endregion
    }
}
