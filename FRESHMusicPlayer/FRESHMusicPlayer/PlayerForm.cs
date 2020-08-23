using DiscordRPC;
using NAudio.Wave;
using Squirrel;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;
using FRESHMusicPlayer.Handlers;
using FRESHMusicPlayer.Utilities;
using System.Net.Http;
namespace FRESHMusicPlayer
{
    public partial class PlayerForm : Form
    {
        public static readonly Player Player = new Player();
        public static bool avoidnextqueue { get => Player.AvoidNextQueue; set => Player.AvoidNextQueue = value; }
        public static DiscordRpcClient client;

        //public static int position;
        public static float currentvolume { get => Player.CurrentVolume; set => Player.CurrentVolume = value; }
        public static string filePath { get => Player.FilePath; set => Player.FilePath = value; }
        public static bool playing { get => Player.Playing; set => Player.Playing = value; }
        public static bool paused { get => Player.Paused; set => Player.Paused = value; }

        public static bool RepeatOnce { get => Player.RepeatOnce; set => Player.RepeatOnce = value; }
        public static bool Shuffle { get => Player.Shuffle; set => Player.Shuffle = value; }

        private static List<string> Queue => Player.Queue;
        public static int QueuePosition { get => Player.QueuePosition; set => Player.QueuePosition = value; }

        public static DateTime lastUpdateCheck;
        /// <summary>
        /// Raised whenever a new track is being played.
        /// </summary>
        public static event EventHandler songChanged;
        public static event EventHandler songStopped;
        public static event EventHandler<PlaybackExceptionEventArgs> songException;
        
        static PlayerForm()
        {
            Player.SongChanged += songChanged;
            Player.SongStopped += songStopped;
            Player.SongException += songException;
        }
        public PlayerForm()
        {
            InitializeComponent();
            UserInterface userInterface = new UserInterface();  // Show your UI form here.
            userInterface.Show();                               // Note: The UI form must close the application when it closes, else the program will stay opened.
        }
        #region CoreFMP
        // Queue System
        /// <summary>
        /// Adds a track to the <see cref="queue"/>.
        /// </summary>
        /// <param name="filePath">The file path to the track to add.</param>
        public static void AddQueue(string filePath)
        {
            Player.AddQueue(filePath);
        }
        public static void AddQueue(string[] filePaths)
        {
            Player.AddQueue(filePaths);
        }
        public static void ClearQueue() => Player.ClearQueue();
        public static List<string> GetQueue()
        {
            return Queue;
        }
        /// <summary>
        /// Skips to the previous track in the queue. If there are no tracks for the player to go back to, nothing will happen.
        /// </summary>
        public static void PreviousSong()
        {
            Player.PreviousSong();
        }
        /// <summary>
        /// Skips to the next track in the queue. If there are no more tracks, the player will stop.
        /// </summary>
        /// <param name="avoidnext">Intended to be used only by the player</param>
        public static void NextSong(bool avoidnext=false)
        {
            Player.NextSong();
        }
        // Music Playing Controls
        /// <summary>
        /// Repositions the playback position of the Player.
        /// </summary>
        /// <param name="seconds">The position in to the track to skip in, in seconds.</param>
        public static void RepositionMusic(int seconds)
        {
            Player.RepositionMusic(seconds);
        }

        /// <summary>
        /// Starts playing the queue. In order to play a track, you must first add it to the queue using <see cref="AddQueue(string)"/>.
        /// </summary>
        /// <param name="repeat">If true, avoids dequeuing the next track. Not to be used for anything other than the Player.</param>
        public static void PlayMusic(bool repeat=false)
        {           
            Player.PlayMusic(repeat);            
        }
        /// <summary>
        /// Completely stops and disposes the player and resets all playback related variables to their defaults.
        /// </summary>
        public static void StopMusic()
        {
            Player.StopMusic();
        }
        /// <summary>
        /// Pauses playback without disposing. Can later be resumed with <see cref="ResumeMusic()"/>.
        /// </summary>
        public static void PauseMusic()
        {
            Player.PauseMusic();
        }// Pauses the music without completely disposing it
        /// <summary>
        /// Resumes playback.
        /// </summary>
        public static void ResumeMusic()
        {
            Player.ResumeMusic();
        }// Resumes music that has been paused
        /// <summary>
        /// Updates the volume of the player during playback to the value of <see cref="currentvolume"/>.
        /// Even if you don't call this, the volume of the player will update whenever the next track plays.
        /// </summary>
        public static void UpdateSettings()
        {
            Player.UpdateSettings();
        }
        // Other Logic Stuff
        /// <summary>
        /// Returns a formatted string of the current playback position.
        /// </summary>
        /// <returns></returns>
        public static string getSongPosition()
        {
            //if (playing) // Only work if music is currently playing
            //if (!positiononly) position += 1; // Only tick up the position if it's being called from UserInterface
            
            string Format(int secs)
            {
                int hours = 0;
                int mins = 0;

                while (secs >= 60)
                {
                    mins++;
                    secs -= 60;
                }

                while (mins >= 60)
                {
                    hours++;
                    mins -= 60;
                }

                string hourStr = hours.ToString(); if (hourStr.Length < 2) hourStr = "0" + hourStr;
                string minStr = mins.ToString(); if (minStr.Length < 2) minStr = "0" + minStr;
                string secStr = secs.ToString(); if (secStr.Length < 2) secStr = "0" + secStr;

                string durStr = "";
                if (hourStr != "00") durStr += hourStr + ":";
                durStr = minStr + ":" + secStr;

                return durStr;
            }

            //ATL.Track theTrack = new ATL.Track(filePath);
            var length = Player.CurrentBackend.TotalTime;
            
            return $"{Format((int)Player.CurrentBackend.CurrentTime.TotalSeconds)} / {Format((int)length.TotalSeconds)}";
        }
        #endregion
        // Integration
        #region DiscordRPC
        public static void InitDiscordRPC()
        {
            /*
                Create a discord client
                NOTE: 	If you are using Unity3D, you must use the full constructor and define
                         the pipe connection.
                */
            client = new DiscordRpcClient("656678380283887626");

            //Set the logger
            //client.Logger = new ConsoleLogger() { Level = Discord.LogLevel.Warning };

            //Subscribe to events
            client.OnReady += (sender, e) =>
            {
                Console.WriteLine("Received Ready from user {0}", e.User.Username);
            };

            client.OnPresenceUpdate += (sender, e) =>
            {
                Console.WriteLine("Received Update! {0}", e.Presence);
            };

            //Connect to the RPC
            client.Initialize();

            //Set the rich presence
            //Call this as many times as you want and anywhere in your code.
        }
        public static void UpdateRPC(string Activity, string Artist = null, string Title = null)
        {
            client?.SetPresence(new RichPresence()
            {
                Details = Title,
                State = $"by {Artist}",
                Assets = new Assets()
                {
                    LargeImageKey = "icon",
                    SmallImageKey = Activity
                },
                Timestamps = Timestamps.Now
            }
            );

        }
        public static void DisposeRPC() => client?.Dispose();
        #endregion
        #region Squirrel
        public static async Task UpdateIfAvailable()
        {
            
            updateInProgress = RealUpdateIfAvailable();
            await updateInProgress;
        }

        public static async Task WaitForUpdatesOnShutdown()
        {
            // We don't actually care about errors here, only completion
            await updateInProgress.ContinueWith(ex => { });
        }

        public static Task updateInProgress = Task.FromResult(true);
        private static async Task RealUpdateIfAvailable(bool useDeltaPatching = true)
        {
            Properties.Settings.Default.General_LastUpdate = DateTime.Now;
            Properties.Settings.Default.Save();
            var mgr = await UpdateManager.GitHubUpdateManager("https://github.com/Royce551/FRESHMusicPlayer", prerelease:Properties.Settings.Default.General_PreRelease);
            try
            {
                UpdateInfo updateInfo = await mgr.CheckForUpdate(!useDeltaPatching);
                if (updateInfo.CurrentlyInstalledVersion == null) return; // Standalone version of FMP, don't bother
                if (updateInfo.ReleasesToApply.Count == 0) return; // No updates to apply, don't bother
                DialogResult dialogResult = MessageBox.Show("A new update for FMP is available. Do you want to update now?", "Update Available", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                if (dialogResult == DialogResult.Yes)
                {
                    
                    await mgr.DownloadReleases(updateInfo.ReleasesToApply);
                    await mgr.ApplyReleases(updateInfo);
                    ShutdownTheApp();
                }
                else return; 
            }
            catch (Exception e)
            { 
                if (useDeltaPatching)
                {
                    await RealUpdateIfAvailable(false);
                }
                else MessageBox.Show($"An error occured during the update process \n (Info - {e.Message}). \n If you are using a pre-release version, no need to worry. If not, let me know.", "Update Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            finally
            {
                mgr.Dispose();        
            }
        }
        

        /// Now, in your shutdown code

        public static async void ShutdownTheApp()
        {

            await WaitForUpdatesOnShutdown();

            Application.Exit();
        }
        #endregion
        private void Player_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason != CloseReason.ApplicationExitCall)
                Application.Exit();
        }
    }
}
