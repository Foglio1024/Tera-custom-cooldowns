﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TCC.Data;
using TCC.Data.Databases;
using TCC.Data.Pc;
using TCC.Interop;
using TCC.Interop.Proxy;
using TCC.Parsing;
using TCC.TeraCommon.Sniffing.Crypt;
using TCC.ViewModels;
using TCC.Windows;
using TeraDataLite;
using TeraPacketParser.Messages;
using Player = TCC.Data.Pc.Player;
using Server = TCC.TeraCommon.Game.Server;

namespace TCC
{
    public class Account
    {
        public bool IsElite { get; set; }
        public List<Character> Characters { get; set; }
    }

    public static class Session
    {
        public const int MaxWeekly = 16;
        public const int MaxDaily = 16;
        public const int MaxGuardianQuests = 40;
        private static bool _logged;
        private static bool _loadingScreen = true;
        private static bool _encounter;
        private static bool _inGameChatOpen;
        private static bool _inGameUiOn;

        public static Server Server { get; set; }
        public static Account Account { get; set; } = new Account();
        public static string Language => DB.ServerDatabase.StringLanguage;
        public static bool LoadingScreen
        {
            get => _loadingScreen;
            set
            {
                if (_loadingScreen == value) return;
                _loadingScreen = value;
                //WindowManager.NotifyVisibilityChanged();
                App.BaseDispatcher.Invoke(() => LoadingScreenChanged?.Invoke());

            }
        }
        public static bool Encounter
        {
            get => _encounter;
            set
            {
                if (_encounter == value) return;
                _encounter = value;
                App.BaseDispatcher.Invoke(() => EncounterChanged?.Invoke());
            }
        }
        public static bool Combat
        {
            get => Me?.IsInCombat ?? false;
            set
            {
                if (Combat == value) return;
                Me.IsInCombat = value;
                App.BaseDispatcher.Invoke(() => CombatChanged?.Invoke()); // check logs for other exceptions here
            }
        }
        public static bool Logged
        {
            get => _logged;
            set
            {
                if (_logged == value) return;
                _logged = value;
                App.BaseDispatcher.Invoke(() => LoggedChanged?.Invoke());
            }
        }
        public static bool InGameUiOn
        {
            get => _inGameUiOn;
            set
            {
                if (_inGameUiOn == value) return;
                _inGameUiOn = value;
                GameUiModeChanged?.Invoke();
            }
        }
        public static bool InGameChatOpen
        {
            get => _inGameChatOpen;
            set
            {
                if (_inGameChatOpen == value) return;
                _inGameChatOpen = value;
                ChatModeChanged?.Invoke();
            }
        }

        public static int CurrentZoneId { get; private set; } = 0;

        public static bool IsMe(ulong eid)
        {
            return eid == Me.EntityId;
        }

        public static event Action ChatModeChanged;
        public static event Action GameUiModeChanged;
        public static event Action EncounterChanged;
        public static event Action CombatChanged;
        public static event Action LoadingScreenChanged;
        public static event Action LoggedChanged;
        public static event Action DatabaseLoaded;
        public static event Action Teleported;

        public static Player Me;

        public static readonly Dictionary<uint, string> GuildMembersNames = new Dictionary<uint, string>();

        public static string GetGuildMemberName(uint id)
        {
            return GuildMembersNames.TryGetValue(id, out var name) ? name : "Unknown player";
        }
        public static TccDatabase DB { get; set; }
        public static bool CivilUnrestZone => CurrentZoneId == 152;
        public static bool IsInDungeon => CurrentZoneId >= 8999;
        public static string CurrentAccountName { get; internal set; }

        public static void SetPlayerHp(float hp)
        {
            Me.CurrentHP = hp;
        }
        public static void SetPlayerMp(float mp)
        {
            Me.CurrentMP = mp;
        }
        public static void SetPlayerSt(float st)
        {
            Me.CurrentST = st;
            if (App.Settings.ClassWindowSettings.Enabled) WindowManager.ViewModels.Class.CurrentManager.SetST(Convert.ToInt32(st));
        }
        public static void SetPlayerFe(float en)
        {
            Me.FlightEnergy = en; //useless
            WindowManager.FlightDurationWindow.SetEnergy(en); // direct window reference lul
        }
        public static void SetPlayerLaurel(Player p)
        {
            try
            {
                p.Laurel = WindowManager.ViewModels.Dashboard.Characters.First(x => x.Name == p.Name).Laurel;
            }
            catch
            {
                p.Laurel = Laurel.None;
            }
        }

        public static void SetPlayerMaxHp(long maxHp)
        {
            Me.MaxHP = maxHp;
            //ClassManager.SetMaxHP(Convert.ToInt32(maxHp));
        }
        public static void SetPlayerMaxMp(int maxMp)
        {
            Me.MaxMP = maxMp;
            //ClassManager.SetMaxMP(Convert.ToInt32(maxMp));
        }
        public static void SetPlayerMaxSt(int maxSt)
        {
            Me.MaxST = maxSt;
            if (App.Settings.ClassWindowSettings.Enabled) WindowManager.ViewModels.Class.CurrentManager.SetMaxST(Convert.ToInt32(maxSt));
        }

        public static void SetPlayerShield(uint damage)
        {
            if (Me.CurrentShield < 0) return;
            //CurrentPlayer.CurrentShield -= damage;
            Me.DamageShield(damage);
        }
        //public static void SetPlayerMaxShield(uint shield)
        //{
        //    CurrentPlayer.MaxShield = shield;
        //    CurrentPlayer.CurrentShield = shield;
        //}
        public static async void InitDatabasesAsync(string lang)
        {
            await Task.Factory.StartNew(() => InitDatabases(lang));
            DatabaseLoaded?.Invoke();
        }

        public static void InitDatabases(string lang)
        {
            DB = new TccDatabase(lang);
            DB.CheckVersion();
            if (!DB.IsUpToDate)
            {
                var res = TccMessageBox.Show($"Some database files may be missing or out of date.\nDo you want to update them?", MessageBoxType.ConfirmationWithYesNo);
                if (res == System.Windows.MessageBoxResult.Yes)
                {
                    DB.DownloadOutdatedDatabases();
                }
            }
            if (!DB.Exists)
            {
                var res = TccMessageBox.Show($"Unable to load database for language '{lang}'. \nThis could be caused by a wrong Language override value or corrupted TCC download.\n\n Do you want to open settings and change it?\n\n Choosing 'No' will load EU-EN database,\nchoosing 'Cancel' will close TCC.", MessageBoxType.ConfirmationWithYesNoCancel);
                if (res == System.Windows.MessageBoxResult.Yes)
                {
                    App.BaseDispatcher.Invoke(() =>
                    {
                        WindowManager.SettingsWindow.TabControl.SelectedIndex = 8;
                        WindowManager.SettingsWindow.ShowDialog();
                    });
                    InitDatabases(App.Settings.LastLanguage);
                }
                else if (res == System.Windows.MessageBoxResult.No) InitDatabases("EU-EN");
                else if (res == System.Windows.MessageBoxResult.Cancel) App.Close();
            }
            else DB.Load();
        }

        public static void InstallHooks()
        {
            // db stuff

            PacketAnalyzer.NewProcessor.Hook<C_LOGIN_ARBITER>(m =>
            {
                CurrentAccountName = m.AccountName;
                DB.ServerDatabase.Language = m.Language;
            });

            // player stuff

            PacketAnalyzer.NewProcessor.Hook<S_LOGIN>(m =>
            {
                Firebase.RegisterWebhook(App.Settings.WebhookUrlGuildBam, true);
                Firebase.RegisterWebhook(App.Settings.WebhookUrlFieldBoss, true);
                Firebase.SendUsageStatAsync();

                App.Settings.LastLanguage = Language;

                Logged = true;
                LoadingScreen = true;
                Encounter = false;
                GuildMembersNames.Clear();
                Server = DB.ServerDatabase.GetServer(m.ServerId);

                Me.Name = m.Name;
                Me.Class = m.CharacterClass;
                Me.EntityId = m.EntityId;
                Me.Level = m.Level;
                Me.PlayerId = m.PlayerId;
                Me.ServerId = m.ServerId;
                Me.Laurel = GetLaurel(Me.PlayerId);

                WindowManager.ReloadPositions();
                TimeManager.Instance.SetServerTimeZone(App.Settings.LastLanguage);
                TimeManager.Instance.SetGuildBamTime(false);
                InitDatabases(App.Settings.LastLanguage);
                AbnormalityManager.SetAbnormalityTracker(m.CharacterClass);
                WindowManager.FloatingButton.SetMoongourdButtonVisibility(); //TODO: do this via vm, need to refactor it first

            });
            PacketAnalyzer.NewProcessor.Hook<S_RETURN_TO_LOBBY>(m =>
            {
                Logged = false;
            });

            // ep

            PacketAnalyzer.NewProcessor.Hook<S_RESET_EP_PERK>(m =>
            {
                if (m.Success) EpDataProvider.SetManaBarrierPerkLevel(0);
            });
            PacketAnalyzer.NewProcessor.Hook<S_LEARN_EP_PERK>(m =>
            {
                if (!m.Perks.TryGetValue(851010, out var level)) return;
                EpDataProvider.SetManaBarrierPerkLevel(level);
            });
            PacketAnalyzer.NewProcessor.Hook<S_LOAD_EP_INFO>(m =>
            {
                if (!m.Perks.TryGetValue(851010, out var level)) return;
                EpDataProvider.SetManaBarrierPerkLevel(level);
            });

            // guild

            PacketAnalyzer.NewProcessor.Hook<S_GUILD_MEMBER_LIST>(m =>
            {
                m.GuildMembersList.ToList().ForEach(g => { GuildMembersNames[g.Key] = g.Value; });
            });
            PacketAnalyzer.NewProcessor.Hook<S_CHANGE_GUILD_CHIEF>(PacketHandler.HandleChangeGuildChief);
            PacketAnalyzer.NewProcessor.Hook<S_NOTIFY_GUILD_QUEST_URGENT>(PacketHandler.HandleNotifyGuildQuestUrgent);
            PacketAnalyzer.NewProcessor.Hook<S_GET_USER_GUILD_LOGO>(p =>
            {
                if (p.GuildLogo == null) return;
                S_IMAGE_DATA.Database[p.GuildId] = p.GuildLogo;

                if (!Directory.Exists("resources/images/guilds")) Directory.CreateDirectory("resources/images/guilds");
                try
                {
                    p.GuildLogo.Save(
                        Path.Combine(App.ResourcesPath, $"images/guilds/guildlogo_{Session.Server.ServerId}_{p.GuildId}_{0}.bmp"),
                        System.Drawing.Imaging.ImageFormat.Bmp
                    );
                }
                catch (Exception e) { Log.F($"Error while saving guild logo: {e}"); }
            });

            // abnormality

            PacketAnalyzer.NewProcessor.Hook<S_ABNORMALITY_BEGIN>(p =>
            {
                if (IsMe(p.TargetId)) FlyingGuardianDataProvider.HandleAbnormal(p);
            });
            PacketAnalyzer.NewProcessor.Hook<S_ABNORMALITY_REFRESH>(p =>
            {
                if (IsMe(p.TargetId)) FlyingGuardianDataProvider.HandleAbnormal(p);
            });
            PacketAnalyzer.NewProcessor.Hook<S_ABNORMALITY_END>(p =>
            {
                if (IsMe(p.TargetId)) FlyingGuardianDataProvider.HandleAbnormal(p);
            });

            // guardian
            PacketAnalyzer.NewProcessor.Hook<S_FIELD_EVENT_ON_ENTER>(p =>
            {
                const string opcode = "SMT_FIELD_EVENT_ENTER";
                DB.SystemMessagesDatabase.Messages.TryGetValue(opcode, out var m);
                SystemMessagesProcessor.AnalyzeMessage("", m, opcode);

                if (!ProxyInterface.Instance.IsStubAvailable || !ProxyInterface.Instance.IsFpsUtilsAvailable || !App.Settings.FpsAtGuardian) return;
                ProxyInterface.Instance.Stub.InvokeCommand("fps mode 3");
            });
            PacketAnalyzer.NewProcessor.Hook<S_FIELD_EVENT_ON_LEAVE>(p =>
            {
                const string opcode = "SMT_FIELD_EVENT_LEAVE";
                DB.SystemMessagesDatabase.Messages.TryGetValue(opcode, out var m);
                SystemMessagesProcessor.AnalyzeMessage("", m, opcode);

                if (!ProxyInterface.Instance.IsStubAvailable || !ProxyInterface.Instance.IsFpsUtilsAvailable || !App.Settings.FpsAtGuardian) return;
                ProxyInterface.Instance.Stub.InvokeCommand("fps mode 1");
            });

            PacketAnalyzer.NewProcessor.Hook<S_USER_STATUS>(m =>
            {
                if (IsMe(m.EntityId)) Combat = m.IsInCombat;
            });
            PacketAnalyzer.NewProcessor.Hook<S_GET_USER_LIST>(m =>
            {
                Logged = false;
                Firebase.RegisterWebhook(App.Settings.WebhookUrlGuildBam, false);
                Firebase.RegisterWebhook(App.Settings.WebhookUrlFieldBoss, false);
            });
            PacketAnalyzer.NewProcessor.Hook<S_LOAD_TOPO>(m =>
            {
                LoadingScreen = true;
                Encounter = false;
                CurrentZoneId = m.Zone;
                Teleported?.Invoke();

            });
            PacketAnalyzer.NewProcessor.Hook<S_ACCOUNT_PACKAGE_LIST>(m =>
            {
                Account.IsElite = m.IsElite;
            });
            PacketAnalyzer.NewProcessor.Hook<S_NOTIFY_TO_FRIENDS_WALK_INTO_SAME_AREA>(PacketHandler.HandleFriendIntoArea);
            PacketAnalyzer.NewProcessor.Hook<S_UPDATE_FRIEND_INFO>(PacketHandler.HandleFriendStatus);
            PacketAnalyzer.NewProcessor.Hook<S_ACCOMPLISH_ACHIEVEMENT>(PacketHandler.HandleAccomplishAchievement);
            PacketAnalyzer.NewProcessor.Hook<S_ANSWER_INTERACTIVE>(PacketHandler.HandleAnswerInteractive);
            PacketAnalyzer.NewProcessor.Hook<S_SYSTEM_MESSAGE_LOOT_ITEM>(PacketHandler.HandleSystemMessageLoot);
            PacketAnalyzer.NewProcessor.Hook<S_SYSTEM_MESSAGE>(PacketHandler.HandleSystemMessage);
            PacketAnalyzer.NewProcessor.Hook<S_SPAWN_ME>(p =>
            {
                EntityManager.ClearNPC();
                FlyingGuardianDataProvider.Stacks = 0;
                FlyingGuardianDataProvider.StackType = FlightStackType.None;
                FlyingGuardianDataProvider.InvokeProgressChanged();
                Task.Delay(2000).ContinueWith(t => // was done with timer before, test it
                {
                    LoadingScreen = false;
                    WindowManager.ForegroundManager.RefreshDim();
                });
            });
            PacketAnalyzer.NewProcessor.Hook<S_SPAWN_USER>(p =>
            {
                switch (p.Name)
                {
                    case "Foglio":
                    case "Foglietto":
                    case "Foglia":
                    case "Myvia":
                    case "Foglietta.Blu":
                    case "Foglia.Trancer":
                    case "Folyria":
                    case "Folyvia":
                    case "Fogliolina":
                    case "Folyemi":
                    case "Foiya":
                    case "Fogliarya":
                        if (p.ServerId != 27) break;
                        if (Session.CivilUnrestZone) break;
                        EntityManager.FoglioEid = p.EntityId;
                        var ab = Session.DB.AbnormalityDatabase.Abnormalities[10241024];
                        AbnormalityManager.BeginAbnormality(ab.Id, Me.EntityId, 0, int.MaxValue, 1);
                        var sysMsg = Session.DB.SystemMessagesDatabase.Messages["SMT_BATTLE_BUFF_DEBUFF"];
                        var msg = $"@0\vAbnormalName\v{ab.Name}";
                        SystemMessagesProcessor.AnalyzeMessage(msg, sysMsg, "SMT_BATTLE_BUFF_DEBUFF");
                        break;
                }
                EntityManager.SpawnUser(p.EntityId, p.Name);
            });
            PacketAnalyzer.NewProcessor.Hook<S_DESPAWN_NPC>(PacketHandler.HandleDespawnNpc);
            PacketAnalyzer.NewProcessor.Hook<S_DESPAWN_USER>(PacketHandler.HandleDespawnUser);

        }

        private static Laurel GetLaurel(uint pId)
        {
            var ch = WindowManager.ViewModels.Dashboard.Characters.FirstOrDefault(x => x.Id == pId);
            return ch?.Laurel ?? Laurel.None;
        }

        public static void SetSorcererElements(bool pFire, bool pIce, bool pArcane)
        {
            Me.Fire = pFire;
            Me.Ice = pIce;
            Me.Arcane = pArcane;

            if (App.Settings.ClassWindowSettings.Enabled
                && Me.Class == Class.Sorcerer
                && WindowManager.ViewModels.Class.CurrentManager is SorcererLayoutVM sm)
            {
                sm.NotifyElementChanged();
            }

        }

        public static void SetSorcererElementsBoost(bool f, bool i, bool a)
        {
            Me.FireBoost = f;
            Me.IceBoost = i;
            Me.ArcaneBoost = a;

            if (App.Settings.ClassWindowSettings.Enabled && Me.Class == Class.Sorcerer)
            {
                TccUtils.CurrentClassVM<SorcererLayoutVM>().NotifyElementBoostChanged();
            }
        }

        public static void Init()
        {
            PacketAnalyzer.ProcessorReady += InstallHooks;
            InitDatabasesAsync(string.IsNullOrEmpty(App.Settings.LastLanguage) ? "EU-EN" : App.Settings.LastLanguage);

        }
    }
}