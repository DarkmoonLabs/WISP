using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Moserware.Skills.TrueSkill;
using Moserware.Skills;

namespace Shared
{
    public class TrueSkillRating
    {
        /// <summary>
        /// Basic information about a match quality between two players
        /// </summary>
        public class SkillMatchInfo
        {
            public SkillMatchInfo(int characterId, double quality)
            {
                CharacterId = characterId;
                MatchQuality = quality;
            }

            /// <summary>
            /// The ID of the character to whom this match info is referring
            /// </summary>
            public int CharacterId { get; set; }
            
            /// <summary>
            /// A quality index from 0 to 1, with 1 being a 100% chance of an even match.
            /// </summary>
            public double MatchQuality { get; set; }
        }

        /// <summary>
        /// Registers the types in this addon with the object creation Factory, allowing any components to be serialied and deserialized properly
        /// </summary>
        public static void RegisterSerializableTypes()
        {
            Factory.Instance.Register(typeof(TSCharacterComponent), () => { return new TSCharacterComponent(); });
        }

        /// <summary>
        /// Gets players that are most suited for competition (would provide as close as possible to an even match).  Both @candidates and @target need to have the TSCharacterComponent attached.
        /// </summary>
        /// <param name="candidates">the possible candidates to match against</param>
        /// <param name="target">the player for whom we wish to find matches</param>
        /// <returns>a list of character IDs in order of </returns>
        public static List<SkillMatchInfo> GetTopQualityMatches(IEnumerable<ICharacterInfo> candidates, ICharacterInfo target, int maxResults)
        {
            DateTime start = DateTime.UtcNow;
            List<SkillMatchInfo> matches = new List<SkillMatchInfo>();

            try
            {
                GameInfo gi = GameInfo.DefaultGameInfo;

                Player targetPlayer = new Player(target.ID);
                double targetMu = target.Properties.GetDoubleProperty((int)TSPropertyID.RatingMean).GetValueOrDefault();
                double targetSigma = target.Properties.GetDoubleProperty((int)TSPropertyID.RatingStandardDeviation).GetValueOrDefault();
                Rating targetRating = new Rating(targetMu, targetSigma);
                Team targetTeam = new Team(targetPlayer, targetRating);
                int numCandidates = 0;

                IEnumerator<ICharacterInfo> enu = candidates.GetEnumerator();
                while (enu.MoveNext())
                {
                    numCandidates++;
                    Player player = new Player(enu.Current.ID);
                    double mu = enu.Current.Properties.GetDoubleProperty((int)TSPropertyID.RatingMean).GetValueOrDefault();
                    double sigma = enu.Current.Properties.GetDoubleProperty((int)TSPropertyID.RatingStandardDeviation).GetValueOrDefault();
                    Rating rating = new Rating(mu, sigma);
                    Team team = new Team(player, rating);
                    double quality = TrueSkillCalculator.CalculateMatchQuality(gi, Teams.Concat(targetTeam, team));

                    matches.Add(new SkillMatchInfo(enu.Current.ID, quality));
                }

                // Sort it
                matches.OrderBy(i => i.MatchQuality);

                // trim it, if necessary
                if (maxResults > 0)
                {
                    if (maxResults > matches.Count)
                    {
                        maxResults = matches.Count;
                    }

                    matches = matches.GetRange(0, maxResults - 1);
                }

                DateTime end = DateTime.UtcNow;
                TimeSpan exeTime = end - start;
                int highestQuality = 0;
                if (matches.Count > 0)
                {
                    highestQuality = (int)Math.Floor(matches[0].MatchQuality * 100);
                }

                Log.LogMsg("TrueSkill match maker tested [" + numCandidates + "] candidates for character [" + target.CharacterName + " | " + target.ID + "]. Returned [" + matches.Count + "] possible matches in [" + exeTime.TotalMilliseconds + " ms]. Best match found had a [" + highestQuality + "%] quality rating.");
            }
            catch(Exception e)
            {
                Log.LogMsg("TrueSkill match maker encountered an error when searching for match candidates. " + e.Message);
            }

            return matches;
        }


    }
}
