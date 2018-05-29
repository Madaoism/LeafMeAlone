﻿
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SlimDX;
using Shared;
using System.Diagnostics;

namespace Shared
{

    // Enum for different match types and versions that we want to have.
    public enum MatchType
    {
        // Team match, version 1.
        TEAMS_1,

        // Free for all, version 1.
        FFA_1
    }

    /// <summary>
    /// Match information.
    /// </summary>
    public class Match
    {

        // Public variable for a default match. May be all we ever use.
        // Just reflects the private variable, or creates the default match if it's null.
        public static Match DefaultMatch
        {
            get
            {
                // Check if the private default match is null. If so, create it.
                if (_DefaultMatch == null)
                {
                    CreateDefaultMatch();
                }

                // Return the private default match.
                return _DefaultMatch;
            }
        }

        // Variable for a default match.
        private static Match _DefaultMatch;

        // Number of teams in this match.
        public int numTeams = 2;

        // Sections of the map that belong to teams.
        public List<TeamSection> teamSections;

        private Stopwatch matchTimer = new Stopwatch();

        // Area that doesn't belong to any teams.
        public TeamSection NoMansLand;
        private int matchTime;

        // Type of this match.
        public MatchType matchType;

        public Match()
        {

        }

        /// <summary>
        /// Creates the default match information.
        /// </summary>
        /// <returns>The default match reference.</returns>
        public static Match CreateDefaultMatch()
        {

            // Create a new match.
            Match newMatch = new Match();

            // Total size of no man's land.
            float NoMansLandSize = Constants.MAP_WIDTH * Constants.NO_MANS_LAND_PERCENT;

            // Number of tiles that will make up no man's land.
            int NoMansLandTiles = (int)(NoMansLandSize / Constants.TILE_SIZE);

            // Number of tiles on each side of the center tile, for no man's land.
            int NoMansLandTilesOnEachSide = NoMansLandTiles / 2 - 1;

            // Set up no man's land information.
            newMatch.NoMansLand = new TeamSection
            {

                // Left bound is slightly offset from the map center, to the left. 
                leftX = (0.0f - (Constants.TILE_SIZE / 2.0f)) - (NoMansLandTilesOnEachSide * Constants.TILE_SIZE),

                // Right bound is slightly offset from the map center, to the right.
                rightX = (0.0f + (Constants.TILE_SIZE / 2.0f)) + (NoMansLandTilesOnEachSide * Constants.TILE_SIZE),

                // Upper bound is at the top of the map.
                upZ = (Constants.MAP_HEIGHT / 2.0f),

                // Lower bound is at the bottom of the map.
                downZ = -(Constants.MAP_HEIGHT / 2.0f),

                // Color should be gray (or normal? idk).
                sectionColor = new Vector3(0.7f, 0.7f, 0.7f)
            };

            // Create a new list of team sections.
            newMatch.teamSections = new List<TeamSection>();

            // Create a new section for team one, on the left side of the map.
            newMatch.teamSections.Add(new TeamSection
            {
                leftX = -(Constants.MAP_WIDTH / 2.0f),
                rightX = newMatch.NoMansLand.leftX,
                upZ = (Constants.MAP_HEIGHT / 2.0f),
                downZ = -(Constants.MAP_HEIGHT / 2.0f),
                team = Team.RED,
                // Make the section red
                sectionColor = new Vector3(1.8f, 1.0f, 1.0f)

            });

            // Create a new section for team two, on the right side of the map.
            newMatch.teamSections.Add(new TeamSection
            {
                leftX = newMatch.NoMansLand.rightX,
                rightX = Constants.MAP_WIDTH / 2.0f,
                upZ = Constants.MAP_HEIGHT / 2.0f,
                downZ = -Constants.MAP_HEIGHT / 2.0f,
                team = Team.BLUE,

                // Make the section blue.
                sectionColor = new Vector3(1.0f, 1.0f, 1.8f)
            });

            // Print out the bounds of the match.
            Console.WriteLine(newMatch.teamSections[0]);
            Console.WriteLine(newMatch.teamSections[1]);
            Console.WriteLine(newMatch.NoMansLand);

            newMatch.matchType = MatchType.TEAMS_1;

            // Set the default match and return it.
            _DefaultMatch = newMatch;
            return newMatch;
        }

        /// <summary>
        /// Whether the current match is active or not
        /// </summary>
        /// <returns>Whether the match is running</returns>
        public bool Started()
        {
            return matchTimer.IsRunning;
        }

        /// <summary>
        /// Count the objects in all sections, and store them.
        /// </summary>
        /// <param name="objects">Objects to count.</param>
        public void CountObjectsOnSides(List<GameObject> objects)
        {

            // Iterate through all sections.
            foreach (TeamSection square in teamSections)
            {
                // Count the objects and save them.
                square.CountObjectsInBounds(objects);
            }

            // Count the no man's land objects.
            NoMansLand.CountObjectsInBounds(objects);
        }

        /// <summary>
        /// Get the number of leaves in a team section.
        /// </summary>
        /// <param name="teamIndex">Index of the team to check.</param>
        /// <param name="objects">Objects to check.</param>
        /// <returns>Number of leaves(objects)</returns>
        public int GetTeamLeaves(int teamIndex, List<GameObject> objects)
        {

            if (teamIndex < 0 || teamIndex >= teamSections.Count)
            {
                Console.WriteLine(string.Format("No team exists with index {0}. There are {1} teams, max index is {2}", teamIndex, teamSections.Count, teamSections.Count - 1));
            }
            // Count the objects in that section.
            teamSections[teamIndex].CountObjectsInBounds(objects);

            // Return the num leaves in that section.
            return teamSections[teamIndex].numLeaves;
        }

        public void Reset()
        {
            matchTimer.Reset();
        }

        /// <summary>
        /// Determines whether the game is over, either by the number of leaves 
        /// on each side or whether the stopwatch for match time is beyond 
        /// threshold. 
        /// </summary>
        /// <returns>The winning team or null on not game over.</returns>
        public Team GameOver()
        {
            Team winningTeam = Team.NONE;

            int maxLeaves = 0;
            foreach (TeamSection teamSection in teamSections)
            {
                if (teamSection.numLeaves > maxLeaves)
                {
                    maxLeaves = teamSection.numLeaves;
                    winningTeam = teamSection.team;
                }
            }

            if (matchTimer.Elapsed.Seconds > matchTime || maxLeaves > Constants.WIN_LEAF_NUM)
            {
                return winningTeam;
            }

            return Team.NONE;
        }

        /// <summary>
        /// Get a match string.
        /// </summary>
        /// <returns>A string version of this match.</returns>
        public override string ToString()
        {

            // Start with a match tag.
            string returnString = "[Current Match Status] ";

            // Iterate through all team sections.
            for (int i = 0; i < teamSections.Count; i++)
            {

                //    Console.WriteLine(teamSections[i]);

                // Add team info about the objects in this section.
                returnString += string.Format("Team {0}: {1}", i, teamSections[i].numLeaves);

                // Divide sections.
                returnString += " | ";
            }

            // Add no man's leaves as a string.
            returnString += string.Format("No Man's Leaves: {0}", NoMansLand.numLeaves);

            // Return the string.
            return returnString;

        }

        /// <summary>
        /// Starts a match with a given timeout.
        /// </summary>
        /// <param name="matchTime">The timeout on the match</param>
        public void StartMatch(int matchTime)
        {
            this.matchTime = matchTime;
            matchTimer.Start();
        }
    }
}
