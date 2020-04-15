using Facepunch;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using Rust;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using Random = System.Random;
//goodnight gamers
namespace Oxide.Plugins
{
    [Info("Minigame", "Fwog Bootew", 1.0)]
    [Description("A Minigame plugin for Rust.")]
    public class Minigame : RustPlugin
    {
        /*
         * BaseEntity baseEntity = col?.ToBaseEntity();
                if (!baseEntity.IsValid())
                    return;

                if (baseEntity is BasePlayer)
                {
                    Instance.OnPlayerEnterZone(baseEntity as BasePlayer, this);
                    return;
                }

                Instance.OnEntityEnterZone(baseEntity, this);
                */
        #region General

        #region Structs

        public struct TriggerInfo
        {
            public Vector3 size;
            public Vector3 pos;
            public Quaternion rot;
            public string name;
            public TriggerInfo(Vector3 size, Vector3 pos, Quaternion rot, string name)
            {
                this.size = size;
                this.pos = pos;
                this.rot = rot;
                this.name = name;
            }
        }

        public struct Kit
        {
            Item[] attire;
            Item[] meds;
            Item[] weapons;
            Item[] misc;
            public string kitName;
            bool giveAmmo;
            int stackDivision;
            public Kit(Item[] attire, Item[] meds, Item[] weapons, Item[] misc, string kitName, int stackDivision = 1, bool giveAmmo = true)
            {
                this.attire = attire;
                this.meds = meds;
                this.weapons = weapons;
                this.misc = misc;
                this.kitName = kitName;
                this.stackDivision = stackDivision;
                this.giveAmmo = giveAmmo;
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
                        player.GiveItem(weapon);
                        if (giveAmmo == true) player.GiveItem(ItemManager.CreateByItemID((weapon.GetHeldEntity() as BaseProjectile).primaryMagazine.ammoType.itemid, ItemManager.CreateByItemID((weapon.GetHeldEntity() as BaseProjectile).primaryMagazine.ammoType.itemid).MaxStackable() / stackDivision));
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
        public class Trigger : MonoBehaviour
        {
            internal Collider collider;
            internal Bounds bounds;
            private Rigidbody rigidbody;
            public Game game;
            /*Vector3 size;
            Vector3 pos;
            Quaternion rot;*/
            /*public Trigger(Vector3 size, Vector3 pos, Quaternion rot)
            {
                this.size = size;
                this.pos = pos;
                this.rot = rot;
            }*/
            private void Awake()
            {
                gameObject.layer = (int)Layer.Reserved1;
                gameObject.name = "Trigger";
            }
            private void OnTriggerEnter(Collider collider)
            {
                BaseEntity baseEntity = collider?.ToBaseEntity();
                if (!baseEntity.IsValid()) return;
                game.OnTriggerEnter(this, baseEntity);
            }
            private void OnTriggerExit(Collider collider)
            {
                BaseEntity baseEntity = collider?.ToBaseEntity();
                if (!baseEntity.IsValid()) return;
                game.OnTriggerExit(this, baseEntity);
            }
            public void InitTrigger(Vector3 size, Vector3 pos, Quaternion rot, string name)
            {
                /*BoxCollider boxCollider;

                boxCollider = gameObject.AddComponent<BoxCollider>();
                boxCollider.isTrigger = true;
                boxCollider.transform.position = pos;
                boxCollider.transform.rotation = rot;
                boxCollider.size = size;*/
                this.name = name;
                transform.position = pos;
                transform.rotation = rot;

                if (collider != null)
                    DestroyImmediate(collider);

                if (rigidbody != null)
                    DestroyImmediate(rigidbody);

                rigidbody = gameObject.AddComponent<Rigidbody>();
                rigidbody.useGravity = false;
                rigidbody.isKinematic = true;
                rigidbody.detectCollisions = true;
                rigidbody.collisionDetectionMode = CollisionDetectionMode.Discrete;

                BoxCollider boxCollider = gameObject.GetComponent<BoxCollider>();

                if (boxCollider == null)
                {
                    boxCollider = gameObject.AddComponent<BoxCollider>();
                    boxCollider.isTrigger = true;
                }
                boxCollider.size = size;
                bounds = boxCollider.bounds;
                collider = boxCollider;
                //game.triggers.Add(this);
            }
            public void deconstruct()
            {
                DestroyImmediate(collider);
                DestroyImmediate(rigidbody);
            }
        }

        /*public class Enemy
        {
            public bool isInvulnerable = false;
            public static List<BaseEntity> enemies = new List<BaseEntity>();
            public BaseEntity enemy;
            public Game game = null;
            public Enemy(BaseEntity enemy, Game game)
            {
                this.game = game;
                this.enemy = enemy;
                enemies.Add(enemy);
            }
            ~Enemy()
            {
                enemies.Remove(enemy);
            }
            public Vector3 getSpawn()
            {
                //Test();
                return game.getEnemySpawn();
            }
        }*/

        public class Minigamer
        {
            //General
            public bool isInvulnerable = false;
            public static List<BasePlayer> players = new List<BasePlayer>();
            public BasePlayer player;
            public int status;
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
                //Test();
                return game.getPlayerSpawn();
            }
        }

        public class Game
        {
            public int minPop;
            public List<Trigger> triggers;
            public string GameName;
            public List<BasePlayer> players = new List<BasePlayer>();
            public List<BaseEntity> enemies = new List<BaseEntity>();
            public int playerMax;
            public bool isOpen = true;
            public bool isGame = false;
            public Vector3 GeneratePlayerSpawn(Vector3[] spawns)
            {
                Random rand = new Random();
                return spawns[rand.Next(spawns.Length)];
            }
            public Vector3 GeneratePlayerSpawnArea(Vector3 PointA, Vector3 PointB)
            {
                Random rand = new Random();
                //return spawns[rand.Next(spawns.Length)];
                return new Vector3(
                    rand.Next(int.Parse(PointA.x.ToString()), int.Parse(PointB.x.ToString())), 
                    rand.Next(int.Parse(PointA.y.ToString()), int.Parse(PointB.y.ToString())), 
                    rand.Next(int.Parse(PointA.z.ToString()), int.Parse(PointB.z.ToString())));
            }
            virtual public Vector3 getSpawn()
            {
                return new Vector3();
            }
            virtual public object OnNpcStopMoving(NPCPlayerApex npc)
            {
                return null;
            }
            virtual public object OnNpcPlayerTarget(NPCPlayerApex npcPlayer, BaseEntity entity)
            {
                return null;
            }
            virtual public void OnVendingTransaction(VendingMachine machine, BasePlayer buyer, int sellOrderId, int numberOfTransactions)
            {

            }
            virtual public void OnMaxStackable(Item item)
            {

            }
            virtual public void OnPlayerRespawn(BasePlayer player)
            {

            }
            virtual public void OnPlayerDie(BasePlayer player, HitInfo info)
            {

            }
            virtual public void OnWeaponFired(BaseProjectile projectile, BasePlayer player)
            {

            }
            virtual public BasePlayer.SpawnPoint getPlayerSpawn()
            {
                return new BasePlayer.SpawnPoint() { pos = new Vector3(), rot = new Quaternion() };
            }
            virtual public Vector3 getEnemySpawn()
            {
                return new Vector3();
            }
            virtual public void runCmd(BasePlayer player, string cmd, string[] args)
            {

            }
            virtual public void init()
            {

            }
            virtual public void playerLeaveGame(BasePlayer player)
            {
                players.Remove(player);
                UpdatePlayers();
                checkIsOpen();
                broadcastToPlayers(string.Format(Lang["PlayerLeft"], player.displayName));
            }
            virtual public void playerJoinGame(BasePlayer player)
            {
                players.Add(player);
                UpdatePlayers();
                checkIsOpen();
                broadcastToPlayers(string.Format(Lang["PlayerJoined"], player.displayName));
            }
            virtual public void initData(BasePlayer player)
            {

            }
            virtual public void OnTriggerEnter(Trigger trigger, BaseEntity entity)
            {

            }
            virtual public void OnTriggerExit(Trigger trigger, BaseEntity entity)
            {

            }
            virtual public void startGame()
            {

            }
            virtual public void endGame()
            {

            }
            public void broadcastToPlayers(string msg)
            {
                foreach (var player in players)
                {
                    player.ChatMessage(msg);
                }
            }
            virtual public void UpdatePlayerUI(BasePlayer player)
            {
                try
                {
                    CuiHelper.DestroyUi(player, "Position_panel");
                }
                catch { }
                string playersString = players.Count.ToString() + "/" + playerMax.ToString();

                var containerplayercount = new CuiElementContainer();

                var panelplayercount = containerplayercount.Add(new CuiPanel { Image = { Color = "0.1 0.1 0.1 0" }, RectTransform = { AnchorMin = "0.96 0.95", AnchorMax = "1 1" }, CursorEnabled = false }, "Hud", "Position_panel");
                containerplayercount.Add(new CuiLabel { Text = { Text = playersString, FontSize = 18 }, RectTransform = { AnchorMin = "0.4 0", AnchorMax = "1.5 0.6" } }, panelplayercount);
                CuiHelper.AddUi(player, containerplayercount);
            }
            public void UpdatePlayers()
            {
                foreach (BasePlayer player in players)
                {
                    UpdatePlayerUI(player);
                }
            }
            virtual public void checkIsOpen()
            {
                if (players.Count == playerMax) isOpen = false;
                else isOpen = true;

            }
        }

        public class HubGame : Game
        {
            static Kit kit = kits[3][0];
            private Vector3 hubSpawn = new Vector3(42.6f, 45.0f, -232.7f);
            //private Vector3 triggerSpawn = new Vector3(42.6f, 38.0f, -232.7f);

            //private TriggerTemperature trigger;
            public HubGame(Vector3 hubSpawn, TriggerInfo[] triggerInfos)
            {
                this.hubSpawn = hubSpawn;
                //trigger = new TriggerTemperature() { triggerSize = 10.0f/*, enabled = true*/ };
                //trigger.transform.position = hubSpawn;
                GameName = "Hub";
                players = new List<BasePlayer>();
                playerMax = 100;
                triggers = new List<Trigger>();
                foreach (var triggerInfo in triggerInfos)
                {
                    triggers.Add(CreateTrigger(triggerInfo));
                }
                foreach (var trigger in triggers)
                {
                    trigger.game = this;
                }
            }
            public override void playerLeaveGame(BasePlayer player)
            {
                players.Remove(player);
                UpdatePlayers();
                checkIsOpen();
                sendDebug("Player left hub");
            }
            public override void playerJoinGame(BasePlayer player)
            {
                getMinigamer(player).isInvulnerable = true;
                players.Add(player);
                UpdatePlayers();
                checkIsOpen();
                player.inventory.Strip();
                player.Heal(100.0f);
                player.metabolism.bleeding = new MetabolismAttribute { value = 0.0f };
                player.Teleport(hubSpawn);
                sendDebug("Player joined hub");
                player.SendNetworkUpdateImmediate();
            }
            public override void OnPlayerRespawn(BasePlayer player)
            {
                player.inventory.Strip();
                player.Heal(100.0f - player.health);
            }
            public override BasePlayer.SpawnPoint getPlayerSpawn()
            {
                //Test();
                return new BasePlayer.SpawnPoint() { pos = hubSpawn, rot = new Quaternion() };
            }
            public override void OnTriggerEnter(Trigger trigger, BaseEntity entity)
            {
                switch (trigger.name)
                {
                    case "PvPPit":
                        if (entity is BasePlayer)
                        {
                            BasePlayer player = entity.ToPlayer();
                            kit.givePlayerKit(player);
                            getMinigamer(player).isInvulnerable = false;
                        }
                        break;
                    default:
                        break;
                }
            }
            public override void OnTriggerExit(Trigger trigger, BaseEntity entity)
            {
                if (entity is BasePlayer)
                {
                    BasePlayer player = entity.ToPlayer();
                    player.inventory.Strip();
                    player.Heal(100.0f);
                    player.metabolism.bleeding = new MetabolismAttribute { value = 0.0f };
                    getMinigamer(player).isInvulnerable = true;
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
                playerMax = 8;
            }
            public override void runCmd(BasePlayer player, string cmd, string[] args)
            {
                switch (cmd)
                {
                    case "Redeem":
                    case "redeem":
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
                        break;
                    case "Class":
                    case "class":
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
                        break;
                    default:
                        player.ChatMessage(Lang["NoUseGame"]);
                        return;
                }
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
            public override void initData(BasePlayer player)
            {
                writeData<int>(0, "MinigameData/PvPData/" + player.displayName + "C");
                writeData<int>(0, "MinigameData/PvPData/" + player.displayName + "K");
                foreach (var redeem in redeems)
                {
                    writeData<int>(0, "MinigameData/PvPData/" + player.displayName + redeem.name);
                }
            }
            public override void playerJoinGame(BasePlayer player)
            {
                getMinigamer(player).isInvulnerable = false;
                players.Add(player);
                UpdatePlayers();
                checkIsOpen();
                broadcastToPlayers(string.Format(Lang["PlayerJoined"], player.displayName));
                player.Teleport(GeneratePlayerSpawn(spawns));
                player.inventory.Strip();
                player.Heal(100.0f - player.health);
                kits[2][getMinigamer(player).Class].givePlayerKit(player);
                player.SendNetworkUpdateImmediate();
            }
            public override void OnPlayerDie(BasePlayer player, HitInfo info)
            {
                try
                {
                    BasePlayer attacker = info.InitiatorPlayer;
                    getMinigamer(player).isInvulnerable = true;
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
                }
                catch
                {
                }
            }
        }
        /**/

        public class SurvivalGame : Game
        {
            private List<BaseEntity> Enemies;
            Minigame minigame = new Minigame();
            private Vector3 hubPos;
            private Vector3 startPos;
            private Vector3[] zombiePos;

            private Buyable[] buyables;

            public int waveCount;

            private List<BasePlayer> alivePlayers;
            public SurvivalGame(Vector3 hubPos, Vector3 startPos, Vector3[] zombiePos, Buyable[] buyables)
            {
                minPop = 1;
                enemies = new List<BaseEntity>();
                GameName = "Survival";
                players = new List<BasePlayer>();
                playerMax = 8;
                alivePlayers = new List<BasePlayer>();
                this.hubPos = hubPos;
                this.startPos = startPos;
                this.zombiePos = zombiePos;
                this.buyables = buyables;
                waveCount = 0;
                foreach (var buyable in this.buyables)
                {
                    buyable.spawn();
                }
            }
            public override void OnVendingTransaction(VendingMachine machine, BasePlayer buyer, int sellOrderId, int numberOfTransactions)
            {
                ProtoBuf.VendingMachine.SellOrder sellOrder = machine.sellOrders.sellOrders[sellOrderId];
                foreach (var buyable in buyables)
                {
                    if (sellOrder.itemToSellID == buyable.buyItemID)
                    {
                        buyable.buy();
                    }
                }


            }
            public override void startGame()
            {
                foreach (var player in players)
                {
                    getMinigamer(player).isInvulnerable = false;
                    player.Heal(100.0f);
                    player.Teleport(startPos);
                    player.inventory.Strip();
                    alivePlayers.Add(player);
                    kits[0][0].givePlayerKit(player);
                }
                isGame = true;
                minigame.RunGame(15f, this);
            }
            public override void endGame()
            {
                foreach (var player in players)
                {
                    player.Heal(100.0f);
                    player.Teleport(hubPos);
                    player.inventory.Strip();
                }
                foreach (var buyable in buyables)
                {
                    buyable.reset();
                }
                try
                {
                    for (int i = 0; i <= enemies.Count; i++)
                    {
                        enemies[i].ToPlayer().DieInstantly();
                    }
                }
                catch { }
                isGame = false;
                minigame.joinStart(this);
            }
            public override void playerJoinGame(BasePlayer player)
            {
                getMinigamer(player).isInvulnerable = false;
                players.Add(player);
                UpdatePlayers();
                checkIsOpen();
                broadcastToPlayers(string.Format(Lang["PlayerJoined"], player.displayName));
                player.Teleport(hubPos);
                player.inventory.Strip();
                player.Heal(100.0f - player.health);
                minigame.joinStart(this);
                player.SendNetworkUpdateImmediate();
            }
            public override void playerLeaveGame(BasePlayer player)
            {
                base.playerLeaveGame(player);
                alivePlayers.Remove(player);
                checkIsPlayersDead();
            }
            public override object OnNpcStopMoving(NPCPlayerApex npc)
            {
                return true;
            }
            public override void OnPlayerDie(BasePlayer player, HitInfo info)
            {
                if (isGame)
                {
                    foreach (var person in alivePlayers)
                    {
                        if (person == player)
                        {
                            alivePlayers.Remove(player);
                            checkIsPlayersDead();
                            return;
                        }
                    }
                    enemies.Remove(player);
                    checkIsEnemiesDead();
                }
            }
            public override BasePlayer.SpawnPoint getPlayerSpawn()
            {
                return new BasePlayer.SpawnPoint() { pos = hubPos, rot = new Quaternion() };
            }

            public void Wave()
            {
                waveCount += 1;
                broadcastToPlayers(string.Format(Lang["NewWave"], waveCount));
                spawnEnemies();
                sendDebug(string.Format(Lang["DebugEnemyList"], enemies.ToSentence()));
            }
            void checkIsEnemiesDead()
            {
                if (enemies.Count == 0)
                {
                    minigame.RunGame(45f, this);
                }
            }

            void checkIsPlayersDead()
            {
                if (alivePlayers.Count == 0 && isGame)
                {
                    endGame();
                }
            }
            void spawnEnemies()
            {
                for (int i = 0; i < waveCount; i++)
                {
                    /*if (i % 6 == 0)
                    {
                        Enemies.Add(new BaseEntity());
                        Enemies[Enemies.Count - 1] = GameManager.server.CreateEntity("assets/prefabs/npc/murderer/murderer.prefab", zombiePos[0], new Quaternion(), true);
                        Enemies[Enemies.Count - 1].Spawn();
                    }
                    if (i % 3 == 0)
                    {
                        Enemies.Add(new BaseEntity());
                        Enemies[Enemies.Count - 1] = GameManager.server.CreateEntity("assets/prefabs/npc/murderer/murderer.prefab", zombiePos[0], new Quaternion(), true);
                        Enemies[Enemies.Count - 1].Spawn();
                    }
                    else*/
                    if (i % 3 == 0)
                    {
                        /*BaseEntity enemy2 = GameManager.server.CreateEntity("assets/prefabs/npc/murderer/murderer.prefab", zombiePos[0], new Quaternion(), true);
                        enemy2.Spawn();
                        Enemies.Add(enemy2);
                        Enemies.Add(GameManager.server.CreateEntity("assets/prefabs/npc/murderer/murderer.prefab", zombiePos[0], new Quaternion(), true));
                        Enemies[Enemies.Count - 1].Spawn();*/
                        enemies.Add(new BaseEntity());
                        enemies[enemies.Count - 1] = GameManager.server.CreateEntity("assets/prefabs/npc/murderer/murderer.prefab", zombiePos[2], new Quaternion(), true);
                        enemies[enemies.Count - 1].Spawn();
                    }
                    else if (i % 2 == 0)
                    {
                        /*BaseEntity enemy2 = GameManager.server.CreateEntity("assets/prefabs/npc/murderer/murderer.prefab", zombiePos[0], new Quaternion(), true);
                        enemy2.Spawn();
                        Enemies.Add(enemy2);
                        Enemies.Add(GameManager.server.CreateEntity("assets/prefabs/npc/murderer/murderer.prefab", EnemyPos1, new Quaternion(), true));
                        Enemies[Enemies.Count - 1].Spawn();*/
                        enemies.Add(new BaseEntity());
                        enemies[enemies.Count - 1] = GameManager.server.CreateEntity("assets/prefabs/npc/murderer/murderer.prefab", zombiePos[1], new Quaternion(), true);
                        enemies[enemies.Count - 1].Spawn();
                    }
                    else//(i % 1 == 0) else
                    {
                        /*BaseEntity enemy = GameManager.server.CreateEntity("assets/prefabs/npc/murderer/murderer.prefab", zombiePos[1], new Quaternion(), true);
                        enemy.Spawn();
                        Enemies.Add(enemy);
                        new BaseEntity();*/
                        enemies.Add(new BaseEntity());
                        enemies[enemies.Count - 1] = GameManager.server.CreateEntity("assets/prefabs/npc/murderer/murderer.prefab", zombiePos[0], new Quaternion(), true);
                        enemies[enemies.Count - 1].Spawn();
                    }
                }
            }
        }

        public class AimGame : Game
        {
            static Kit kit = kits[5][0];
            private Vector3[] spawns;
            private int botCount;
            //private Vector3 hubSpawn = new Vector3(42.6f, 45.0f, -232.7f);
            //private Vector3 triggerSpawn = new Vector3(42.6f, 38.0f, -232.7f);

            //private TriggerTemperature trigger;
            public AimGame(Vector3[] spawns, int botCount)
            {
                this.botCount = botCount;
                this.spawns = spawns;
                //trigger = new TriggerTemperature() { triggerSize = 10.0f/*, enabled = true*/ };
                //trigger.transform.position = hubSpawn;
                GameName = "Aim";
                players = new List<BasePlayer>();
                playerMax = 100;
                enemies = new List<BaseEntity>();
            }
            private void spawnBots()
            {
                sendDebug("test1");
                for (int i = 0; i < botCount; i++)
                {
                    sendDebug("test2");
                    var entity = GameManager.server.CreateEntity("assets/prefabs/npc/murderer/murderer.prefab", GeneratePlayerSpawn(spawns), new Quaternion(), true);
                    sendDebug("test3");
                    //(entity as BasePlayer).inventory.Strip();
                    sendDebug("test4");
                    entity.Spawn();
                    sendDebug("test5");
                    enemies.Add(entity);
                    sendDebug("init bot aim");
                }
            }
            public override void init()
            {
                sendDebug("init aim");
                spawnBots();
            }
            public override object OnNpcStopMoving(NPCPlayerApex npc)
            {
                sendDebug("npc try stop");
                return true;
            }
            public override object OnNpcPlayerTarget(NPCPlayerApex npcPlayer, BaseEntity entity)
            {
                sendDebug("npc target");
                return true;
            }
            public override void playerLeaveGame(BasePlayer player)
            {
                players.Remove(player);
                UpdatePlayers();
                checkIsOpen();
                sendDebug("Player left aim");
            }
            public override void playerJoinGame(BasePlayer player)
            {
                getMinigamer(player).isInvulnerable = true;
                players.Add(player);
                UpdatePlayers();
                checkIsOpen();
                player.inventory.Strip();
                player.Heal(100.0f);
                player.metabolism.bleeding = new MetabolismAttribute { value = 0.0f };
                player.Teleport(GeneratePlayerSpawn(spawns));
                kit.givePlayerKit(player);
                player.SendNetworkUpdateImmediate();
                sendDebug("Player joined aim");
            }
            public override void OnWeaponFired(BaseProjectile projectile, BasePlayer player)
            {
                projectile.GetItem().condition = projectile.GetItem().info.condition.max;
                projectile.primaryMagazine.contents = projectile.primaryMagazine.capacity;
                projectile.SendNetworkUpdateImmediate();
            }
            public override void OnPlayerDie(BasePlayer player, HitInfo info)
            {
                base.OnPlayerDie(player, info);
            }
            public override void OnPlayerRespawn(BasePlayer player)
            {
                player.inventory.Strip();
                player.Heal(100.0f - player.health);
            }
            public override BasePlayer.SpawnPoint getPlayerSpawn()
            {
                return new BasePlayer.SpawnPoint() { pos = GeneratePlayerSpawn(spawns), rot = new Quaternion() };
            }
        }

        public class BuildGame : Game
        {
            Kit kit = kits[0][1];
            public BuildGame(TriggerInfo[] triggerInfos)
            {
                GameName = "Build";
                players = new List<BasePlayer>();
                playerMax = 100;
                triggers = new List<Trigger>();
                foreach (var triggerInfo in triggerInfos)
                {
                    triggers.Add(CreateTrigger(triggerInfo));
                }
                foreach (var trigger in triggers)
                {
                    trigger.game = this;
                }
            }

            public override void playerJoinGame(BasePlayer player)
            {
                getMinigamer(player).isInvulnerable = true;
                players.Add(player);
                UpdatePlayers();
                checkIsOpen();
                player.inventory.Strip();
                player.Heal(100.0f);
                player.metabolism.bleeding = new MetabolismAttribute { value = 0.0f };
                //player.Teleport(GeneratePlayerSpawnArea(triggers[0].bounds.extents, triggers[0].bounds.extents));
                //broken
                player.Teleport(triggers[0].transform.position);
                kit.givePlayerKit(player);
                player.SendNetworkUpdateImmediate();
                sendDebug("Player joined build");
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

        public class redeemLR300 : redeem
        {
            public redeemLR300()
            {
                name = "lr300";
                requirement = 4;
            }
            public override void givePlayerRedeem(BasePlayer player)
            {
                Item item = ItemManager.CreateByName("rifle.lr300", 1, 1621894466);
                (item.GetHeldEntity() as BaseProjectile).primaryMagazine.contents = (item.GetHeldEntity() as BaseProjectile).primaryMagazine.capacity;
                player.GiveItem(item);
            }
        }

        public class redeemSuppresor : redeem
        {
            public redeemSuppresor()
            {
                name = "suppresor";
                requirement = 3;
            }
            public override void givePlayerRedeem(BasePlayer player)
            {
                Item item = ItemManager.CreateByName("weapon.mod.silencer", 1);
                //(item.GetHeldEntity() as BaseProjectile).primaryMagazine.contents = (item.GetHeldEntity() as BaseProjectile).primaryMagazine.capacity;
                player.GiveItem(item);
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
                requirement = 2;
            }
            public override void givePlayerRedeem(BasePlayer player)
            {
                player.GiveItem(ItemManager.CreateByName("tactical.gloves", 1));
            }
        }

        #endregion

        #region Variables

        private static List<Minigamer> Minigamers = new List<Minigamer>();
        static List<Game> Games;

        static bool isDebug = false;

        static bool isLoaded = false;

        static PlayerInventory playerInventory = new PlayerInventory();

        public static Kit[][] kits;

        #endregion

        #region Commands

        [ChatCommand("SetStatus")]
        void ccSetStatus(BasePlayer player, string msg, string[] args)
        {
            if (!player.IsAdmin)
            {
                player.ChatMessage(Lang["NoUse"]);
                return;
            }
            int x;
            if (args.Length == 2)
            {
                if (int.TryParse(args[1], out x))
                {
                    foreach (var person in BasePlayer.activePlayerList)
                    {
                        if (person.displayName == args[0])
                        {
                            getMinigamer(person).status = x;
                            player.ChatMessage(string.Format(Lang["SetStatus"], person.displayName, x.ToString()));
                        }
                    }
                }
            }
            else if (args.Length == 1)
            {
                if (int.TryParse(args[0], out x))
                {
                    getMinigamer(player).status = x;
                    player.ChatMessage(string.Format(Lang["SetStatus"], player.displayName, x.ToString()));
                }
            }
            else player.ChatMessage(Lang["BadArgs"]);
        }

        [ChatCommand("Debug")]
        void ccDebug(BasePlayer player)
        {
            if (!player.IsAdmin)
            {
                player.ChatMessage(Lang["NoUse"]);
                return;
            }
            if (isDebug)
            {
                isDebug = false;
                writeData<bool>(isDebug, "MinigameData/General/isDebug");
                player.ChatMessage(Lang["DebugOff"]);
            }
            else
            {
                isDebug = true;
                writeData<bool>(isDebug, "MinigameData/General/isDebug");
                player.ChatMessage(Lang["DebugOn"]);
            }
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
                //OnPlayerInit(player);
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

        [ChatCommand("Game")]
        void ccGame(BasePlayer player)
        {
            player.ChatMessage(string.Format(Lang["CurrentGame"], getMinigamer(player).game.GameName));
        }

        [ChatCommand("Games")]
        void ccGames(BasePlayer player)
        {
            player.ChatMessage(string.Format(Lang["Games"], Games.ToSentence()));
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
                        if (Game != minigamer.game)
                        {
                            if (Game.isOpen)
                            {
                                if (minigamer.isInGame()) minigamer.leaveGame();
                                minigamer.joinGame(Game);
                                return;
                            }
                            else player.ChatMessage(Lang["GameFull"]);
                        }
                        else player.ChatMessage(string.Format(Lang["InGame"], Game.GameName));
                    }
                }
                //player.ChatMessage(string.Format(Lang["BadGameName"], args[0]));
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

        public static void sendDebug(string msg)
        {
            if (isDebug)
            {
                foreach (var player in BasePlayer.activePlayerList)
                {
                    if (player.IsAdmin)
                    {
                        player.ChatMessage(msg);
                    }
                }
            }
        }

        void joinStart(Game game)
        {
            if (game.players.Count == game.minPop)
            {
                game.broadcastToPlayers(Lang["60ToStart"]);
                timer.Repeat(60f, 1, () =>
                {
                    game.startGame();
                });
                //rhe
            }
        }

        static Trigger CreateTrigger(TriggerInfo triggerInfo)
        {
            Trigger trigger = new GameObject().AddComponent<Trigger>();
            trigger.InitTrigger(triggerInfo.size, triggerInfo.pos, triggerInfo.rot, triggerInfo.name);
            return trigger;
        }

        static void Test()
        {
            sendDebug("Test");
        }

        static T readData<T>(string path)
        {
            return JsonConvert.DeserializeObject<T>(Interface.Oxide.DataFileSystem.ReadObject<string>(path));
        }

        static void writeData<T>(T data, string path)
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

        private static Game getGameByEnemy(BaseEntity entity)
        {
            foreach(var Game in Games)
            {
                foreach(var enemy in Game.enemies)
                {
                    if (enemy == entity) return Game;
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
            {"NoUseGame", "You may not use this command in this game."},
            {"PlayerJoined", "{0} has joined the game!"},
            {"PlayerLeft", "{0} has left the game."},
            {"DebugOn", "Debug mode enabled."},
            {"DebugOff", "Debug mode disabled."},
            {"BadGameName", "Could not find game {0}, please try a different name. Do /Games to get a list of games."},
            {"NotInGame", "You are not currently in a game."},
            {"InGame", "You are already in game {0}."},
            {"PlayerJoinedServer", "{0} has joined the server!"},
            {"PlayerLeftServer", "{0} has left the server."},
            {"GameFull", "That game is currently full."},
            {"CurrentGame", "You are currently in game {0}."},
            {"Games", "There are the following games: {0}."},
            {"60ToStart", "Game will start in 60 seconds."},
            {"SetStatus", "Set {0}'s status to {1}."},
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

        void OnServerInitialized()
        {
            //
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
            try
            {
                isDebug = readData<bool>("MinigameData/General/isDebug");
            }
            catch
            {
                writeData<bool>(true, "MinigameData/General/isDebug");
                isDebug = readData<bool>("MinigameData/General/isDebug");
            }
            kits = new Kit[][]
{
            //Level-1
            new Kit[]
        {
            new Kit
            (
                new Item[] { /*ItemManager.CreateByName("", 1)*/ },
                new Item[] { ItemManager.CreateByName("bandage", 3) },
                new Item[] { /*ItemManager.CreateByName("", 1)*/ },
                new Item[] { ItemManager.CreateByName("spear.stone", 1) },
                "Soldier"
            ),
            new Kit
            (
                new Item[] { /*ItemManager.CreateByName("", 1)*/ },
                new Item[] { /*ItemManager.CreateByName("bandage", 3)*/ },
                new Item[] { /*ItemManager.CreateByName("", 1)*/ },
                new Item[] { ItemManager.CreateByName("wood", 999999), ItemManager.CreateByName("stones", 999999), ItemManager.CreateByName("metal.fragments", 999999), ItemManager.CreateByName("metal.refined", 999999), ItemManager.CreateByName("building.planner", 1), ItemManager.CreateByName("hammer", 1) },
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
        },
            new Kit[]
            {
                new Kit
                (
                    new Item[] {ItemManager.CreateByName("", 1)},
                    new Item[] {ItemManager.CreateByName("", 1)},
                    new Item[] {ItemManager.CreateByName("rifle.ak", 1), ItemManager.CreateByName("rifle.lr300", 1), ItemManager.CreateByName("rifle.bolt", 1), ItemManager.CreateByName("lmg.m249", 1), ItemManager.CreateByName("shotgun.spas12", 1), ItemManager.CreateByName("smg.mp5", 1), ItemManager.CreateByName("rifle.m39", 1), ItemManager.CreateByName("pistol.m92", 1), ItemManager.CreateByName("pistol.python", 1), ItemManager.CreateByName("crossbow", 1), ItemManager.CreateByName("rifle.semiauto", 1), ItemManager.CreateByName("smg.thompson", 1), ItemManager.CreateByName("shotgun.pump", 1), ItemManager.CreateByName("smg.2", 1), ItemManager.CreateByName("bow.hunting", 1)},
                    new Item[] {ItemManager.CreateByName("", 1)},
                    "Aim",
                    1,
                    false
                )
            }
};

            redeems = new redeem[]
        {
            new redeemL96(),
            new redeemLauncher(),
            new redeemGloves(),
            new redeemM249()
        };
            Games = new List<Game>
            {
            new HubGame(new Vector3(42.6f, 45.0f, -232.7f), new TriggerInfo[] {new TriggerInfo(new Vector3(50.0f, 13.0f, 50.0f), new Vector3(42.6f, 38.0f, -232.7f), new Quaternion(), "PvPPit")}),
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
            new SurvivalGame(new Vector3(-19.8f, 45.0f, 36.4f), new Vector3(-32.4f, 47.8f, 21.7f), new Vector3[]{ new Vector3(-42.3f, 40.3f, 23.4f), new Vector3(-30.6f, 40.3f, 11.6f), new Vector3(-23.9f, 40.4f, 28.2f) }, new Buyable[]{new Buyable(new Vector3(-25.0f, 40.0f, 23.0f), new Quaternion(0.0f, 0.0f, 0.0f, 1.0f)), new Buyable(new Vector3(-25.0f, 40.0f, 24.3f), new Quaternion(0.0f, 0.0f, 0.0f, 1.0f))}),
            new AimGame(new Vector3[] { new Vector3(-168.6f, 46.4f, 155.3f), new Vector3(-277.0f, 45.5f, 154.7f), new Vector3(-229.5f, 46.6f, 82.1f), new Vector3(-161.5f, 47.5f, 36.2f), new Vector3(-287.3f, 44.7f, 38.6f), new Vector3(-230.3f, 44.7f, 62.2f), new Vector3(-205.0f, 45.8f, 138.6f), new Vector3(-255.9f, 43.1f, 143.8f), new Vector3(-189.7f, 41.9f, 69.9f)}, 10),
            new BuildGame(new TriggerInfo[]{new TriggerInfo(new Vector3(20.0f, 20.0f, 20.0f), new Vector3(-30.0f, 40.0f, -140.0f), new Quaternion(), "BuildArea") })
        };
            Minigamers.Clear();
            foreach (var player in BasePlayer.activePlayerList)
            {
                OnPlayerInit(player);
            }
            foreach (var Game in Games)
            {
                Game.init();
                Game.UpdatePlayers();
            }
            isLoaded = true;
        }

        /*object CanMoveItem(Item item, PlayerInventory playerLoot, uint targetContainer, int targetSlot, int amount)
        {
            Puts("Test1");
            playerInventory = playerLoot;
            Puts("Test2" + playerInventory.ToString() + " : " + playerInventory.GetComponent<BasePlayer>().displayName);
            return null;
        }*/
        /*int OnMaxStackable(Item item)
        {
            Puts("Test3 : " + item.name + " : " + item.GetOwnerPlayer().displayName);
            try
            {
                if (!(getMinigamer(item.GetOwnerPlayer()).game is BuildGame))
                {
                    Puts("Test4");
                    return item.MaxStackable();
                }
                Puts("Test5");
                return 999999;
            }
            catch
            {
                Puts("Test6");
                return item.MaxStackable();
            }
        }*/
        private void OnWeaponFired(BaseProjectile projectile, BasePlayer player)
        {
            getMinigamer(player).game.OnWeaponFired(projectile, player);
        }
        /*object OnNpcStopMoving(NPCPlayerApex npc)
        {
            sendDebug("try stop moving");
            return getGameByEnemy(npc as BaseEntity).OnNpcStopMoving(npc);
            //return null;
        }*/
        object OnNPCPlayerTarget(NPCPlayerApex npc, BaseEntity entity)
        {
            sendDebug("Player target");
            return getGameByEnemy(npc as BaseEntity).OnNpcPlayerTarget(npc, entity);
            /*sendDebug("pp");
            try
            {
                sendDebug("pp1");
                return getMinigamer(entity as BasePlayer).game.OnNpcPlayerTarget(npcPlayer, entity);
            }
            catch { sendDebug("ppoof"); }*/
            //if (entity is BasePlayer)
            //return true;
            //
        }

        void OnPlayerHealthChange(BasePlayer player, float oldValue, float newValue)
        {
            try
            {
                if (getMinigamer(player).isInvulnerable) player.Heal(100.0f);
            }
            catch { }
        }

        void Loaded()
        {
            if (isLoaded) OnServerInitialized();
        }
        void Unload()
        {
            foreach (var game in Games)
            {
                try
                {
                    for (int i = 0; i <= game.triggers.Count; i++)
                    {
                        game.triggers[i].deconstruct();
                        Puts("Destroyed trigger");
                    }
                }
                catch { }
            }
        }

        void OnPlayerSpawn(BasePlayer player)
        {
            foreach (var Game in Games)
            {
                Game.initData(player);
            }
        }

        void OnVendingTransaction(VendingMachine machine, BasePlayer buyer, int sellOrderId, int numberOfTransactions)
        {
            getMinigamer(buyer).game.OnVendingTransaction(machine, buyer, sellOrderId, numberOfTransactions);
        }

        object OnPlayerRespawn(BasePlayer player)
        {
            try
            {
                BasePlayer.SpawnPoint spawnPoint = getMinigamer(player).getSpawn();
                return spawnPoint;
            }
            catch
            {
                //Minigamers.Add(new Minigamer(player, Games[0]));
                BasePlayer.SpawnPoint spawnPoint = Games[0].getPlayerSpawn();
                return spawnPoint;
            }

        }

        void OnPlayerRespawned(BasePlayer player)
        {
            try
            {
                getMinigamer(player).game.OnPlayerRespawn(player);
            }
            catch
            {
                Games[0].OnPlayerRespawn(player);
            }
        }

        void OnPlayerInit(BasePlayer player)
        {
            Minigamers.Add(new Minigamer(player, Games[0]));
            getMinigamer(player).game.playerJoinGame(player);
            PrintToChat(string.Format(Lang["PlayerJoinedServer"], player.displayName));
            Puts("Player" + player.displayName + " joined server");
            Puts(Minigamers.ToSentence());
        }

        /*void OnPlayerSleepEnded(BasePlayer player)
        {
            try
            {
                
            }
            catch
            {
                Minigamers.Add(new Minigamer(player, Games[0]));
                PrintToChat(string.Format(Lang["PlayerJoinedServer"], player.displayName));
                //getMinigamer(player).game.playerJoinGame(player);
            }
            Puts(Minigamers.ToSentence());
        }*/

        void OnPlayerDisconnected(BasePlayer player)
        {
            getMinigamer(player).game.playerLeaveGame(player);
            Minigamers.Remove(getMinigamer(player));
            PrintToChat(string.Format(Lang["PlayerLeftServer"], player.displayName));
            Puts("Player" + player.displayName + " left server");
            Puts(Minigamers.ToSentence());
        }

        void OnPlayerDie(BasePlayer player, HitInfo info)
        {
            try
            {
                getMinigamer(player).game.OnPlayerDie(player, info);
            }
            catch
            {
                try
                {
                    getMinigamer(info.InitiatorPlayer).game.OnPlayerDie(player, info);
                }
                catch { }
            }

        }

        #endregion

        #endregion

        #region PvPArena

        #region Variables

        static redeem[] redeems;

        #endregion

        #region Commands

        [ChatCommand("Redeem")]
        void ChatCommandRedeem(BasePlayer player, string cmd, string[] args)
        {
            getMinigamer(player).game.runCmd(player, cmd, args);
        }

        [ChatCommand("Class")]
        void ChatCommandClass(BasePlayer player, string cmd, string[] args)
        {
            getMinigamer(player).game.runCmd(player, cmd, args);
        }

        #endregion

        #region Methods

        #endregion

        #endregion

        #region SurvivalArena

        #region Methods

        public void RunGame(float time, SurvivalGame game)
        {
            sendDebug(string.Format(Lang["DebugWave"], game.waveCount, time));
            timer.Repeat(time, 1, () =>
            {
                if (game.isGame)
                {
                    game.Wave();
                }

            }
            );
        }

        #endregion

        #region Classes

        public class Buyable
        {
            public int buyItemID;
            Vector3 pos;
            Quaternion rot;
            BaseEntity entity;
            string prefab;
            public Buyable(Vector3 pos, Quaternion rot, string prefab = "assets/bundled/prefabs/static/door.hinged.security.blue.prefab", int buyItemID = -484206264)
            {
                this.buyItemID = buyItemID;
                this.pos = pos;
                this.rot = rot;
                this.prefab = prefab;
            }
            public void spawn()
            {
                entity = GameManager.server.CreateEntity(prefab, pos, rot, true);
                entity.Spawn();
            }
            public void buy()
            {
                entity.transform.position = new Vector3(0.0f, 1.0f, 0.0f);
                entity.SendNetworkUpdate();
            }
            public void reset()
            {
                entity.transform.position = new Vector3(pos.x, pos.y, pos.z);
                entity.SendNetworkUpdate();
            }
        }

        #endregion

        #endregion
    }
}