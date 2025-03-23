namespace AncientMountain.Managed.Data
{
    /// <summary>
    /// Defines Player Unit Type (Player,PMC,Scav,etc.)
    /// </summary>
    public enum WebPlayerType : int
    {
        /// <summary>
        /// AI-controlled Bot.
        /// </summary>
        Bot = 0,
        /// <summary>
        /// LocalPlayer running the Web Server.
        /// </summary>
        LocalPlayer = 1,
        /// <summary>
        /// Teammate of LocalPlayer.
        /// </summary>
        Teammate = 2,
        /// <summary>
        /// Human-Controlled Player (PMC).
        /// </summary>
        Player = 3,
        /// <summary>
        /// Human-Controlled Scav.
        /// </summary>
        PlayerScav = 4,

        // ✅ Added new types for Boss, Guard, Rogue, and Follower

        /// <summary>
        /// Scav Raiders & PvE Usec.
        /// </summary>
        Raider = 5,

        /// <summary>
        /// Rogues (Ex-USEC)
        /// </summary>
        Rogue = 6,

        /// <summary>
        /// Named Bosses (Tagilla, Killa, Reshala, etc.).
        /// </summary>
        Boss = 7,

        /// <summary>
        /// Cultists (Sektants).
        /// </summary>
        Cultist = 17,

        /// <summary>
        /// Guards / Followers of Bosses (Reshala Guards, Glukhar Guards, etc.).
        /// </summary>
        Guard = 14,

        /// <summary>
        /// Other Named Followers (Morana followers, Kollontay Guards).
        /// </summary>
        Follower = 15
    }
}
