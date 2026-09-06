using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Media;
using Steamworks;
using System.Text.RegularExpressions;

namespace DS3ConnectionInfo
{
    public class Player
    {



        private static Dictionary<CSteamID, Player> activePlayers = new Dictionary<CSteamID, Player>();

        private P2PSessionState_t sessionState;

        public CSteamID SteamID { get; private set; }
        public string SteamName { get; private set; }
        public ulong NetId { get; private set; }
        private string region;
        public string Region { get => region == "[STEAM RELAY]" ? UiText.Current["SteamRelay"] : region; private set => region = value; }
        public string CharSlot { get; private set; }
        public string CharName { get; private set; }
        public int TeamId { get; private set; }

        public PlayerAttributes Attributes { get; private set; }
        public int? Level => Attributes?.Level;
        public int? Vigor => Attributes?.Vigor;
        public int? Attunement => Attributes?.Attunement;
        public int? Endurance => Attributes?.Endurance;
        public int? Strength => Attributes?.Strength;
        public int? Dexterity => Attributes?.Dexterity;
        public int? Intelligence => Attributes?.Intelligence;
        public int? Faith => Attributes?.Faith;
        public string Health => Attributes?.Health ?? "—";

        public string TeamName => Team.GetTeamFromId(TeamId).Name;
        public TeamAllegiance TeamAlliegance => Team.GetTeamFromId(TeamId).Allegiance;
        public ulong SteamId64 => SteamID.m_SteamID;
        public double Ping => ETWPingMonitor.GetPing(NetId);
        public double AveragePing => ETWPingMonitor.GetAveragePing(NetId);
        public double Jitter => ETWPingMonitor.GetJitter(NetId);
        public double LatePacketRatio => ETWPingMonitor.GetLatePacketRatio(NetId);

        public SolidColorBrush SteamNameColor => new SolidColorBrush((Color)ColorConverter.ConvertFromString(
            (CharSlot == "") ? Settings.Default.ConnectingColor : "#FFFFFFFF"));
        public SolidColorBrush TeamColor => new SolidColorBrush((Color)ColorConverter.ConvertFromString(Team.GetTeamFromId(TeamId).Color));

        public string OverlayName => GetOverlayName();
        private string GetOverlayName()
        {
            string[] keyNames = new string[2] { "SteamName", "CharName" };
            string fmt = (CharSlot == "") ? Settings.Default.NameFormatConnecting : Settings.Default.NameFormat;
            return FormatUtils.NamedFormat(fmt, keyNames, SteamName, CharName);
        }

        public string PingColor
        {
            get
            {
                switch (Ping)
                {
                    case -1:
                        return Settings.Default.TextColor;
                    case double n when (n <= 50):
                        return Settings.Default.PingColor1;
                    case double n when (n <= 100):
                        return Settings.Default.PingColor2;
                    case double n when (n <= 200):
                        return Settings.Default.PingColor3;
                    default:
                        return Settings.Default.PingColor4;
                }
            }
        }

        private Player(CSteamID steamID)
        {
            SteamID = steamID;
            SteamName = SteamFriends.GetFriendPersonaName(steamID);

            NetId = 0;
            sessionState = new P2PSessionState_t();

            CharSlot = "";
            TeamId = -1;
            CharName = "";
        }

        private void UpdateNetInfo(P2PSessionState_t session)
        {
            bool endpointChanged = sessionState.m_nRemoteIP != session.m_nRemoteIP || sessionState.m_nRemotePort != session.m_nRemotePort;
            sessionState = session;

            if (endpointChanged)
            {
                // If IP/port changed for whatever reason
                ETWPingMonitor.Unregister(NetId);

                Region = "...";

                byte[] ipBytes = BitConverter.GetBytes(sessionState.m_nRemoteIP).Reverse().ToArray();
                NetId = (ulong)sessionState.m_nRemotePort << 32 | BitConverter.ToUInt32(ipBytes, 0);
                ETWPingMonitor.Register(NetId);

                if (session.m_bUsingRelay == 0)
                    IpLocationAPI.GetLocationAsync(new IPAddress(ipBytes).ToString(), r => Region = r);
                else
                    Region = "[STEAM RELAY]";
            }
        }

        public static IEnumerable<Player> ActivePlayers()
        {
            return activePlayers.Values.AsEnumerable();
        }

        public static void UpdateInGameInfo()
        {
            // Invalidate first: disconnected/unloaded/reused slots must never retain old values.
            foreach (Player player in activePlayers.Values)
            {
                player.CharSlot = "";
                player.CharName = "";
                player.TeamId = -1;
                player.Attributes = null;
            }
            for (int slot = 1; slot <= 5; slot++)
            {
                try
                {
                    long character = DS3Interop.GetPlayerBase(slot);
                    if (character == 0) continue;
                    CSteamID id = DS3Interop.GetTruePlayerSteamId(slot);
                    if (!activePlayers.TryGetValue(id, out Player player)) continue;
                    string name = DS3Interop.GetPlayerName(slot);
                    int team = DS3Interop.GetPlayerTeam(slot);
                    PlayerAttributes attributes = null;
                    try { attributes = DS3Interop.GetPlayerAttributes(character); }
                    catch (UnauthorizedAccessException) { }
                    // Reject reads spanning a leave/join transition, even if the slot was reused.
                    if (DS3Interop.GetPlayerBase(slot) != character ||
                        DS3Interop.GetTruePlayerSteamId(slot) != id) continue;
                    player.CharSlot = slot.ToString();
                    player.CharName = name;
                    player.TeamId = team;
                    player.Attributes = attributes;
                }
                catch (Exception) { } // Network and character lifetimes are independent.
            }
        }

        public static void UpdatePlayerList()
        {
            // There's probably a better way to get all current active P2P connections,
            // but I couldn't find one. The SessionInfo pointers are not reliable as 
            // players can spoof their Steam ID there.
            int cnt = SteamFriends.GetCoplayFriendCount();
            for (int i = 0; i < cnt; i++)
            {
                CSteamID id = SteamFriends.GetCoplayFriend(i);

                P2PSessionState_t session = new P2PSessionState_t();
                if (!SteamNetworking.GetP2PSessionState(id, out session) || (session.m_bConnectionActive == 0 && session.m_bConnecting == 0))
                {
                    if (activePlayers.ContainsKey(id))
                    {
                        ETWPingMonitor.Unregister(activePlayers[id].NetId);
                        activePlayers.Remove(id);
                    }
                    continue;
                }
                if (!activePlayers.ContainsKey(id))
                    activePlayers[id] = new Player(id);
                
                activePlayers[id].UpdateNetInfo(session);
            }
        }
    }
}
