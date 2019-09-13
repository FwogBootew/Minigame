using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using Random = System.Random;
//goodnight gamers
namespace Oxide.Plugins
{
    [Info("Minigame", "Fwog Bootew", 1.0)]
    [Description("A Minigame plugin for Rust.")]
    public class Minigame : RustPlugin
    {
        #region General

        #region Structs

        public struct Kit
        {
            Item[] attire;
            Item[] meds;
            Item[] weapons;
            Item[] misc;
            public string kitName;
            int stackDivision;
            public Kit(Item[] attire, Item[] meds, Item[] weapons, Item[] misc, string kitName, int stackDivision = 1)
            {
                this.attire = attire;
                this.meds = meds;
                this.weapons = weapons;
                this.misc = misc;
                this.kitName = kitName;
                this.stackDivision = stackDivision;
            }
            public void givePlayerKit(BasePlayer player)
            {
                foreach (Item item in attire)
                {
                    if (item != null)
                    {
                        ItemManager.CreateByItemID(item.info.itemid, item.amount).MoveToContainer(player.inventory.containerWear);
                    }
                }
                foreach (Item item in weapons)
                {

                    if (item != null)
                    {
                        Item weapon = ItemManager.CreateByItemID(item.info.itemid, item.amount);
                        (weapon.GetHeldEntity() as BaseProjectile).primaryMagazine.contents = (weapon.GetHeldEntity() as BaseProjectile).primaryMagazine.capacity;
                        weapon.MoveToContainer(player.inventory.containerBelt);
                        player.GiveItem(ItemManager.CreateByItemID((weapon.GetHeldEntity() as BaseProjectile).primaryMagazine.ammoType.itemid, ItemManager.CreateByItemID((weapon.GetHeldEntity() as BaseProjectile).primaryMagazine.ammoType.itemid).MaxStackable() / stackDivision));
                    }

                }
                foreach (Item item in misc)
                {
                    if (item != null)
                    {
                        player.GiveItem(ItemManager.CreateByItemID(item.info.itemid, item.amount));
                    }
                }
                foreach (Item item in meds)
                {
                    if (item != null)
                    {
                        player.GiveItem(ItemManager.CreateByItemID(item.info.itemid, item.amount));
                    }
                }
            }
        }

        #endregion

        #region Classes
        public class Minigamer
        {
            //General
            public static List<BasePlayer> players = new List<BasePlayer>();
            public BasePlayer player;
            public Game game = null;
            //PvP
            //public Kit kit;
            public int Class;
            //Survival
            public Minigamer(BasePlayer player, Game game)
            {
                Class = 0;
                this.game = game;
                this.player = player;
                players.Add(player);
            }
            ~Minigamer()
            {
                players.Remove(player);
            }
            public bool isInGame()
            {
                if (game != null) return true;
                return false;
            }
            public void leaveGame()
            {
                game.playerLeaveGame(player);
                game = null;
            }
            public void joinGame(Game game)
            {
                this.game = game;
                game.playerJoinGame(player);
            }
            public BasePlayer.SpawnPoint getSpawn()
            {
                Test();
                return game.getPlayerSpawn();
            }
        }

        public class Game
        {
            public string GameName;
            public List<BasePlayer> players = new List<BasePlayer>();
            virtual public Vector3 getSpawn()
            {
                return new Vector3();
            }
            virtual public void OnPlayerRespawn(BasePlayer player)
            {

            }
            virtual public void OnPlayerDie(BasePlayer player)
            {

            }
            virtual public BasePlayer.SpawnPoint getPlayerSpawn()
            {
                return new BasePlayer.SpawnPoint() { pos = new Vector3(), rot = new Quaternion() };
            }
            virtual public void playerLeaveGame(BasePlayer player)
            {
                players.Remove(player);
                broadcastToPlayers(string.Format(Lang["PlayerLeft"], player.displayName));
            }
            virtual public void playerJoinGame(BasePlayer player)
            {
                players.Add(player);
                broadcastToPlayers(string.Format(Lang["PlayerJoined"], player.displayName));
            }
            virtual public void OnTriggerEnter(TriggerBase trigger, BasePlayer player)
            {

            }
            virtual public void OnTriggerExit(TriggerBase trigger, BasePlayer player)
            {

            }
            public void broadcastToPlayers(string msg)
            {
                foreach(var player in players)
                {
                    player.ChatMessage(msg);
                }
            }
        }

        public class HubGame : Game
        {
            static Kit kit = kits[3][0];
            private Vector3 hubSpawn = new Vector3(42.6f, 45.0f, -232.7f);
            private TriggerTemperature trigger;
            public HubGame(Vector3 hubSpawn)
            {
                this.hubSpawn = hubSpawn;
                trigger = new TriggerTemperature() {triggerSize = 10.0f, enabled = true};
                trigger.transform.position = hubSpawn;
                GameName = "Hub";
                players = new List<BasePlayer>();
            }
            public override void playerLeaveGame(BasePlayer player)
            {
                players.Remove(player);
                if (isDebug)
                {
                    foreach (var person in BasePlayer.activePlayerList)
                    {
                        if (person.IsAdmin) person.ChatMessage("Player left hub");
                    }
                }
            }
            public override void playerJoinGame(BasePlayer player)
            {
                players.Add(player);
                player.inventory.Strip();
                player.Heal(100.0f);
                player.Teleport(hubSpawn);
                if (isDebug)
                {
                    foreach (var person in BasePlayer.activePlayerList)
                    {
                        if (person.IsAdmin) person.ChatMessage("Player joined hub");
                    }
                }
            }
            public override void OnPlayerRespawn(BasePlayer player)
            {
                player.inventory.Strip();
                player.Heal(100.0f - player.health);
            }
            public override BasePlayer.SpawnPoint getPlayerSpawn()
            {
                //Test();
                return new BasePlayer.SpawnPoint() { pos = hubSpawn, rot = new Quaternion()};
            }
            public override void OnTriggerEnter(TriggerBase trigger, BasePlayer player)
            {
                if (this.trigger = trigger as TriggerTemperature)
                {
                    kit.givePlayerKit(player);
                }
            }
            public override void OnTriggerExit(TriggerBase trigger, BasePlayer player)
            {
                if (this.trigger = trigger as TriggerTemperature)
                {
                    player.inventory.Strip();
                    player.Heal(100.0f);
                }
            }
        }

        public class PvPGame : Game
        {
            Vector3[] spawns;
            public PvPGame(Vector3[] spawns)
            {
                this.spawns = spawns;
                GameName = "PvP";
                players = new List<BasePlayer>();
            }
            public override void OnPlayerRespawn(BasePlayer player)
            {
                player.inventory.Strip();
                player.Heal(100.0f - player.health);
                kits[2][getMinigamer(player).Class].givePlayerKit(player);
            }
            public override BasePlayer.SpawnPoint getPlayerSpawn()
            {
                Random rand = new Random();
                return new BasePlayer.SpawnPoint() { pos = spawns[rand.Next(spawns.Length)], rot = new Quaternion() };
            }
            private Vector3 GeneratePlayerSpawn()
            {
                Random rand = new Random();
                return spawns[rand.Next(spawns.Length)];
            }
            public override void playerJoinGame(BasePlayer player)
            {
                players.Add(player);
                broadcastToPlayers(string.Format(Lang["PlayerJoined"], player.displayName));
                player.Teleport(GeneratePlayerSpawn());
                player.inventory.Strip();
                player.Heal(100.0f - player.health);
                kits[2][getMinigamer(player).Class].givePlayerKit(player);
            }
        }
        /**/

        public class SurvivalGame : Game
        {
            public SurvivalGame()
            {
                GameName = "Survival";
                players = new List<BasePlayer>();
            }
        }

        public class redeem
        {
            public string name = "";
            public int requirement;
            public redeem()
            {

            }
            public virtual void givePlayerRedeem(BasePlayer player)
            {

            }
        }

        public class redeemM249 : redeem
        {
            public redeemM249()
            {
                name = "m249";
                requirement = 10;
            }
            public override void givePlayerRedeem(BasePlayer player)
            {
                Item item = ItemManager.CreateByName("lmg.m249", 1, 1712378771);
                (item.GetHeldEntity() as BaseProjectile).primaryMagazine.contents = (item.GetHeldEntity() as BaseProjectile).primaryMagazine.capacity;
                player.GiveItem(item);
            }
        }

        public class redeemL96 : redeem
        {
            public redeemL96()
            {
                name = "l96";
                requirement = 5;
            }
            public override void givePlayerRedeem(BasePlayer player)
            {
                //weapon.mod.8x.scope
                Item item = ItemManager.CreateByName("rifle.l96", 1);
                (item.GetHeldEntity() as BaseProjectile).primaryMagazine.contents = (item.GetHeldEntity() as BaseProjectile).primaryMagazine.capacity;
                //item.GetHeldEntity().GiveItem(ItemManager.CreateByName("weapon.mod.8x.scope", 1));
                player.GiveItem(item);
                player.GiveItem(ItemManager.CreateByName("weapon.mod.8x.scope", 1));
            }
        }

        public class redeemLauncher : redeem
        {
            public redeemLauncher()
            {
                name = "launcher";
                requirement = 7;
            }
            public override void givePlayerRedeem(BasePlayer player)
            {
                //weapon.mod.8x.scope
                Item item = ItemManager.CreateByName("rocket.launcher", 1);
                (item.GetHeldEntity() as BaseProjectile).primaryMagazine.contents = (item.GetHeldEntity() as BaseProjectile).primaryMagazine.capacity;
                player.GiveItem(item);
                player.GiveItem(ItemManager.CreateByName("ammo.rocket.basic", 3));
            }
        }

        public class redeemGloves : redeem
        {
            public redeemGloves()
            {
                name = "gloves";
                requirement = 3;
            }
            public override void givePlayerRedeem(BasePlayer player)
            {
                player.GiveItem(ItemManager.CreateByName("tactical.gloves", 1));
            }
        }

        #endregion

        #region Variables

        private static List<Minigamer> Minigamers = new List<Minigamer>();
        List<Game> Games = new List<Game>
        {
            new HubGame(new Vector3(42.6f, 45.0f, -232.7f)),
            new PvPGame(new Vector3[]
            {
                new Vector3(-196.5f, 40.0f, -57.0f),
                new Vector3(-226.2f, 40.0f, -67.4f),
                new Vector3(-191.6f, 40.1f, -123.1f),
                new Vector3(-187.3f, 40.0f, -89.9f),
                new Vector3(-219.3f, 40.0f, -122.4f),
                new Vector3(-231.7f, 40.3f, -105.9f),
                new Vector3(-186.6f, 40.3f, -74.3f),
            }),
            new SurvivalGame()
        };

        TriggerTemperature trigger;

        static bool isDebug = false;

        public static Kit[][] kits = new Kit[][]
{
            //Level-1
            new Kit[]
        {
            new Kit
            (
                new Item[] { /*ItemManager.CreateByName("", 1)*/ },
                new Item[] { ItemManager.CreateByName("bandage", 3) },
                new Item[] { /*ItemManager.CreateByName("", 1)*/ },
                new Item[] { ItemManager.CreateByName("spear.stone", 1), ItemManager.CreateByName("spear.stone", 1), ItemManager.CreateByName("spear.stone", 1) },
                "Soldier"
            ),
            new Kit
            (
                new Item[] { /*ItemManager.CreateByName("", 1)*/ },
                new Item[] { ItemManager.CreateByName("bandage", 3) },
                new Item[] { /*ItemManager.CreateByName("", 1)*/ },
                new Item[] { /*ItemManager.CreateByName("", 1)*/  },
                "CQB",
                4
            ),
            new Kit
            (
                new Item[] { /*ItemManager.CreateByName("", 1)*/ },
                new Item[] { ItemManager.CreateByName("bandage", 3) },
                new Item[] { ItemManager.CreateByName("pistol.nailgun", 1) },
                new Item[] { /*ItemManager.CreateByName("", 1)*/ },
                "Rouge"
            ),
            new Kit
            (
                new Item[] { /*ItemManager.CreateByName("", 1)*/ },
                new Item[] { ItemManager.CreateByName("bandage", 3) },
                new Item[] { ItemManager.CreateByName("bow.hunting", 1) },
                new Item[] { /*ItemManager.CreateByName("", 1)*/ },
                "Scout",
                4
            )
        },
            //Level-2
            new Kit[]
        {
            new Kit
            (
                new Item[] { ItemManager.CreateByName("bone.armor.suit", 1) },
                new Item[] { ItemManager.CreateByName("bandage", 3) },
                new Item[] { ItemManager.CreateByName("bow.hunting", 1) },
                new Item[] { /*ItemManager.CreateByName("", 1)*/ },
                "Soldier"
            ),
            new Kit
            (
                new Item[] { ItemManager.CreateByName("bone.armor.suit", 1) },
                new Item[] { ItemManager.CreateByName("bandage", 3) },
                new Item[] { ItemManager.CreateByName("shotgun.waterpipe", 1) },
                new Item[] { /*ItemManager.CreateByName("", 1)*/ },
                "CQB",
                4
            ),
            new Kit
            (
                new Item[] { /*ItemManager.CreateByName("", 1)*/ },
                new Item[] { ItemManager.CreateByName("bandage", 3) },
                new Item[] { ItemManager.CreateByName("pistol.eoka", 1) },
                new Item[] { ItemManager.CreateByName("knife.combat", 1) },
                "Rouge",
                4
            ),
            new Kit
            (
                new Item[] { /*ItemManager.CreateByName("", 1)*/ },
                new Item[] { ItemManager.CreateByName("bandage", 3) },
                new Item[] { ItemManager.CreateByName("crossbow", 1) },
                new Item[] { /*ItemManager.CreateByName("", 1)*/ },
                "Scout",
                4
            )
        },
            //Level-3
            new Kit[]
        {
            new Kit
            (
                new Item[] { ItemManager.CreateByName("roadsign.jacket", 1), ItemManager.CreateByName("coffeecan.helmet", 1), ItemManager.CreateByName("roadsign.kilt", 1), ItemManager.CreateByName("roadsign.gloves", 1), ItemManager.CreateByName("shoes.boots", 1), ItemManager.CreateByName("hoodie", 1), ItemManager.CreateByName("pants", 1) },
                new Item[] { ItemManager.CreateByName("syringe.medical", 2), ItemManager.CreateByName("syringe.medical", 2), ItemManager.CreateByName("bandage", 3) },
                new Item[] { ItemManager.CreateByName("smg.thompson", 1) },
                new Item[] { /*ItemManager.CreateByName("", 1)*/ },
                "Soldier"
            ),
            new Kit
            (
                new Item[] { ItemManager.CreateByName("roadsign.jacket", 1), ItemManager.CreateByName("coffeecan.helmet", 1), ItemManager.CreateByName("roadsign.kilt", 1), ItemManager.CreateByName("roadsign.gloves", 1), ItemManager.CreateByName("shoes.boots", 1), ItemManager.CreateByName("hoodie", 1), ItemManager.CreateByName("pants", 1) },
                new Item[] { ItemManager.CreateByName("syringe.medical", 2), ItemManager.CreateByName("syringe.medical", 2), ItemManager.CreateByName("bandage", 3) },
                new Item[] { ItemManager.CreateByName("smg.2", 1), ItemManager.CreateByName("shotgun.double", 1) },
                new Item[] { /*ItemManager.CreateByName("", 1)*/ },
                "CQB",
                2
            ),
            new Kit
            (
                new Item[] { ItemManager.CreateByName("hazmatsuit", 1) },
                new Item[] { ItemManager.CreateByName("syringe.medical", 2), ItemManager.CreateByName("bandage", 3) },
                new Item[] { ItemManager.CreateByName("pistol.semiauto", 1) },
                new Item[] { ItemManager.CreateByName("grenade.beancan", 5) },
                "Rouge"
            ),
            new Kit
            (
                new Item[] { ItemManager.CreateByName("hazmatsuit", 1) },
                new Item[] { ItemManager.CreateByName("syringe.medical", 2), ItemManager.CreateByName("bandage", 3) },
                new Item[] { ItemManager.CreateByName("rifle.semiauto", 1) },
                new Item[] { /*ItemManager.CreateByName("", 1)*/ },
                "Scout"
            )
        },
            //Level-4
            new Kit[]
        {
            new Kit
            (
                new Item[] { ItemManager.CreateByName("metal.plate.torso", 1), ItemManager.CreateByName("metal.facemask", 1), ItemManager.CreateByName("roadsign.kilt", 1), ItemManager.CreateByName("roadsign.gloves", 1), ItemManager.CreateByName("shoes.boots", 1), ItemManager.CreateByName("hoodie", 1), ItemManager.CreateByName("pants", 1) },
                new Item[] { ItemManager.CreateByName("syringe.medical", 2), ItemManager.CreateByName("syringe.medical", 2), ItemManager.CreateByName("syringe.medical", 2), ItemManager.CreateByName("bandage", 3) },
                new Item[] { ItemManager.CreateByName("rifle.ak", 1) },
                new Item[] { /*ItemManager.CreateByName("", 1)*/ },
                "Soldier"
            ),
            new Kit
            (
                new Item[] { ItemManager.CreateByName("metal.plate.torso", 1), ItemManager.CreateByName("metal.facemask", 1), ItemManager.CreateByName("roadsign.kilt", 1), ItemManager.CreateByName("roadsign.gloves", 1), ItemManager.CreateByName("shoes.boots", 1), ItemManager.CreateByName("hoodie", 1), ItemManager.CreateByName("pants", 1) },
                new Item[] { ItemManager.CreateByName("syringe.medical", 2), ItemManager.CreateByName("syringe.medical", 2), ItemManager.CreateByName("syringe.medical", 2), ItemManager.CreateByName("bandage", 3) },
                new Item[] { ItemManager.CreateByName("shotgun.pump", 1), ItemManager.CreateByName("smg.2", 1) },
                new Item[] { /*ItemManager.CreateByName("", 1)*/ },
                "CQB",
                2
            ),
            new Kit
            (
                new Item[] { ItemManager.CreateByName("roadsign.jacket", 1), ItemManager.CreateByName("metal.facemask", 1), ItemManager.CreateByName("roadsign.kilt", 1), ItemManager.CreateByName("roadsign.gloves", 1), ItemManager.CreateByName("shoes.boots", 1), ItemManager.CreateByName("hoodie", 1), ItemManager.CreateByName("pants", 1) },
                new Item[] { ItemManager.CreateByName("syringe.medical", 2), ItemManager.CreateByName("syringe.medical", 2), ItemManager.CreateByName("bandage", 3) },
                new Item[] { ItemManager.CreateByName("pistol.python", 1), ItemManager.CreateByName("crossbow", 1) },
                new Item[] { ItemManager.CreateByName("grenade.f1", 5), ItemManager.CreateByName("arrow.fire", 5) },
                "Rouge",
                4
            ),
            new Kit
            (
                new Item[] { ItemManager.CreateByName("roadsign.jacket", 1), ItemManager.CreateByName("metal.facemask", 1), ItemManager.CreateByName("roadsign.kilt", 1), ItemManager.CreateByName("tactical.gloves", 1), ItemManager.CreateByName("shoes.boots", 1), ItemManager.CreateByName("hoodie", 1), ItemManager.CreateByName("pants", 1) },
                new Item[] { ItemManager.CreateByName("syringe.medical", 2), ItemManager.CreateByName("syringe.medical", 2), ItemManager.CreateByName("bandage", 3) },
                new Item[] { ItemManager.CreateByName("rifle.bolt", 1), ItemManager.CreateByName("pistol.semiauto", 1) },
                new Item[] { ItemManager.CreateByName("weapon.mod.small.scope", 1) },
                "Scout",
                4
            )
        },
            //Level-5
            new Kit[]
        {
            new Kit
            (
                new Item[] { ItemManager.CreateByName("metal.plate.torso", 1, 797410767), ItemManager.CreateByName("metal.facemask", 1, 784316334), ItemManager.CreateByName("roadsign.kilt", 1, 1442346890), ItemManager.CreateByName("roadsign.gloves", 1), ItemManager.CreateByName("shoes.boots", 1, 10023), ItemManager.CreateByName("hoodie", 1, 14179), ItemManager.CreateByName("pants", 1, 1406835139) },
                new Item[] { ItemManager.CreateByName("syringe.medical", 2), ItemManager.CreateByName("syringe.medical", 2), ItemManager.CreateByName("syringe.medical", 2), ItemManager.CreateByName("syringe.medical", 2), ItemManager.CreateByName("bandage", 3) },
                new Item[] { ItemManager.CreateByName("rifle.lr300", 1, 1741459108) },
                new Item[] { /*ItemManager.CreateByName("", 1)*/ },
                "Soldier"
            ),
            new Kit
            (
                new Item[] { ItemManager.CreateByName("metal.plate.torso", 1, 797410767), ItemManager.CreateByName("metal.facemask", 1, 784316334), ItemManager.CreateByName("roadsign.kilt", 1, 1442346890), ItemManager.CreateByName("roadsign.gloves", 1), ItemManager.CreateByName("shoes.boots", 1, 10023), ItemManager.CreateByName("hoodie", 1, 14179), ItemManager.CreateByName("pants", 1, 1406835139) },
                new Item[] { ItemManager.CreateByName("syringe.medical", 2), ItemManager.CreateByName("syringe.medical", 2), ItemManager.CreateByName("syringe.medical", 2), ItemManager.CreateByName("bandage", 3) },
                new Item[] { ItemManager.CreateByName("shotgun.spas12", 1), ItemManager.CreateByName("smg.mp5", 1) },
                new Item[] { /*ItemManager.CreateByName("", 1)*/ },
                "CQB",
                2
            ),
            new Kit
            (
                new Item[] { ItemManager.CreateByName("metal.plate.torso", 1, 797410767), ItemManager.CreateByName("metal.facemask", 1, 784316334), ItemManager.CreateByName("roadsign.kilt", 1, 1442346890), ItemManager.CreateByName("roadsign.gloves", 1), ItemManager.CreateByName("shoes.boots", 1, 10023), ItemManager.CreateByName("hoodie", 1, 14179), ItemManager.CreateByName("pants", 1, 1406835139) },
                new Item[] { ItemManager.CreateByName("syringe.medical", 2), ItemManager.CreateByName("syringe.medical", 2), ItemManager.CreateByName("syringe.medical", 2), ItemManager.CreateByName("bandage", 3) },
                new Item[] { ItemManager.CreateByName("smg.mp5", 1), ItemManager.CreateByName("pistol.m92", 1) },
                new Item[] { ItemManager.CreateByName("grenade.f1", 5)},
                "Rouge",
                2
            ),
            new Kit
            (
                new Item[] { ItemManager.CreateByName("metal.plate.torso", 1, 797410767), ItemManager.CreateByName("metal.facemask", 1, 784316334), ItemManager.CreateByName("roadsign.kilt", 1, 1442346890), ItemManager.CreateByName("roadsign.gloves", 1), ItemManager.CreateByName("shoes.boots", 1, 10023), ItemManager.CreateByName("hoodie", 1, 14179), ItemManager.CreateByName("pants", 1, 1406835139) },
                new Item[] { ItemManager.CreateByName("syringe.medical", 2), ItemManager.CreateByName("syringe.medical", 2), ItemManager.CreateByName("syringe.medical", 2), ItemManager.CreateByName("bandage", 3) },
                new Item[] { ItemManager.CreateByName("rifle.m39", 1), ItemManager.CreateByName("pistol.m92", 1) },
                new Item[] { ItemManager.CreateByName("weapon.mod.small.scope", 1) },
                "Scout",
                2
            )
        }
};

        #endregion

        #region Commands

        [ChatCommand("Debug")]
        void ccDebug(BasePlayer player)
        {
            if (player.IsAdmin)
            {
                if (isDebug) isDebug = false;
                else isDebug = true;
                return;
            }
            player.ChatMessage(Lang["NoUse"]);
        }

        [ChatCommand("Test")]
        void ccTest(BasePlayer player)
        {
            if (player.IsAdmin)
            {
                try
                {
                    PrintToChat(getMinigamer(player).game.GameName);
                }
                catch
                {
                    PrintToChat("oof");
                }
                OnPlayerInit(player);
                writeData<int>(0, "MinigameData/PvPData/" + player.displayName + "C");
                writeData<int>(0, "MinigameData/PvPData/" + player.displayName + "K");
                foreach (var redeem in redeems)
                {
                    writeData<int>(0, "MinigameData/PvPData/" + player.displayName + redeem.name);
                }
                PrintToChat("people 1");
                foreach (var person in Games[0].players)
                {
                    PrintToChat(person.displayName);
                }
                PrintToChat("people 2");
                foreach (var person in Games[1].players)
                {
                    PrintToChat(person.displayName);
                }
                return;
            }
            player.ChatMessage(Lang["NoUse"]);
        }

        [ChatCommand("Pos")]
        void ccPos(BasePlayer player)
        {
            if (player.IsAdmin)
            {
                player.ChatMessage(player.ServerPosition.ToString());
                return;
            }
            player.ChatMessage(Lang["NoUse"]);
        }

        [ChatCommand("Join")]
        void ccJoin(BasePlayer player, string msg, string[] args)
        {
            //add if in specified game already
            Minigamer minigamer = getMinigamer(player);
            if (args.Length == 1)
            {
                foreach (var Game in Games)
                {
                    if (args[0] == Game.GameName)
                    {
                        if (minigamer.isInGame())
                        {
                            minigamer.leaveGame();
                        }
                        minigamer.joinGame(Game);
                        return;
                    }
                }
                player.ChatMessage(string.Format(Lang["BadGameName"], args[0]));
            }
            else
            {
                player.ChatMessage(Lang["BadArgs"]);
            }
        }
        [ChatCommand("Leave")]
        void ccLeave(BasePlayer player, string msg, string[] args)
        {
            Minigamer minigamer = getMinigamer(player);
            if (minigamer.game == Games[0])
            {
                player.ChatMessage(Lang["NotInGame"]);
            }
            else
            {
                minigamer.leaveGame();
                minigamer.joinGame(Games[0]);
            }
        }

        #endregion

        #region Methods

        static void Test()
        {
            if (isDebug)
            {
                foreach (var player in BasePlayer.activePlayerList)
                {
                    if (player.IsAdmin) player.ChatMessage("Test");
                }
            }
        }

        T readData<T>(string path)
        {
            return JsonConvert.DeserializeObject<T>(Interface.Oxide.DataFileSystem.ReadObject<string>(path));
        }

        void writeData<T>(T data, string path)
        {
            string dataString = JsonConvert.SerializeObject(data);
            Interface.Oxide.DataFileSystem.WriteObject(path, dataString);
        }

        private static Minigamer getMinigamer(BasePlayer player)
        {
            foreach (var Minigamer in Minigamers)
            {
                if (Minigamer.player.GetHashCode() == player.GetHashCode())
                {
                    return Minigamer;
                }
            }
            return null;
        }

        bool isInGame(BasePlayer player)
        {
            foreach (var Minigamer in Minigamers)
            {
                if (Minigamer.player == player)
                {
                    if (Minigamer.isInGame()) return true;
                    else return false;
                }
            }
            return false;
        }

        #endregion

        #region Data

        public static Dictionary<string, string> Lang = new Dictionary<string, string>
            {
            //General
            {"BadArgs", "Invalid Arguements."},
            {"NoUse", "You may not use this command."},
            {"PlayerJoined", "{0} has joined the game!"},
            {"PlayerLeft", "{0} has left the game."},
            {"DebugOn", "Debug mode enabled."},
            {"DebugOff", "Debug mode disabled."},
            {"BadGameName", "Could not find game {0}, please try a different name. Do /Games to get a list of games."},
            {"NotInGame", "You are not currently in a game."},
            {"PlayerJoinedServer", "{0} has joined the server!"},
            {"PlayerLeftServer", "{0} has left the server."},
            //Survival
            {"DebugEnemyList", "The following are the entites in the enemies list: {0}."},
            {"DebugWave", "Starting Wave {0} in {1} seconds."},
            {"HasJoined", "You have already joined the game."},
            {"DidJoin", "You have joined the game, please wait for the next one to start."},
            {"DidLeave", "You have left the game."},
            {"HasLeft", "You have not joined a game."},
            {"NewWave", "Beginning Wave {0}!"},
            {"AllDead", "Wave over, all enemies have died. 60 Seconds until next Wave."},
            {"GameOver", "All Players have died, game over."},
            {"InGame", "You may not join, a game is already in session."},
            {"Kicked", "You have been kicked from the game because you died, you must rejoin to play in the next game."},
            {"SetTime", "Set Wave Time to {0} seconds."},
            {"NoObject", "No Object."},
            //PvP
            {"NoRedeem", "You do not have any of those redeems."},
            {"Redeemed", "You succesfully redeemed {0}."},
            {"ChoseClass", "You've chosen class {0}."},
            {"CurrentClass", "You're currently class {0}."},
            {"NotClass", "That isn't a class."},

            };

        #endregion

        #region Hooks

        public void OnServerInitialized()
        {

            /*private Rigidbody rigidbody;

            internal Collider collider;

            internal Bounds bounds;
            rigidbody = gameObject.AddComponent<Rigidbody>();
            rigidbody.useGravity = false;
            rigidbody.isKinematic = true;
            rigidbody.detectCollisions = true;
            rigidbody.collisionDetectionMode = CollisionDetectionMode.Discrete;

            SphereCollider sphereCollider = gameObject.GetComponent<SphereCollider>();
            BoxCollider boxCollider = gameObject.GetComponent<BoxCollider>();

            if (definition.Size != Vector3.zero)
            {
                if (sphereCollider != null)
                    Destroy(sphereCollider);

                if (boxCollider == null)
                {
                    boxCollider = gameObject.AddComponent<BoxCollider>();
                    boxCollider.isTrigger = true;
                }
                boxCollider.size = definition.Size;
                bounds = boxCollider.bounds;
                collider = boxCollider;
            }*/
        }

        public void Loaded()
        {
            redeems = redeems.OrderByDescending(m => m.requirement).ToArray();
            redeems.Reverse();
        }

        void OnPlayerSpawn(BasePlayer player)
        {
            writeData<int>(0, "MinigameData/PvPData/" + player.displayName + "C");
            writeData<int>(0, "MinigameData/PvPData/" + player.displayName + "K");
            foreach (var redeem in redeems)
            {
                writeData<int>(0, "MinigameData/PvPData/" + player.displayName + redeem.name);
            }
        }

        private void OnEntityEnter(TriggerBase trigger, BaseEntity entity)
        {
            if (entity is BasePlayer && entity as BasePlayer != null)
            {
                getMinigamer(entity as BasePlayer).game.OnTriggerEnter(trigger, entity as BasePlayer);
            }
        }
        void OnEntityLeave(TriggerBase trigger, BaseEntity entity)
        {
            if (entity is BasePlayer && entity as BasePlayer != null)
            {
                getMinigamer(entity as BasePlayer).game.OnTriggerExit(trigger, entity as BasePlayer);
            }
        }

        object OnPlayerRespawn(BasePlayer player)
        {
            BasePlayer.SpawnPoint spawnPoint = getMinigamer(player).getSpawn();
            return spawnPoint;
        }

        void OnPlayerRespawned(BasePlayer player)
        {
            getMinigamer(player).game.OnPlayerRespawn(player);
        }

        void OnPlayerInit(BasePlayer player)
        {
            Minigamers.Add(new Minigamer(player, Games[0]));
            //getMinigamer(player).game.playerJoinGame(player);
            PrintToChat(string.Format(Lang["PlayerJoinedServer"], player.displayName));
        }
        void OnPlayerDisconnected(BasePlayer player)
        {
            getMinigamer(player).game.playerLeaveGame(player);
            Minigamers.Remove(getMinigamer(player));
            PrintToChat(string.Format(Lang["PlayerLeftServer"], player.displayName));
        }

        void OnPlayerDie(BasePlayer player, HitInfo info)
        {
            try
            {
                BasePlayer attacker = info.InitiatorPlayer;
                //if (getMinigamer(attacker).game.GameName == "PvP" && getMinigamer(player).game.GameName == "PvP")
                //{
                    int Kills;
                    if (attacker != player)
                    {
                        Kills = readData<int>("MinigameData/PvPData/" + attacker.displayName + "K");
                        Kills += 1;
                        string color = "55ff00";
                        if (Kills < 3) color = "55ff00";
                        else if (Kills < 5) color = "95ff00";
                        else if (Kills < 8) color = "d4ff00";
                        else if (Kills < 10) color = "ffea00";
                        else if (Kills < 15) color = "ffaa00";
                        else if (Kills < 25) color = "ff6a00";
                        else if (Kills < 35) color = "ff4000";
                        else if (Kills < 50) color = "ff2b00";
                        else if (Kills < 100) color = "ff0000";
                        else color = "303030";
                        attacker.ChatMessage($"<color=#{color}>{Kills.ToString()}</color> Kills Killstreak");
                        writeData<int>(Kills, "MinigameData/PvPData/" + attacker.displayName + "K");
                        foreach (redeem Redeem in redeems)
                        {
                            if (Kills == Redeem.requirement)
                            {
                                //Interface.Oxide.DataFileSystem.WriteObject("ArenaData/" + attacker.displayName + Redeem.name, GetPlayerRedeem(attacker, Redeem.name) + 1);
                                writeData<int>(readData<int>("MinigameData/PvPData/" + attacker.displayName + Redeem.name) + 1, "MinigameData/PvPData/" + attacker.displayName + Redeem.name);
                                attacker.ChatMessage($"You got a new <color=#3195ff>{Redeem.name}</color> redeem for " + Kills.ToString() + " kills! Use /Redeem to use it!");
                            }
                        }
                        writeData<int>(0, "MinigameData/PvPData/" + player.displayName + "K");
                    }
                //}
            }
            catch
            {
            }
            
        }

        #endregion

        #endregion

        #region PvPArena

        #region Variables

        redeem[] redeems = new redeem[]
        {
            new redeemL96(),
            new redeemLauncher(),
            new redeemGloves(),
            new redeemM249()
        };

        #region Commands

        [ChatCommand("Redeem")]
        void ChatCommandRedeem(BasePlayer player, string cmd, string[] args)
        {
            if (getMinigamer(player).game.GameName == "PvP")
            {
                if (args.Length != 1) player.ChatMessage(Lang["BadArgs"]);
                else
                {
                    int RedeemID;
                    var isInt = int.TryParse(args[0], out RedeemID);
                    if (isInt && RedeemID <= redeems.Length && RedeemID > 0)
                    {
                        int i = readData<int>("MinigameData/PvPData/" + player.displayName + redeems[RedeemID - 1].name);
                        if (i > 0)
                        {
                            Interface.Oxide.DataFileSystem.WriteObject("MinigameData/PvPData/" + player.displayName + redeems[RedeemID - 1].name, readData<int>("MinigameData/PvPData/" + player.displayName + redeems[RedeemID - 1].name) - 0);
                            redeems[RedeemID - 1].givePlayerRedeem(player);
                            player.ChatMessage(string.Format(Lang["Redeemed"], redeems[RedeemID - 1].name));
                        }
                        else player.ChatMessage(Lang["NoRedeem"]);
                    }
                    else
                    {
                        foreach (redeem Redeem in redeems)
                        {
                            if (Redeem.name == args[0])
                            {
                                if (readData<int>("MinigameData/PvPData/" + player.displayName + args[0]) > 0)
                                {
                                    Interface.Oxide.DataFileSystem.WriteObject("MinigameData/PvPData/" + player.displayName + Redeem.name, readData<int>("MinigameData/PvPData/" + player.displayName + args[0]) - 1);
                                    Redeem.givePlayerRedeem(player);
                                    player.ChatMessage(string.Format(Lang["Redeemed"], redeems[RedeemID - 1].name));
                                    return;
                                }
                            }
                        }
                    }
                    player.ChatMessage(Lang["BagArgs"]);
                }
            }
        }
        /*else if (args.Length == 0)
        {
            string msg = "You have";
            List<string> redeemstrings = new List<string>();
            foreach (redeem Redeem in redeems)
            {
                if (GetPlayerRedeem(player, Redeem.name) > 0)
                {
                    msg += $", {GetPlayerRedeem(player, Redeem.name)} {Redeem.name}";
                }
            }
            player.ChatMessage(msg.Remove(msg.IndexOf(','), 1) + " redeems.");
        }
        else
        {
            int RedeemID;
            var isInt = int.TryParse(args[0], out RedeemID);
            if (isInt && RedeemID <= redeems.Length && RedeemID > 0)
            {
                if (GetPlayerRedeem(player, redeems[RedeemID - 1].name) > 0)
                {
                    Interface.Oxide.DataFileSystem.WriteObject("ArenaData/" + player.displayName + redeems[RedeemID - 1].name, GetPlayerRedeem(player, redeems[RedeemID - 1].name) - 1);
                    redeems[RedeemID - 1].givePlayerRedeem(player);
                    player.ChatMessage("You've redeemed " + redeems[RedeemID - 1].name + "!");
                }
                else player.ChatMessage("You do not have any of those redeems!");
            }
            else
            {
                foreach (redeem Redeem in redeems)
                {
                    if (Redeem.name == args[0])
                    {
                        if (GetPlayerRedeem(player, Redeem.name) > 0)
                        {
                            Interface.Oxide.DataFileSystem.WriteObject("ArenaData/" + player.displayName + Redeem.name, GetPlayerRedeem(player, Redeem.name) - 1);
                            Redeem.givePlayerRedeem(player);
                            player.ChatMessage("You've redeemed " + Redeem.name + "!");
                            return;
                        }
                    }
                }
                BullyPlayer(player, 1);
            }
        }*/


        [ChatCommand("Class")]
        void ChatCommandClass(BasePlayer player, string cmd, string[] args)
        {
            if (args.Length == 0) player.ChatMessage(string.Format(Lang["CurrentClass"], kits[2][readData<int>("MinigameData/PvPData/" + player.displayName + "C") - 1].kitName));
            else if (args.Length > 1) player.ChatMessage(Lang["BadArgs"]);
            else
            {
                int Class;
                var isInt = int.TryParse(args[0], out Class);
                if (isInt && Class <= kits[2].Length && Class > 0)
                { 
                    writeData<int>(Class, "MinigameData/PvPData/" + player.displayName);
                    getMinigamer(player).Class = Class;
                    player.ChatMessage(string.Format(Lang["ChoseClass"], kits[2][Class - 1].kitName));
                }
                else player.ChatMessage(Lang["NotClass"]);
            }
        }

        #endregion

        #endregion

        #endregion

        #region SurvivalArena



        #endregion

        /*static int Map = 1;
        static int Level = 1;
        //public kit[][] kits = new kit[][] { };
        
        Vector3[][] maps = new Vector3[][] {
                new Vector3[]
            {
                new Vector3(-34, 21f, 35.2f),
                new Vector3(-6.8f, 21f, 54.2f),
                new Vector3(5.8f, 21f, 2f),
                new Vector3(-7.7f, 21f, -10.6f),
                new Vector3(-33f, 22f, -2.3f),
                new Vector3(4.5f, 21f, 24.8f),
                new Vector3(6f, 21f, 40.2f),
                new Vector3(-38f, 20f, 5.2f),
                new Vector3(5.7f, 21f, -9.8f)
            },
                new Vector3[]
            {
                new Vector3(-74, 21f, -226.5f),
                new Vector3(-226.5f, 21f, -131.5f),
                new Vector3(-140f, 24f, -107f),
                new Vector3(-137f, 31f, -238f),
                new Vector3(-186f, 31f, -197f),
                new Vector3(-150f, 31f, -161f),
                new Vector3(-121f, 32f, -225f),
                new Vector3(-232.5f, 21f, -170.5f)
            },
                new Vector3[]
            {
                new Vector3(-18.4f, 22f, 258.5f),
                new Vector3(13.5f, 21.5f, 216f),
                new Vector3(-26f, 21f, 190f),
                new Vector3(-64f, 21f, 210.5f),
                new Vector3(-62f, 21f, 227f),
                new Vector3(-24f, 21f, 214f),
                new Vector3(-37.5f, 21f, -233.5f),
                new Vector3(-54f, 21f, 269f)
            }
            };

        /*object OnPlayerRespawn(BasePlayer player)
        {
            return new BasePlayer.SpawnPoint() { pos = getRandomSpawn(), rot = new Quaternion(0, 0, 0, 1) };
        }*/
        /*void OnPlayerRespawned(BasePlayer player)
        {
            if (getMinigamer(player).game.GameName == "PvP")
            {
                int Class;
                try
                {
                    Class = readData<int>("MinigameData/ArenaData/" + player.displayName + "C");
                }
                catch (Exception exception)
                {
                    PrintToConsole(exception.ToString());
                    string ClassString = JsonConvert.SerializeObject("1");
                    Interface.Oxide.DataFileSystem.WriteObject("ArenaData/" + player.displayName, ClassString);
                    Class = readData<int>("MinigameData/ArenaData/" + player.displayName + "C");
                }
                player.inventory.Strip();
                player.Heal(100f);
                kits[2][Class - 1].givePlayerKit(player);
            }
            
        }*/

        

        /*void UpdatePlayers()
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList as List<BasePlayer>)
            {
                CuiHelper.DestroyUi(player, "Position_panel");
                CuiHelper.DestroyUi(player, "Position_panel2");
                UpdatePlayerUI(player);
            }
        }
        [ChatCommand("Update")]
        void cccuiUpdate(BasePlayer player)
        {
            if (player.IsAdmin)
            {
                UpdatePlayers();
            }
            else
                BullyPlayer(player, 0);
        }
        void UpdatePlayerUI(BasePlayer player)
        {
            string playersString = BasePlayer.activePlayerList.Count.ToString();

            var containerplayercount = new CuiElementContainer();

            var panelplayercount = containerplayercount.Add(new CuiPanel { Image = { Color = "0.1 0.1 0.1 0" }, RectTransform = { AnchorMin = "0.96 0.95", AnchorMax = "1 1" }, CursorEnabled = false }, "Hud", "Position_panel");
            containerplayercount.Add(new CuiLabel { Text = { Text = playersString, FontSize = 18 }, RectTransform = { AnchorMin = "0.4 0", AnchorMax = "1.5 0.6" } }, panelplayercount);
            CuiHelper.AddUi(player, containerplayercount);

            /*var containerleaderboard = new CuiElementContainer();

            var panelleaderboard = containerleaderboard.Add(new CuiPanel { Image = { Color = "0.1 0.1 0.1 0" }, RectTransform = { AnchorMin = "-0.11 0", AnchorMax = "0.2 1" }, CursorEnabled = false }, "Hud", "Position_panel2");
            //List<BasePlayer> topPlayers;
            //SortedDictionary<int, BasePlayer> topPlayers = new SortedDictionary<int, BasePlayer>();
            List<KeyValuePair<int, BasePlayer>> topPlayers = new List<KeyValuePair<int, BasePlayer>>();
            //List<int> topPlayers = new List<int>();
            foreach (BasePlayer person in BasePlayer.activePlayerList as List<BasePlayer>)
            {
                topPlayers.Add(new KeyValuePair<int, BasePlayer>(GetPlayerKills(person), person));
                //topPlayers.Add
            }
            float y = 0.98f;
            foreach (KeyValuePair<int, BasePlayer> pair in topPlayers.OrderBy(kvp => kvp.Key).Take(3))
            {
                containerleaderboard.Add(new CuiLabel { Text = { Text = pair.Value.displayName + " " + pair.Key.ToString() + " kills", FontSize = 18 }, RectTransform = { AnchorMin = "0.98 0", AnchorMax = "1.5 1" } }, panelleaderboard);
                  y -= 0.05f;
            }
            
            CuiHelper.AddUi(player, containerleaderboard);*/
        //}

        //Classes

        /*public class Order
            {
                public int sellID;
                public int sellAmount;
                public int currencyID;
                public int currencyAmount;
                public Order(int sellID, int sellAmount, int currencyID, int currencyAmount)
                {
                    this.sellID = sellID;
                    this.sellAmount = sellAmount;
                    this.currencyID = currencyID;
                    this.currencyAmount = currencyAmount;
                }
            }

            //Chat Commands

            [ChatCommand("Join")]
            void ccJoin(BasePlayer player)
            {
                if (isPlayer(player))
                {
                    player.ChatMessage(Lang["HasJoined"]);
                }
                else if (!isGame)
                {
                    Players.Add(player);
                    player.ChatMessage(Lang["DidJoin"]);
                    PrintToChat(string.Format(Lang["PlayerJoined"], player.displayName));
                    player.Teleport(spawnPos);
                    player.Heal(100.0f - player.health);
                }
                else
                {
                    player.ChatMessage(Lang["InGame"]);
                }
            }

            [ChatCommand("Leave")]
            void ccLeave(BasePlayer player)
            {
                if (isPlayer(player))
                {
                    player.ChatMessage(Lang["HasLeft"]);
                    PrintToChat(string.Format(Lang["PlayerLeft"], player.displayName));
                    Players.Remove(player);
                    if (isGame)
                    {
                        player.DieInstantly();
                    }
                }
                else
                {
                    player.ChatMessage(Lang["DidLeave"]);
                }
            }

            //Admin Commands

            [ChatCommand("Start")]
            void ccStart(BasePlayer player)
            {
                if (player.IsAdmin)
                {
                    startGame();
                }
                else
                {
                    player.ChatMessage(Lang["NoUse"]);
                }
            }

            [ChatCommand("Debug")]
            void ccDebug(BasePlayer player)
            {
                if (player.IsAdmin)
                {
                    if (isDebug)
                    {
                        player.ChatMessage(Lang["DebugOff"]);
                        isDebug = false;
                    }
                    else
                    {
                        player.ChatMessage(Lang["DebugOn"]);
                        isDebug = true;
                    }
                }
                else
                {
                    player.ChatMessage(Lang["NoUse"]);
                }
            }

            [ChatCommand("Time")]
            void ccTime(BasePlayer player, string cmd, string args)
            {
                if (player.IsAdmin)
                {
                    if (args.Length != 1)
                    {
                        player.ChatMessage(Lang["BadArgs"]);
                    }
                    else
                    {
                        float i;
                        if (float.TryParse(args[0].ToString(), out i))
                        {
                            waveTime = i;
                            player.ChatMessage(string.Format(Lang["SetTime"], args[0]));
                        }
                        else
                        {
                            player.ChatMessage(Lang["BadArgs"]);
                        }
                    }
                }
                else
                {
                    player.ChatMessage(Lang["NoUse"]);
                }
            }

            [ChatCommand("End")]
            void ccEnd(BasePlayer player)
            {
                if (player.IsAdmin)
                {
                    endGame();
                }
                else
                {
                    player.ChatMessage(Lang["NoUse"]);
                }
            }

            [ChatCommand("Pos")]
            void ccPos(BasePlayer player)
            {
                if (player.IsAdmin)
                {
                    player.ChatMessage(player.transform.position.ToString());
                }
                else
                {
                    player.ChatMessage(Lang["NoUse"]);
                }
            }

            [ChatCommand("Rot")]
            void ccRot(BasePlayer player)
            {
                if (player.IsAdmin)
                {
                    player.ChatMessage(player.GetNetworkRotation().y.ToString());
                }
                else
                {
                    player.ChatMessage(Lang["NoUse"]);
                }
            }

            //Hooks

            void OnServerInitialized()
            {
                Players = new List<BasePlayer>();
                Enemies = new List<BaseEntity>();
                ArenaObjects = new List<BaseEntity>();
                vendingMachines = new List<NPCVendingMachine>();

                foundationPos = new Vector3[]
                {
                new Vector3(-37.4f, 39.9f, 29.6f),
                new Vector3(-24.3f, 39.9f, 17.5f),
                new Vector3(-35.3f, 39.9f, 13.4f),
                new Vector3(-41.3f, 39.9f, 18.6f),
                new Vector3(-17.6f, 39.9f, 23.5f)
                };
                foundationRot = new Quaternion[]
                {
                new Quaternion(0.0f, 0.5f, 0.0f, 1.0f),
                new Quaternion(0.0f, 0.5f, 0.0f, 1.0f),
                new Quaternion(0.0f, 0.0f, 0.0f, 1.0f),
                new Quaternion(0.0f, 0.0f, 0.0f, 1.0f),
                new Quaternion(0.0f, 0.0f, 0.0f, 1.0f)
                };
                vendingMachinePos = new Vector3[]
                {
                new Vector3(-34.0f, 40.1f, 14.4f),
                new Vector3(-35.4f, 40.1f, 14.3f),
                new Vector3(-25.6f, 40.1f, 17.7f),
                new Vector3(-37.1f, 40.1f, 28.1f),
                new Vector3(-36.2f, 40.1f, 29.0f),
                new Vector3(-24.4f, 40.1f, 18.7f),
                new Vector3(-18.6f, 40.1f, 23.5f),
                new Vector3(-39.9f, 40.1f, 19.1f)
                };
                Puts(vendingMachinePos.Length.ToString());
                vendingMachineRot = new Quaternion[]
                {
                new Quaternion(0.0f, 0.02310395f, 0.0f, 1.0f),
                new Quaternion(0.0f, 0.02310395f, 0.0f, 1.0f),
                new Quaternion(0.0f, -0.35653587f, 0.0f, 1.0f),
                new Quaternion(0.0f, 2.55653587f, 0.0f, 1.0f),
                new Quaternion(0.0f, 2.55653587f, 0.0f, 1.0f),
                new Quaternion(0.0f, -0.35653587f, 0.0f, 1.0f),
                new Quaternion(0.0f, -0.95653587f, 0.0f, 1.0f),
                new Quaternion(0.0f, 0.95653587f, 0.0f, 1.0f)
                };
                vendingMachineOrder = new Order[][]
                {
                new Order[]
                {
                    new Order(1079279582, 1, -932201673, 10),
                    new Order(-2072273936, 1, -932201673, 1),
                    new Order(1367190888, 1, -932201673, 3),
                    new Order(-586342290, 1, -932201673, 14)
                },
                new Order[]
                {
                    new Order(1751045826, 1, -932201673, 15),
                    new Order(237239288, 1, -932201673, 15),
                    new Order(-1549739227, 1, -932201673, 15),
                    new Order(-1108136649, 1, -932201673, 50)
                },
                new Order[]
                {
                    new Order(-803263829, 1, -932201673, 20),
                    new Order(-699558439, 1, -932201673, 15),
                    new Order(-2002277461, 1, -932201673, 15),
                    new Order(1850456855, 1, -932201673, 15),
                    new Order(-194953424, 1, -932201673, 30),
                    new Order(1110385766, 1, -932201673, 35)
                },
                new Order[]
                {
                    new Order(1965232394, 1, -932201673, 30),
                    new Order(649912614, 1, -932201673, 40),
                    new Order(-765183617, 1, -932201673, 50),
                    new Order(818877484, 1, -932201673, 60),
                    new Order(-904863145, 1, -932201673, 70),
                    new Order(1796682209, 1, -932201673, 85)
                },
                new Order[]
                {
                    new Order(14241751, 1, -932201673, 5),
                    new Order(-1234735557, 1, -932201673, 1),
                    new Order(785728077, 3, -932201673, 1),
                    new Order(-1211166256, 2, -932201673, 1),
                    new Order(-1685290200, 1, -932201673, 4),
                    new Order(-1036635990, 1, -932201673, 6)
                },
                new Order[]
                {
                    new Order(-932201673, 1, 69511070, 5),
                    new Order(-932201673, 1, -151838493, 15),
                    new Order(-932201673, 3, -858312878, 10),
                    new Order(69511070, 2, -932201673, 1),
                    new Order(-151838493, 3, -932201673, 1),
                    new Order(-858312878, 2, -932201673, 1)
                },
                new Order[]
                {
                    new Order(1545779598, 1, -932201673, 200),
                    new Order(1588298435, 1, -932201673, 70),
                    new Order(-852563019, 1, -932201673, 130),
                    new Order(-742865266, 1, -932201673, 85),
                    new Order(143803535, 1, -932201673, 35),
                    new Order(442886268, 1, -932201673, 400)
                },
                new Order[]
                {
                    new Order(-484206264, 1, -932201673, 150)
                }
                /*
                1079279582, 1, -932201673, 10 = syringe
                -2072273936, 1, -932201673, 1 = bandage
                1367190888, 1, -932201673, 3 = corn
                -586342290, 1, -932201673, 14 = blueberries

                1751045826, 1, -932201673, 15 = hoodie
                237239288, 1, -932201673, 15 = pants
                -1549739227, 1, -932201673, 15 = boots
                -1108136649, 1, -932201673, 50 = tac gloves

                -803263829, 1, -932201673, 20 = coffecan
                -699558439, 1, -932201673, 15 = road gloves
                -2002277461, 1, -932201673, 15 =  road jacket
                1850456855, 1, -932201673, 15 = road kilt
                -194953424, 1, -932201673, 30 =  metal face
                1110385766, 1, -932201673, 35 = metal chest

                1965232394, 1, -932201673, 30 = crossy
                649912614, 1, -932201673, 40 = revvy
                -765183617, 1, -932201673, 50 = double barrel
                818877484, 1, -932201673, 60 = p2
                -904863145, 1, -932201673, 70 = semi rifle
                1796682209, 1, -932201673, 85 = custom

                14241751, 1, -932201673, 5 = fire arrow
                -1234735557, 1, -932201673, 1 = arrow
                785728077, 3, -932201673, 1 = pistol bullet
                -1211166256, 2, -932201673, 1 = rifle bullet
                -1685290200, 1, -932201673, 4 = 12 gauge
                -1036635990, 1, -932201673, 6 = incen shell

                -932201673, 1, 69511070, 5 = metal frags 4 scrap
                -932201673, 1, -151838493, 15 = wood 4 scrap
                -932201673, 3, -858312878, 10 = cloth 4 scrap
                69511070, 2, -932201673, 1 = scrap 4 metal frags
                -151838493, 3, -932201673, 1 = scrap 4 wood
                -858312878, 2, -932201673, 1 = scrap 4 cloth

                1545779598, 1, -932201673, 200/*, 1760078043* = /ak - no mercy
                1588298435, 1, -932201673, 70 = bolt
                -852563019, 1, -932201673, 130 = m92
                -742865266, 1, -932201673, 85 = rocket
                143803535, 1, -932201673, 35 = grenade
                442886268, 1, -932201673, 400 = launcher

                -484206264, 1, -932201673, 150 = blue card
                */
                /*};

                setupArena();
            }

            void Unloaded()
            {
                endGame();
                setupArena();
            }

            void OnPlayerSleepEnded(BasePlayer player)
            {
                player.inventory.Strip();
            }

            void OnPlayerDie(BasePlayer player)
            {
                if (isPlayer(player) && isGame)
                {
                    player.ChatMessage(Lang["Kicked"]);
                    Players.Remove(player);
                    checkIsPlayersDead();
                }
            }

            /*object OnPlayerRespawn(BasePlayer player)
            {
                player.inventory.Strip();
                player.Heal(100.0f - player.health);
                Puts("OnPlayerRespawn works!");
                return new BasePlayer.SpawnPoint() { pos = hubPos, rot = new Quaternion(0, 0, 0, 1) };
            }*/

            /*void OnEntityKill(BaseNetworkable entity)
            {
                if (Enemies.Count != 0)
                {
                    foreach (var enemy in Enemies)
                    {
                        if (enemy.GetHashCode() == entity.GetHashCode())
                        {
                            try
                            {
                                Enemies.Remove(enemy);
                                checkIsEnemiesDead();
                                break;
                            }
                            catch
                            {
                                Puts(Lang["NoObject"]);
                            }
                        }
                    }
                }
            }

            object OnRotateVendingMachine(VendingMachine machine, BasePlayer player)
            {
                return true;
            }

            void OnVendingTransaction(VendingMachine machine, BasePlayer buyer, int sellOrderId, int numberOfTransactions)
            {
                ProtoBuf.VendingMachine.SellOrder sellOrder = machine.sellOrders.sellOrders[sellOrderId];
                if (sellOrder.itemToSellID == -484206264)
                {
                    //door1.transform.position = door1.transform.position - new Vector3(0.0f, 40.0f, 0.0f);
                    //door2.transform.position = door2.transform.position - new Vector3(0.0f, 40.0f, 0.0f);
                    //door1.transform.TransformVector(0.0f, -40.0f, 0.0f);
                    //door2.transform.TransformVector(0.0f, -40.0f, 0.0f);
                    //door1.transform.TransformVector(0.0f, 0.0f, 0.0f);
                    //door2.transform.TransformVector(0.0f, 0.0f, 0.0f);
                    door1.transform.position = new Vector3(0.0f, 1.0f, 0.0f);
                    door1.SendNetworkUpdate();
                    door2.transform.position = new Vector3(0.0f, 1.0f, 0.0f);
                    door2.SendNetworkUpdate();
                }
            }
            //
            //Variables

            public static Vector3 hubPos = new Vector3(-19.9f, 46.0f, 36.0f);
            public static Vector3 spawnPos = new Vector3(-32.1f, 46.7f, 21.8f);

            public static Vector3 EnemyPos1 = new Vector3(-24.0f, 40.3f, 28.0f);
            public static Vector3 EnemyPos2 = new Vector3(-42.2f, 40.3f, 23.4f);
            public static Vector3 EnemyPos3 = new Vector3(-30.6f, 40.3f, 12.8f);

            public static Vector3 grillPos = new Vector3(-32.4f, 46.8f, 21.7f);

            public static Quaternion grillRot = new Quaternion(0.0f, 3.0f, 0.0f, 1.0f);

            public static Vector3 workbenchPos = new Vector3(-30.8f, 39.9f, 28.7f);

            public static Quaternion workbenchRot = new Quaternion(0.0f, -19.8f, 0.0f, 1.0f);

            public static Vector3 doorPos1 = new Vector3(-25.0f, 40.0f, 23.0f);
            public static Vector3 doorPos2 = new Vector3(-25.0f, 40.0f, 24.3f);

            public static Quaternion doorRot1 = new Quaternion(0.0f, 0.0f, 0.0f, 1.0f);
            public static Quaternion doorRot2 = new Quaternion(0.0f, 0.0f, 0.0f, 1.0f);

            public static Vector3[] foundationPos;
            public static Quaternion[] foundationRot;

            public static Vector3[] vendingMachinePos;
            public static Quaternion[] vendingMachineRot;
            public static Order[][] vendingMachineOrder;
            public static List<NPCVendingMachine> vendingMachines;

            public static List<BasePlayer> Players;

            public static List<BaseEntity> Enemies;

            public static List<BaseEntity> ArenaObjects;

            public static List<BaseEntity> RemoveEntitesList;
            public static List<BasePlayer> RemovePlayersList;

            public static bool isGame = false;
            public static bool isDebug = false;

            public static int waveCount = 0;

            public static float waveTime = 45.0f;

            public static BaseEntity grill;

            public static BaseEntity door1;
            public static BaseEntity door2;

            //Methods

            void setupArena()
            {
                BaseCombatEntity basecombat;
                BaseEntity temp;

                grill = GameManager.server.CreateEntity("assets/prefabs/building core/foundation/foundation.prefab", grillPos, grillRot, true);
                grill.Spawn();
                grill.GetComponent<BuildingBlock>().SetGrade((BuildingGrade.Enum)4);
                basecombat = grill.GetComponentInParent<BaseCombatEntity>();
                basecombat.ChangeHealth(basecombat.MaxHealth());

                door1 = GameManager.server.CreateEntity("assets/bundled/prefabs/static/door.hinged.security.blue.prefab", doorPos1, doorRot1, true);
                door1.Spawn();
                door2 = GameManager.server.CreateEntity("assets/bundled/prefabs/static/door.hinged.security.blue.prefab", doorPos2, doorRot2, true);
                door2.Spawn();

                /*BaseEntity vendingMachine1 = GameManager.server.CreateEntity("assets/prefabs/deployable/vendingmachine/npcvendingmachine.prefab", vendingMachinePos[0], vendingMachineRot[0]);
                BaseEntity vendingMachine2 = GameManager.server.CreateEntity("assets/prefabs/deployable/vendingmachine/npcvendingmachine.prefab", vendingMachinePos[1], vendingMachineRot[1]);
                BaseEntity vendingMachine3 = GameManager.server.CreateEntity("assets/prefabs/deployable/vendingmachine/npcvendingmachine.prefab", vendingMachinePos[2], vendingMachineRot[2]);
                BaseEntity vendingMachine4 = GameManager.server.CreateEntity("assets/prefabs/deployable/vendingmachine/npcvendingmachine.prefab", vendingMachinePos[3], vendingMachineRot[3]);
                BaseEntity vendingMachine5 = GameManager.server.CreateEntity("assets/prefabs/deployable/vendingmachine/npcvendingmachine.prefab", vendingMachinePos[4], vendingMachineRot[4]);
                BaseEntity vendingMachine6 = GameManager.server.CreateEntity("assets/prefabs/deployable/vendingmachine/npcvendingmachine.prefab", vendingMachinePos[5], vendingMachineRot[5]);
                BaseEntity vendingMachine7 = GameManager.server.CreateEntity("assets/prefabs/deployable/vendingmachine/npcvendingmachine.prefab", vendingMachinePos[6], vendingMachineRot[6]);
                BaseEntity vendingMachine8 = GameManager.server.CreateEntity("assets/prefabs/deployable/vendingmachine/npcvendingmachine.prefab", vendingMachinePos[7], vendingMachineRot[7]);
                vendingMachine1.Spawn();
                vendingMachine2.Spawn();
                vendingMachine3.Spawn();
                vendingMachine4.Spawn();
                vendingMachine5.Spawn();
                vendingMachine6.Spawn();
                vendingMachine7.Spawn();
                vendingMachine8.Spawn();
                vendingMachine1.GetComponent<NPCVendingMachine>().vendingOrders.orders = getOrders(0);
                vendingMachine2.GetComponent<NPCVendingMachine>().vendingOrders.orders = getOrders(1);
                vendingMachine3.GetComponent<NPCVendingMachine>().vendingOrders.orders = getOrders(2);
                vendingMachine4.GetComponent<NPCVendingMachine>().vendingOrders.orders = getOrders(3);
                vendingMachine5.GetComponent<NPCVendingMachine>().vendingOrders.orders = getOrders(4);
                vendingMachine6.GetComponent<NPCVendingMachine>().vendingOrders.orders = getOrders(5);
                vendingMachine7.GetComponent<NPCVendingMachine>().vendingOrders.orders = getOrders(6);
                vendingMachine8.GetComponent<NPCVendingMachine>().vendingOrders.orders = getOrders(7);

                setOrders(vendingMachine1.GetComponent<NPCVendingMachine>(), 0);
                setOrders(vendingMachine2.GetComponent<NPCVendingMachine>(), 1);
                setOrders(vendingMachine3.GetComponent<NPCVendingMachine>(), 2);
                setOrders(vendingMachine4.GetComponent<NPCVendingMachine>(), 3);
                setOrders(vendingMachine5.GetComponent<NPCVendingMachine>(), 4);
                setOrders(vendingMachine6.GetComponent<NPCVendingMachine>(), 5);
                setOrders(vendingMachine7.GetComponent<NPCVendingMachine>(), 6);
                setOrders(vendingMachine8.GetComponent<NPCVendingMachine>(), 7);*/

                /*for (int i = 0; i < vendingMachines.Count; i++)
                {
                    vendingMachines[i].Spawn();
                    vendingMachines[i].ServerPosition = vendingMachinePos[i];
                    vendingMachines[i].ServerRotation = vendingMachineRot[i];
                    vendingMachines[i].GetComponent<NPCVendingMachine>().vendingOrders.orders = getOrders(i);
                    ArenaObjects.Add(vendingMachines[i]);
                }*/




                /*for (int i = 0; i < foundationPos.Length; i++)
                {
                    temp = basecombat = null;
                    temp = GameManager.server.CreateEntity("assets/prefabs/building core/foundation/foundation.prefab", foundationPos[i], foundationRot[i], true);
                    temp.Spawn();
                    temp.GetComponent<BuildingBlock>().SetGrade((BuildingGrade.Enum)4);
                    //temp.Spawn();
                    basecombat = temp.GetComponentInParent<BaseCombatEntity>();
                    basecombat.ChangeHealth(basecombat.MaxHealth());
                    ArenaObjects.Add(temp);
                }

                /*BaseEntity vendingMachine1 = GameManager.server.CreateEntity("assets/prefabs/deployable/vendingmachine/vendingmachine.deployed.prefab", vendingMachinePos[0], vendingMachineRot[0], true);
                BaseEntity vendingMachine2 = GameManager.server.CreateEntity("assets/prefabs/deployable/vendingmachine/vendingmachine.deployed.prefab", vendingMachinePos[1], vendingMachineRot[1], true);
                BaseEntity vendingMachine3 = GameManager.server.CreateEntity("assets/prefabs/deployable/vendingmachine/vendingmachine.deployed.prefab", vendingMachinePos[2], vendingMachineRot[2], true);
                BaseEntity vendingMachine4 = GameManager.server.CreateEntity("assets/prefabs/deployable/vendingmachine/vendingmachine.deployed.prefab", vendingMachinePos[3], vendingMachineRot[3], true);
                BaseEntity vendingMachine5 = GameManager.server.CreateEntity("assets/prefabs/deployable/vendingmachine/vendingmachine.deployed.prefab", vendingMachinePos[4], vendingMachineRot[4], true);
                BaseEntity vendingMachine6 = GameManager.server.CreateEntity("assets/prefabs/deployable/vendingmachine/vendingmachine.deployed.prefab", vendingMachinePos[5], vendingMachineRot[5], true);
                BaseEntity vendingMachine7 = GameManager.server.CreateEntity("assets/prefabs/deployable/vendingmachine/vendingmachine.deployed.prefab", vendingMachinePos[6], vendingMachineRot[6], true);
                BaseEntity vendingMachine8 = GameManager.server.CreateEntity("assets/prefabs/deployable/vendingmachine/vendingmachine.deployed.prefab", vendingMachinePos[7], vendingMachineRot[7], true);

                vendingMachine1.Spawn();
                vendingMachine2.Spawn();
                vendingMachine3.Spawn();
                vendingMachine4.Spawn();
                vendingMachine5.Spawn();
                vendingMachine6.Spawn();
                vendingMachine7.Spawn();
                vendingMachine8.Spawn();

                ArenaObjects.Add(vendingMachine1);
                ArenaObjects.Add(vendingMachine2);
                ArenaObjects.Add(vendingMachine3);
                ArenaObjects.Add(vendingMachine4);
                ArenaObjects.Add(vendingMachine5);
                ArenaObjects.Add(vendingMachine6);
                ArenaObjects.Add(vendingMachine7);
                ArenaObjects.Add(vendingMachine8);

                //scrap = -932201673
                //syringe = 1079279582
                //bandage = -2072273936
                //corn = 1367190888
                //blueberries = -586342290

                setOrders(vendingMachine1.GetComponent<VendingMachine>(), 0);
                setOrders(vendingMachine2.GetComponent<VendingMachine>(), 1);
                setOrders(vendingMachine3.GetComponent<VendingMachine>(), 2);
                setOrders(vendingMachine4.GetComponent<VendingMachine>(), 3);
                setOrders(vendingMachine5.GetComponent<VendingMachine>(), 4);
                setOrders(vendingMachine6.GetComponent<VendingMachine>(), 5);
                setOrders(vendingMachine7.GetComponent<VendingMachine>(), 6);
                setOrders(vendingMachine8.GetComponent<VendingMachine>(), 7);*/

                /*addOrder(vendingMachine1.GetComponent<VendingMachine>(), 1079279582, 1, -932201673, 10);//syringe
                addOrder(vendingMachine1.GetComponent<VendingMachine>(), -2072273936, 1, -932201673, 1);//bandage
                addOrder(vendingMachine1.GetComponent<VendingMachine>(), 1367190888, 1, -932201673, 3);//corn
                addOrder(vendingMachine1.GetComponent<VendingMachine>(), -586342290, 1, -932201673, 14);//blueberries

                addOrder(vendingMachine2.GetComponent<VendingMachine>(), 1751045826, 1, -932201673, 15);//hoodie
                addOrder(vendingMachine2.GetComponent<VendingMachine>(), 237239288, 1, -932201673, 15);//pants
                addOrder(vendingMachine2.GetComponent<VendingMachine>(), -1549739227, 1, -932201673, 15);//boots
                addOrder(vendingMachine2.GetComponent<VendingMachine>(), -1108136649, 1, -932201673, 50);//tac gloves

                addOrder(vendingMachine3.GetComponent<VendingMachine>(), -803263829, 1, -932201673, 20);//coffecan
                addOrder(vendingMachine3.GetComponent<VendingMachine>(), -699558439, 1, -932201673, 15);//road gloves
                addOrder(vendingMachine3.GetComponent<VendingMachine>(), -2002277461, 1, -932201673, 15);// road jacket
                addOrder(vendingMachine3.GetComponent<VendingMachine>(), 1850456855, 1, -932201673, 15);//road kilt
                addOrder(vendingMachine3.GetComponent<VendingMachine>(), -194953424, 1, -932201673, 30);// metal face
                addOrder(vendingMachine3.GetComponent<VendingMachine>(), 1110385766, 1, -932201673, 35);//metal chest

                addOrder(vendingMachine4.GetComponent<VendingMachine>(), 1965232394, 1, -932201673, 30);//crossy
                addOrder(vendingMachine4.GetComponent<VendingMachine>(), 649912614, 1, -932201673, 40);//revvy
                addOrder(vendingMachine4.GetComponent<VendingMachine>(), -765183617, 1, -932201673, 50);//double barrel
                addOrder(vendingMachine4.GetComponent<VendingMachine>(), 818877484, 1, -932201673, 60);//p2
                addOrder(vendingMachine4.GetComponent<VendingMachine>(), -904863145, 1, -932201673, 70);//semi rifle
                addOrder(vendingMachine4.GetComponent<VendingMachine>(), 1796682209, 1, -932201673, 85);//custom

                addOrder(vendingMachine5.GetComponent<VendingMachine>(), 14241751, 1, -932201673, 5);//fire arrow
                addOrder(vendingMachine5.GetComponent<VendingMachine>(), -1234735557, 1, -932201673, 1);//arrow
                addOrder(vendingMachine5.GetComponent<VendingMachine>(), 785728077, 3, -932201673, 1);//pistol bullet
                addOrder(vendingMachine5.GetComponent<VendingMachine>(), -1211166256, 2, -932201673, 1);//rifle bullet
                addOrder(vendingMachine5.GetComponent<VendingMachine>(), -1685290200, 1, -932201673, 4);//12 gauge
                addOrder(vendingMachine5.GetComponent<VendingMachine>(), -1036635990, 1, -932201673, 6);//incen shell

                addOrder(vendingMachine6.GetComponent<VendingMachine>(), -932201673, 1, 69511070, 5);//metal frags 4 scrap
                addOrder(vendingMachine6.GetComponent<VendingMachine>(), -932201673, 1, -151838493, 15);//wood 4 scrap
                addOrder(vendingMachine6.GetComponent<VendingMachine>(), -932201673, 3, -858312878, 10);//cloth 4 scrap
                addOrder(vendingMachine6.GetComponent<VendingMachine>(), 69511070, 2, -932201673, 1);//scrap 4 metal frags
                addOrder(vendingMachine6.GetComponent<VendingMachine>(), -151838493, 3, -932201673, 1);//scrap 4 wood
                addOrder(vendingMachine6.GetComponent<VendingMachine>(), -858312878, 2, -932201673, 1);//scrap 4 cloth

                addOrder(vendingMachine7.GetComponent<VendingMachine>(), 1545779598, 1, -932201673, 200);//ak
                addOrder(vendingMachine7.GetComponent<VendingMachine>(), 1588298435, 1, -932201673, 70);//bolt
                addOrder(vendingMachine7.GetComponent<VendingMachine>(), -852563019, 1, -932201673, 130);//m92
                addOrder(vendingMachine7.GetComponent<VendingMachine>(), -742865266, 1, -932201673, 85);//rocket
                addOrder(vendingMachine7.GetComponent<VendingMachine>(), 143803535, 1, -932201673, 35);//grenade
                addOrder(vendingMachine7.GetComponent<VendingMachine>(), 442886268, 1, -932201673, 400);//launcher

                addOrder(vendingMachine8.GetComponent<VendingMachine>(), -484206264, 1, -932201673, 150);//blue card*/

                //GameManager.server.CreateEntity("assets/prefabs/deployable/tier 1 workbench/workbench1.deployed.prefab", workbenchPos, workbenchRot, true).Spawn();
            }

    /*void setOrders(VendingMachine vendingMachine, int ordersID)
    {
        foreach (var thing in vendingMachineOrder[ordersID])
        {
            ProtoBuf.VendingMachine.SellOrder order = new ProtoBuf.VendingMachine.SellOrder();
            order.itemToSellID = thing.sellID;
            order.itemToSellAmount = thing.sellAmount;
            order.currencyID = thing.currencyID;
            order.currencyAmountPerItem = thing.currencyAmount;
            vendingMachine.sellOrders.sellOrders.Add(order);
            ItemManager.CreateByItemID(thing.sellID, 999999/*, skin*//*).MoveToContainer(vendingMachine.inventory);
            vendingMachine.RefreshSellOrderStockLevel();
        }

    }

    void addOrder(NPCVendingMachine vendingMachine, int sellID, int sellAmount, int buyID, int buyAmount/*, ulong skin = 0*//*)
    {
        ProtoBuf.VendingMachine.SellOrder order = new ProtoBuf.VendingMachine.SellOrder();
        order.itemToSellID = sellID;
        order.itemToSellAmount = sellAmount;
        order.currencyID = buyID;
        order.currencyAmountPerItem = buyAmount;
        vendingMachine.sellOrders.sellOrders.Add(order);
        ItemManager.CreateByItemID(sellID, 999999/*, skin*//*).MoveToContainer(vendingMachine.inventory);
        vendingMachine.RefreshSellOrderStockLevel();
    }

    void checkIsEnemiesDead()
    {
        if (Enemies.Count == 0)
        {
            RunGame(waveTime);
        }
    }

    void checkIsPlayersDead()
    {
        if (Players.Count == 0 && isGame)
        {
            endGame();
        }
    }

    void endGame()
    {
        isGame = false;
        PrintToChat(Lang["GameOver"]);
        waveCount = 0;
        //grill.transform.position = grillPos;
        //door1.transform.position = doorPos1;
        //door2.transform.position = doorPos2;
        //grill.transform.TransformVector(grillPos);
        //grill.transform.TransformVector(doorPos1);
        //grill.transform.TransformVector(doorPos2);
        door1.transform.position = doorPos1;
        door1.SendNetworkUpdate();
        door2.transform.position = doorPos2;
        door2.SendNetworkUpdate();
        grill.transform.position = grillPos;
        grill.SendNetworkUpdate();
        if (Enemies.Count != 0)
        {
            for (int i = 0; i <= Enemies.Count; i++)
            {
                Enemies.Remove(Enemies[i]);
            }
            Enemies.Clear();
        }
        foreach (var person in Players)
        {
            person.inventory.Strip();
            person.Heal(100.0f - person.health);
            person.Teleport(hubPos);
        }
    }

    void startGame()
    {
        isGame = true;
        foreach (var person in Players)
        {
            //person.Teleport(spawnPos);
            person.inventory.Strip();
            person.Heal(100.0f - person.health);
            person.inventory.GiveItem(ItemManager.CreateByName("spear.stone", 1));
            person.inventory.GiveItem(ItemManager.CreateByName("bandage", 1));
        }
        PrintToChat("Starting Game!");
        grill.transform.position = grill.transform.position - new Vector3(0.0f, 40.0f, 0.0f);
        grill.SendNetworkUpdate();
        //grill.transform.TransformVector(0.0f, 0.0f, 0.0f);
        //grill.transform.position = new Vector3(0.0f, 1.0f, 0.0f);
        // grill.transform.SetPositionAndRotation(new Vector3(0.0f, 1.0f, 0.0f), new Quaternion());
        RunGame(15.0f);
    }

    void RunGame(float time)
    {
        if (isDebug)
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                if (player.IsAdmin)
                {
                    player.ChatMessage(string.Format(Lang["DebugWave"], waveCount, time));
                }
            }
        }
        timer.Repeat(time, 1, () =>
        {
            if (isGame)
            {
                Wave();
            }

        }
        );
    }

    void Wave()
    {
        waveCount += 1;
        PrintToChat(string.Format(Lang["NewWave"], waveCount));
        prepareEnemies();
        spawnEnemies();
        if (isDebug)
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                if (player.IsAdmin)
                {
                    player.ChatMessage(string.Format(Lang["DebugEnemyList"], Enemies.ToSentence()));
                }
            }
        }
    }

    bool isPlayer(BasePlayer player)
    {
        bool isPlayer = false;
        foreach (var person in Players)
        {
            if (person == player) isPlayer = true;
        }
        return isPlayer;
    }

    void prepareEnemies()
    {

    }

    void spawnEnemies()
    {
        for (int i = 0; i < waveCount; i++)
        {
            /*if (i % 6 == 0)
            {
                Enemies.Add(new BaseEntity());
                Enemies[Enemies.Count - 1] = GameManager.server.CreateEntity("assets/prefabs/npc/murderer/murderer.prefab", EnemyPos1, new Quaternion(), true);
                Enemies[Enemies.Count - 1].Spawn();
            }
            if (i % 3 == 0)
            {
                Enemies.Add(new BaseEntity());
                Enemies[Enemies.Count - 1] = GameManager.server.CreateEntity("assets/prefabs/npc/murderer/murderer.prefab", EnemyPos1, new Quaternion(), true);
                Enemies[Enemies.Count - 1].Spawn();
            }
            else */
                                                           /*if (i % 3 == 0)
                                                           {
                                                               /*BaseEntity enemy2 = GameManager.server.CreateEntity("assets/prefabs/npc/murderer/murderer.prefab", EnemyPos1, new Quaternion(), true);
                                                               enemy2.Spawn();
                                                               Enemies.Add(enemy2);*/
                                                           //Enemies.Add(GameManager.server.CreateEntity("assets/prefabs/npc/murderer/murderer.prefab", EnemyPos1, new Quaternion(), true));
                                                           //Enemies[Enemies.Count - 1].Spawn();
                                                           /*Enemies.Add(new BaseEntity());
                                                           Enemies[Enemies.Count - 1] = GameManager.server.CreateEntity("assets/prefabs/npc/murderer/murderer.prefab", EnemyPos3, new Quaternion(), true);
                                                           Enemies[Enemies.Count - 1].Spawn();
                                                       }
                                                       else if (i % 2 == 0)
                                                       {
                                                           /*BaseEntity enemy2 = GameManager.server.CreateEntity("assets/prefabs/npc/murderer/murderer.prefab", EnemyPos1, new Quaternion(), true);
                                                           enemy2.Spawn();
                                                           Enemies.Add(enemy2);*/
                                                           //Enemies.Add(GameManager.server.CreateEntity("assets/prefabs/npc/murderer/murderer.prefab", EnemyPos1, new Quaternion(), true));
                                                           //Enemies[Enemies.Count - 1].Spawn();
                                                           /*Enemies.Add(new BaseEntity());
                                                           Enemies[Enemies.Count - 1] = GameManager.server.CreateEntity("assets/prefabs/npc/murderer/murderer.prefab", EnemyPos2, new Quaternion(), true);
                                                           Enemies[Enemies.Count - 1].Spawn();
                                                       }
                                                       else//(i % 1 == 0) else
                                                       {
                                                           /*BaseEntity enemy = GameManager.server.CreateEntity("assets/prefabs/npc/murderer/murderer.prefab", EnemyPos2, new Quaternion(), true);
                                                           enemy.Spawn();
                                                           Enemies.Add(enemy);*/
                                                           //new BaseEntity();
                                                           /*Enemies.Add(new BaseEntity());
                                                           Enemies[Enemies.Count - 1] = GameManager.server.CreateEntity("assets/prefabs/npc/murderer/murderer.prefab", EnemyPos1, new Quaternion(), true);
                                                           Enemies[Enemies.Count - 1].Spawn();
                                                       }
                                                   }
                                               }

                                               Vector3 getRandomEnemyPos()
                                               {
                                                   Vector3 pos = new Vector3();
                                                   Random rand = new Random();
                                                   switch (rand.Next(2))
                                                   {
                                                       case 1:
                                                           pos = EnemyPos1;
                                                           break;
                                                       case 2:
                                                           pos = EnemyPos2;
                                                           break;
                                                       default:
                                                           pos = EnemyPos1;
                                                           break;
                                                   }
                                                   return pos;
                                               }

                                               //Data


                                           }*/
}
