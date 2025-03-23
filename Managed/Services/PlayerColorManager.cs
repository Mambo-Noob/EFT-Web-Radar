using System.Collections.Generic;
using AncientMountain.Managed.Services;
using SkiaSharp;

namespace AncientMountain.Managed.Data
{
    public static class PlayerColorManager
    {
        private static readonly Dictionary<string, WebPlayerType> _playerNameMapping = new()
        {
            // 🟠 Bosses
            { "Big Pipe", WebPlayerType.Boss }, { "Birdeye", WebPlayerType.Boss }, 
            { "Glukhar", WebPlayerType.Boss }, { "Kaban", WebPlayerType.Boss }, 
            { "Killa", WebPlayerType.Boss }, { "Raider", WebPlayerType.Boss }, 
            { "Reshala", WebPlayerType.Boss }, { "Sanitar", WebPlayerType.Boss }, 
            { "Shturman", WebPlayerType.Boss }, { "Tagilla", WebPlayerType.Boss },
            { "Partisan", WebPlayerType.Boss }, { "Knight", WebPlayerType.Boss },

            // 🔴 Cultists
            { "Sektant", WebPlayerType.Cultist },

            // 🟢 Glukhar Guards
            { "Afganec", WebPlayerType.Guard }, { "Alfons", WebPlayerType.Guard }, 
            { "Assa", WebPlayerType.Guard },

            // 🔵 Kaban Guards
            { "Baklazhan", WebPlayerType.Guard }, { "Banovyy", WebPlayerType.Guard }, 
            { "Barmaley", WebPlayerType.Guard },

            // 🔴 Reshala Guards
            { "Anton Zavodskoy", WebPlayerType.Guard }, { "Damirka Zavodskoy", WebPlayerType.Guard }, 
            { "Filya Zavodskoy", WebPlayerType.Guard },

            // 🟠 Rogues
            { "Afraid", WebPlayerType.Rogue }, { "Aimbotkin", WebPlayerType.Rogue }, 
            { "Applejuice", WebPlayerType.Rogue },

            // 🟣 Sanitar Guards
            { "Akusher", WebPlayerType.Guard }, { "Anesteziolog", WebPlayerType.Guard }, 
            { "Dermatolog", WebPlayerType.Guard },

            // 🟡 Scav Raiders
            { "Akula", WebPlayerType.Raider }, { "BZT", WebPlayerType.Raider }, 
            { "Balu", WebPlayerType.Raider }, { "Bankir", WebPlayerType.Raider },

            // 🟣 Shturman Guards
            { "Dimon Svetloozerskiy", WebPlayerType.Follower }, { "Enchik Svetloozerskiy", WebPlayerType.Follower },

            // ⚫ PvE Usec
            { "Myo", WebPlayerType.Raider }, { "JorickGP", WebPlayerType.Raider }, 
            { "TicTak", WebPlayerType.Raider }, { "Mackcik", WebPlayerType.Raider }
        };

        private static readonly Dictionary<WebPlayerType, SKColor> _playerColors = new()
        {
            { WebPlayerType.Boss, SKColors.Orange },  // Bosses
            { WebPlayerType.Cultist, SKColors.Red },  // Cultists
            { WebPlayerType.Guard, SKColors.Purple }, // Guards
            { WebPlayerType.Rogue, SKColors.OrangeRed }, // Rogues
            { WebPlayerType.Raider, SKColors.Yellow },  // Scav Raiders
            { WebPlayerType.Follower, SKColors.Blue }  // Named Followers
        };

        public static WebPlayerType GetPlayerType(string playerName)
        {
            return _playerNameMapping.TryGetValue(playerName, out var playerType) ? playerType : WebPlayerType.Bot;
        }

        public static SKColor GetPlayerColor(string playerName)
        {
            var type = GetPlayerType(playerName);
            return _playerColors.TryGetValue(type, out var color) ? color : SKColors.White;
        }
    }
}
