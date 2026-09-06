using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DS3ConnectionInfo
{
    /// <summary>
    /// "Loyalty" refers to who that individual is not allowed to hurt, "Mission" refers to who the individual is incentivized to hurt
    /// </summary>
    public enum TeamAllegiance
    {
        Host,       /// Loyal to host, mission to keep host alive until and through critical battle
        Protector,  /// Loyal to host, mission to keep host alive until invaders are dispatched
        Mad,        /// Loyal to none, mission to kill the host or any phantom
        Invader,    /// Loyal to none, mission to kill the host
        Defender,   /// Loyal to each other, mission to kill host
        Enemy,      /// Loyal to each other, mission to kill host and those loyal, or any invader if Seed of a Giant used
        Unknown     /// Loyalty and mission unidentified, complicated, or depends on host actions
    }
    
    public class Team
    {
        private string name;
        public string Name => UiText.Current[name];
        public TeamAllegiance Allegiance { get; private set; }
        public string Color => colors[Allegiance];
        
        private static readonly Dictionary<int, Team> teams = new Dictionary<int, Team>()
        {
            {1,  new Team("Team0",                                       TeamAllegiance.Host) },
            {2,  new Team("Team1",                                    TeamAllegiance.Host) },
            {3,  new Team("Team2",                              TeamAllegiance.Invader) },
            {4,  new Team("Team3",                                     TeamAllegiance.Host) },
            {6,  new Team("Team4",                                      TeamAllegiance.Enemy) },
            {7,  new Team("Team5",                  TeamAllegiance.Enemy) },
            {8,  new Team("Team6",                                     TeamAllegiance.Host) },
            {9,  new Team("Team7",                                TeamAllegiance.Enemy) },
            {10, new Team("Team8",                                 TeamAllegiance.Enemy) },
            {11, new Team("Team9",                                 TeamAllegiance.Unknown) },
            {12, new Team("Team10",                               TeamAllegiance.Unknown) },
            {13, new Team("Team11",                                     TeamAllegiance.Unknown) },
            {16, new Team("Team12",                                TeamAllegiance.Invader) },
            {17, new Team("Team13",                         TeamAllegiance.Defender) },
            {18, new Team("Team14",                           TeamAllegiance.Defender) },
            {24, new Team("Team15",                                TeamAllegiance.Unknown) },
            {26, new Team("Team16",                                        TeamAllegiance.Unknown) },
            {27, new Team("Team17",                                TeamAllegiance.Unknown) },
            {29, new Team("Team18",                                      TeamAllegiance.Unknown) },
            {31, new Team("Team19",                                TeamAllegiance.Mad) },
            {32, new Team("Team20",                                 TeamAllegiance.Mad) },
            {33, new Team("Team21",   TeamAllegiance.Enemy) },
            {0,  new Team("Team22",                                       TeamAllegiance.Unknown) }
        };
        
        private static readonly Dictionary<TeamAllegiance, string> colors = new Dictionary<TeamAllegiance, string>()
        {
            { TeamAllegiance.Host,      "#FFFFFFFF" },
            { TeamAllegiance.Protector, "#FF0000FF" },
            { TeamAllegiance.Mad,       "#FFC71585" },
            { TeamAllegiance.Invader,   "#FFFF0000" },
            { TeamAllegiance.Defender,  "#FF7B68EE" },
            { TeamAllegiance.Enemy,     "#FF006400" },
            { TeamAllegiance.Unknown,   "#FFFFA500" },
        };
        
        private Team(string name, TeamAllegiance allegiance)
        {
            this.name = name;
            Allegiance = allegiance;
        }
        
        public static Team GetTeamFromId(int id)
        {
            return teams.ContainsKey(id) ? teams[id] : teams[0];
        }
    }
}
