using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using MCForge.SQL;
using System.Data;

namespace MCForge {
    public static class Economy {

        public const string createTable =
            @"CREATE TABLE if not exists Economy (
	            player 	    VARCHAR(20),
	            money       INT UNSIGNED,
                total       INT UNSIGNED NOT NULL DEFAULT 0,
                purchase    VARCHAR(255) NOT NULL DEFAULT '%cNone',
                payment     VARCHAR(255) NOT NULL DEFAULT '%cNone',
                salary      VARCHAR(255) NOT NULL DEFAULT '%cNone',
                fine        VARCHAR(255) NOT NULL DEFAULT '%cNone',
	            PRIMARY KEY(player)
            );";

        public struct EcoStats {
            public string playerName, purchase, payment, salary, fine;
            public int money, totalSpent;
            public EcoStats(string name, int mon, int tot, string pur, string pay, string sal, string fin) {
                playerName = name;
                money = mon;
                totalSpent = tot;
                purchase = pur;
                payment = pay;
                salary = sal;
                fine = fin;
            }
        }

        public static class Settings {
            public static bool Enabled = false;

            //Maps
            public static bool Levels = false;
            public static List<Level> LevelsList = new List<Level>();
            public class Level {
                public int price;
                public string name;
                public string x;
                public string y;
                public string z;
                public string type;
                public bool private_; //coming soon
            }

            //Titles
            public static bool Titles = false;
            public static int TitlePrice = 100;

            //Colors
            public static bool Colors = false;
            public static int ColorPrice = 100;

            //TitleColors
            public static bool TColors = false;
            public static int TColorPrice = 100;

            //Ranks
            public static bool Ranks = false;
            public static string MaxRank = Group.findPerm(LevelPermission.AdvBuilder).trueName;
            public static List<Rank> RanksList = new List<Rank>();
            public class Rank {
                public Group group;
                public int price = 1000;
            }
        }

        public static void Load(bool serverLoading = false) {
            if (serverLoading) {
            retry:
                if (Server.useMySQL) MySQL.executeQuery(createTable); else SQLite.executeQuery(createTable); //create database on server loading
                string queryP = "SELECT * FROM Players"; string queryE = "SELECT * FROM Economy";
                DataTable eco = Server.useMySQL ? MySQL.fillData(queryE) : SQLite.fillData(queryE);
                try {
                    DataTable players = Server.useMySQL ? MySQL.fillData(queryP) : SQLite.fillData(queryP);
                    if (players.Rows.Count == eco.Rows.Count) { } //move along, nothing to do here
                    else if (players.Rows.Count != eco.Rows.Count && eco.Rows.Count == 0) { //if first time, copy content from player to economy
                        string query = "INSERT Economy (player, money) SELECT Players.Name, Players.Money FROM Players";
                        if (Server.useMySQL) MySQL.executeQuery(query); else SQLite.executeQuery(query);
                    } else {
                        //this will only be needed when the server shuts down while it was copying content (or some other error)
                        if (Server.useMySQL) MySQL.executeQuery("DROP TABLE Economy"); else SQLite.executeQuery("DROP TABLE Economy");
                        goto retry;
                    }
                    players.Dispose(); eco.Dispose();
                } catch { }
            }

            if (!File.Exists("properties/economy.properties")) { Server.s.Log("Economy properties don't exist, creating"); File.Create("properties/economy.properties").Close(); Save(); }
            using (StreamReader r = File.OpenText("properties/economy.properties")) {
                string line;
                while (!r.EndOfStream) {
                    line = r.ReadLine().ToLower().Trim();
                    string[] linear = line.ToLower().Trim().Split(':');
                    try {
                        switch (linear[0]) {
                            case "enabled":
                                if (linear[1] == "true") { Settings.Enabled = true; } else if (linear[1] == "false") { Settings.Enabled = false; }
                                break;

                            case "title":
                                if (linear[1] == "price") { Settings.TitlePrice = int.Parse(linear[2]); }
                                if (linear[1] == "enabled") {
                                    if (linear[2] == "true") { Settings.Titles = true; } else if (linear[2] == "false") { Settings.Titles = false; }
                                }
                                break;

                            case "color":
                                if (linear[1] == "price") { Settings.ColorPrice = int.Parse(linear[2]); }
                                if (linear[1] == "enabled") {
                                    if (linear[2] == "true") { Settings.Colors = true; } else if (linear[2] == "false") { Settings.Colors = false; }
                                }
                                break;
                            case "tcolor":
                                if (linear[1] == "price") { Settings.TColorPrice = int.Parse(linear[2]); }
                                if (linear[1] == "enabled") {
                                    if (linear[2] == "true") { Settings.TColors = true; } else if (linear[2] == "false") { Settings.TColors = false; }
                                }
                                break;
                            case "rank":
                                if (linear[1] == "price") {
                                    Economy.Settings.Rank rnk = new Economy.Settings.Rank();
                                    rnk = Economy.FindRank(linear[2]);
                                    if (rnk == null) {
                                        rnk = new Economy.Settings.Rank();
                                        rnk.group = Group.Find(linear[2]);
                                        rnk.price = int.Parse(linear[3]);
                                        Economy.Settings.RanksList.Add(rnk);
                                    } else {
                                        Economy.Settings.RanksList.Remove(rnk);
                                        rnk.price = int.Parse(linear[3]);
                                        Economy.Settings.RanksList.Add(rnk);
                                    }
                                }
                                if (linear[1] == "maxrank") {
                                    //Group grp = Group.Find(linear[2]);
                                    //if (grp != null) { Settings.MaxRank = grp.Permission; }
                                    string grpname = linear[2];
                                    if (Group.Exists(grpname)) Settings.MaxRank = grpname;
                                }
                                if (linear[1] == "enabled") {
                                    if (linear[2] == "true") { Settings.Ranks = true; } else if (linear[2] == "false") { Settings.Ranks = false; }
                                }
                                break;

                            case "level":
                                if (linear[1] == "enabled") {
                                    if (linear[2] == "true") { Settings.Levels = true; } else if (linear[2] == "false") { Settings.Levels = false; }
                                }
                                if (linear[1] == "levels") {
                                    Settings.Level lvl = new Settings.Level();
                                    if (FindLevel(linear[2]) != null) { lvl = FindLevel(linear[2]); Settings.LevelsList.Remove(lvl); }
                                    switch (linear[3]) {
                                        case "name":
                                            lvl.name = linear[4];
                                            break;

                                        case "price":
                                            lvl.price = int.Parse(linear[4]);
                                            break;

                                        case "x":
                                            lvl.x = linear[4];
                                            break;

                                        case "y":
                                            lvl.y = linear[4];
                                            break;

                                        case "z":
                                            lvl.z = linear[4];
                                            break;

                                        case "type":
                                            lvl.type = linear[4];
                                            break;
                                    }
                                    Settings.LevelsList.Add(lvl);
                                }
                                break;
                        }
                    } catch { }
                }
                r.Close();
            }
            Save();
        }

        public static void Save() {
            if (!File.Exists("properties/economy.properties")) { Server.s.Log("Economy properties don't exist, creating"); }
            //Thread.Sleep(2000);
            File.Delete("properties/economy.properties");
            //Thread.Sleep(2000);
            using (StreamWriter w = File.CreateText("properties/economy.properties")) {
                //enabled
                w.WriteLine("enabled:" + Settings.Enabled);
                //title
                w.WriteLine();
                w.WriteLine("title:enabled:" + Settings.Titles);
                w.WriteLine("title:price:" + Settings.TitlePrice);
                //color
                w.WriteLine();
                w.WriteLine("color:enabled:" + Settings.Colors);
                w.WriteLine("color:price:" + Settings.ColorPrice);
                //tcolor
                w.WriteLine();
                w.WriteLine("titlecolor:enabled:" + Settings.TColors);
                w.WriteLine("titlecolor:price" + Settings.TColorPrice);
                //rank
                w.WriteLine();
                w.WriteLine("rank:enabled:" + Settings.Ranks);
                w.WriteLine("rank:maxrank:" + Settings.MaxRank);
                foreach (Settings.Rank rnk in Settings.RanksList) {
                    w.WriteLine("rank:price:" + rnk.group.name + ":" + rnk.price);
                }
                //maps
                w.WriteLine();
                w.WriteLine("level:enabled:" + Settings.Levels);
                foreach (Settings.Level lvl in Settings.LevelsList) {
                    w.WriteLine();
                    w.WriteLine("level:levels:" + lvl.name + ":name:" + lvl.name);
                    w.WriteLine("level:levels:" + lvl.name + ":price:" + lvl.price);
                    w.WriteLine("level:levels:" + lvl.name + ":x:" + lvl.x);
                    w.WriteLine("level:levels:" + lvl.name + ":y:" + lvl.y);
                    w.WriteLine("level:levels:" + lvl.name + ":z:" + lvl.z);
                    w.WriteLine("level:levels:" + lvl.name + ":type:" + lvl.type);
                    //w.WriteLine("level:levels:" + lvl.name + ":private:" + lvl.private_); //coming soon
                }
                w.Close();
            }
        }

        public static Settings.Level FindLevel(string name) {
            Settings.Level found = null;
            foreach (Settings.Level lvl in Settings.LevelsList) {
                try {
                    if (lvl.name.ToLower() == name.ToLower()) {
                        found = lvl;
                    }
                } catch { }
            }
            return found;
        }

        public static Settings.Rank FindRank(string name) {
            Settings.Rank found = null;
            foreach (Settings.Rank rnk in Settings.RanksList) {
                try {
                    if (rnk.group.name.ToLower() == name.ToLower()) {
                        found = rnk;
                    }
                } catch { }
            }
            return found;
        }

        public static Economy.Settings.Rank NextRank(Player p) {
            Group foundGroup = p.group;
            Group nextGroup = null; bool nextOne = false;
            for (int i = 0; i < Group.GroupList.Count; i++) {
                Group grp = Group.GroupList[i];
                if (nextOne) {
                    if (grp.Permission >= LevelPermission.Nobody) break;
                    nextGroup = grp;
                    break;
                }
                if (grp == foundGroup)
                    nextOne = true;
            }
            return Economy.FindRank(nextGroup.name);
        }

        public static EcoStats RetrieveEcoStats(string playername) {
            EcoStats es;
            es.playerName = playername;
            string query = "SELECT * FROM Economy WHERE player = '" + playername + "'";
            using (DataTable eco = Server.useMySQL ? MySQL.fillData(query) : SQLite.fillData(query)) {
                if (eco.Rows.Count == 1) {
                    es.money = int.Parse(eco.Rows[0]["money"].ToString());
                    es.totalSpent = int.Parse(eco.Rows[0]["total"].ToString());
                    es.purchase = eco.Rows[0]["purchase"].ToString();
                    es.payment = eco.Rows[0]["payment"].ToString();
                    es.salary = eco.Rows[0]["salary"].ToString();
                    es.fine = eco.Rows[0]["fine"].ToString();
                } else {
                    es.money = 0;
                    es.totalSpent = 0;
                    es.purchase = "%cNone";
                    es.payment = "%cNone";
                    es.salary = "%cNone";
                    es.fine = "%cNone";
                }
            }
            return es;
        }

        public static void UpdateEcoStats(EcoStats es) {
            string query1 = String.Format("REPLACE INTO Economy (player, money, total, purchase, payment, salary, fine) VALUES ('{0}', {1}, {2}, '{3}', '{4}', '{5}', '{6}')", es.playerName, es.money, es.totalSpent, es.purchase, es.payment, es.salary, es.fine);
            string query2 = String.Format("INSERT OR REPLACE INTO Economy (player, money, total, purchase, payment, salary, fine) VALUES ('{0}', {1}, {2}, '{3}', '{4}', '{5}', '{6}')", es.playerName, es.money, es.totalSpent, es.purchase, es.payment, es.salary, es.fine);
            if (Server.useMySQL) MySQL.executeQuery(query1); else SQLite.executeQuery(query2);
        }
    }
}