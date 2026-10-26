using System;
using System.Collections.Generic;
using System.Linq;

namespace Torn.Grids
{
	class RingGrid
	{
		/// <summary>Generate a Lord of the Ring-style fixture, with multiple ring games running in parallel.
		/// "team" and "player" are a bit interchangeable in this code, because Lord of the Ring is a solo event: each "team" has just one player in it.</summary>
		public string GenerateRingGrid(Fixture fixture, List<LeagueTeam> teams, int numRings, int teamsPerRing, int gamesPerTeam, DateTime firstGameDateTime, int minutesBetweenGames, bool referees)
		{
			fixture.Games.Clear();

			if (teams.Count < numRings * teamsPerRing)
				return Error(string.Format("{0} teams is not enough to fill {1} rings with {2} teams per ring. Try {3} rings or less.", teams.Count, numRings, teamsPerRing, teams.Count / teamsPerRing));

			if (!((teamsPerRing == 3 && gamesPerTeam == 6 && PopulateBlocks(numRings)) ||
				  (teamsPerRing == 3 && gamesPerTeam == 4 && teams.Count == 36 && Round2Blocks(numRings)) ||
				  (teamsPerRing == 3 && gamesPerTeam == 4 && teams.Count == 9 && Round3Blocks(numRings))))
				GenerateBlock(teams.Count, teamsPerRing, gamesPerTeam, numRings);

			int smallestBlockSize = rawBlocks.Keys.Min();
			int largestBlockSize = rawBlocks.Keys.Max();

			if (smallestBlockSize > teams.Count || (teams.Count > largestBlockSize && smallestBlockSize * 2 > teams.Count))  // We can handle if teams.Count fits in a single block, or if teams.Count is large enough that we can use two or more blocks.
				GenerateBlock(teams.Count, teamsPerRing, gamesPerTeam, numRings);
			
			fixture.Teams.Clear();
			fixture.Teams.Populate(teams);

			var blockSizes = new List<int>();

			if (rawBlocks.ContainsKey(teams.Count))
				blockSizes.Add(teams.Count);
			else
			{
				int niceMultiple = numRings % 2 == 0 && gamesPerTeam % 6 == 0 ? numRings / 2 : numRings;  // If there are _n_ rings, then it's nice to choose blocks that have a multiple of _n_ players, because each game uses all rings. Special case: if _n_ is even, then half _n_ works fine, because each player plays 6 times, and, um, maths.
				int smallestNiceBlock = (int)Math.Ceiling(1.0 * smallestBlockSize / niceMultiple) * niceMultiple;
				bool tightSqueeze = smallestNiceBlock * 2 + niceMultiple > teams.Count;
				if (smallestNiceBlock > largestBlockSize || tightSqueeze)
					smallestNiceBlock = smallestBlockSize;

				int remainingPlayers = teams.Count;
				int remainingBlocks = Math.Max(teams.Count / smallestNiceBlock, 1);

				int firstBlock = tightSqueeze ? remainingPlayers / remainingBlocks : ((remainingPlayers / remainingBlocks / niceMultiple) * niceMultiple) + remainingPlayers % niceMultiple;  // Put all the non-multiple-of-_n_-ness in one block, so every other block can be nice.
				blockSizes.Add(firstBlock);
				remainingPlayers -= firstBlock;
				remainingBlocks--;

				// Figure out the sizes we will use for the remaining blocks.
				while (remainingPlayers > 0)
				{
					int nextBlock = remainingBlocks == 1 ? remainingPlayers : (remainingPlayers / remainingBlocks / niceMultiple) * niceMultiple;
					while (nextBlock < smallestBlockSize)
						nextBlock += niceMultiple;
					blockSizes.Add(nextBlock);
					remainingPlayers -= nextBlock;
					remainingBlocks--;
				}

				foreach (var blockSize in blockSizes.Where(b => !rawBlocks.ContainsKey(b)))
					GenerateBlock(blockSize, teamsPerRing, gamesPerTeam, numRings);
				//return Error(string.Format("Could not generate fixture for {0} teams, {1} rings, {2} games each.", teams.Count, numRings, gamesPerTeam));

				if (blockSizes.Sum() != teams.Count)
					return Error(string.Format("Could not generate fixture for {0} teams, {1} rings, {2} games each. Blocks summed to {3}.", teams.Count, numRings, gamesPerTeam, blockSizes.Sum()));

				blockSizes.Sort((x, y) => y - x);  // Largest first.
			}

			// Build the blocks of FixtureGames.
			var fixtureBlocks = new List<FixtureGames>();
			int teamOffset = 0;
			int colourOffset = 0;
			foreach (var blockSize in blockSizes)
			{
				fixtureBlocks.Add(RawToFixture(teams, rawBlocks[blockSize], teamOffset, colourOffset));
				teamOffset += blockSize;
				colourOffset++;
			}

			// Interleave those FixtureGame's to make the fixture.
			for (int game = 0; game < fixtureBlocks.Max(fb => fb.Count); game++)
				for (int block = 0; block < blockSizes.Count; block++)
					if (fixtureBlocks[block].Valid(game))
						fixture.Games.Add(fixtureBlocks[block][game]);

			// Set the start time for each FixtureGame.
			var time = firstGameDateTime;
			foreach (var fg in fixture.Games)
			{
				fg.Time = time;
				time = time.AddMinutes(minutesBetweenGames);
			}

			if (referees)
				AddReferees(fixture, teams, fixtureBlocks.Count >= 2 && fixtureBlocks[0].Count > fixtureBlocks[1].Count ? blockSizes[0] : 0);

			return generated ? "The generated fixture has a poor who-plays-who. On the Settings tab, ensure you tick Shuffle > Teams between games, and if necessary click Generate again.\r\n" : "";
		}

		private string Error(string message)
		{
			Console.WriteLine(message);
			return message;
		}

		private class RefCount { public LeagueTeam Team; public int Count; }

		private class RefCounts : List<RefCount>
		{
			/// <summary>Return this team's record. If this team is not in the list, we return null.</summary>
			public RefCount Find(LeagueTeam team)
			{
				return this.Find(rc => rc.Team == team);
			}
		}

		/// <summary>Add referees to games.
		/// Don't use players whose alias ends in * as referees, as the * denotes an inexperienced player.</summary>
		/// <param name="lastGameRefereeOffset">For the last game, where should we pull referees from? If first block has more games, that means they
		/// are playing in the last game, and are unavailable to referee it, so pass a value here that points to players from the second block.</param>
		public void AddReferees(Fixture fixture, List<LeagueTeam> teams, int lastGameRefereeOffset)
		{
			FixtureGame thisGame;
			FixtureGame nextGame;

			var refCounts = new RefCounts();  // Track how many times each player is refereeing.
			foreach (var team in teams)
				if (!IsN00b(team))
					refCounts.Add(new RefCount() { Team = team, Count = 0 });

			// Add referees: each player refs the game before they play.
			for (int game = 0; game < fixture.Games.Count - 1; game++)
			{
				thisGame = fixture.Games[game];
				nextGame = fixture.Games[game + 1];

				foreach (var player in nextGame.Teams.Keys.Where(t => CanReferee(thisGame, t)))
				{
					thisGame.Teams.Add(player, Colour.Referee);
					refCounts.Find(player).Count++;
				}
			}

			thisGame = fixture.Games.Last();

			// Add referees for the last game.
			for (int player = lastGameRefereeOffset; player < lastGameRefereeOffset + thisGame.Teams.Count(t => t.Value != Colour.Referee); player++)
				if (CanReferee(thisGame, teams[player]))
				{
					thisGame.Teams.Add(teams[player], Colour.Referee);
					refCounts.Find(teams[player]).Count++;
				}

			// Look for games that didn't get enough referees and give them more.
			var gamesThatNeedRefs = fixture.Games.Where(fg => fg.Teams.Count(t => t.Value == Colour.Referee) < fg.Teams.Count(t => t.Value != Colour.Referee)).ToList();  // We desire one referee per player.
			while (gamesThatNeedRefs.Any())
			{
				bool assignedAnyRefs = false;

				for (int i = 0; i < gamesThatNeedRefs.Count; i++)
				{
					thisGame = gamesThatNeedRefs[i];
					int refsDesired = thisGame.Teams.Count(t => t.Value != Colour.Referee) - thisGame.Teams.Count(t => t.Value == Colour.Referee);
					refCounts.Sort((x, y) => x.Count - y.Count);  // Smallest first.
					var refsAvailableForThisGame = refCounts.Where(rc => CanReferee(thisGame, rc.Team)).ToList();
					while (refsDesired > 0 && refsAvailableForThisGame.Any())
					{
						RefCount firstRef = refsAvailableForThisGame.First();
						thisGame.Teams.Add(firstRef.Team, Colour.Referee);
						refsAvailableForThisGame.Remove(firstRef);
						firstRef.Count++;  // Update their entry in the master refCounts list.
						assignedAnyRefs = true;
						refsDesired--;
						if (refsDesired == 0)
						{
							gamesThatNeedRefs.Remove(thisGame);
							i--;
						}
					}
				}

				if (!assignedAnyRefs)
					return;  // If we go through all the games without making a single match, we just need to stop: there are no remaining players who can ref the games that need refs.
			}
		}

		/// <summary>Return true if this player is available to referee this game.</summary>
		private bool CanReferee(FixtureGame game, LeagueTeam team)
		{
			return !game.Teams.Keys.Any(k => k.TeamId == team.TeamId) && !IsN00b(team);  // If they're already in this game (as a player or as a referee), or they're marked as a first-time player, they can't referee this game.
		}

		/// <summary>Returns true if the player has a * on the end of their alias, which indicates they're a first-time player.</summary>
		private bool IsN00b(LeagueTeam team)
		{
			return team.Name.EndsWith("*");
		}

		/// <summary>Convert a RawBlock to a FixtureGame with actual teams filled in.</summary>
		/// <param name="colourOffset">For games after the first in this block, offset the colour we give to each team by this much.
		/// This is to prevent particular referees and players "following" each other around: without this offset, if a player referees
		/// the ring they're about to play in, and two consecutive blocks are the same size, then the players in block 1 will always be
		/// refereed by the same players in block 2.</param>
		private FixtureGames RawToFixture(List<LeagueTeam> teams, RawBlock block, int teamOffset, int colourOffset)
		{
			var games = new FixtureGames();
			int rings = block.Max(g => g.Max());

			for (int i = 0; i < block.Count; i++)
			{
				var rawGame = block[i];
				var fg = new FixtureGame();

				for (int j = 0; j < rawGame.Count; j++)
					if (rawGame[j] != 0)  // 0 is a bye for this player.
						fg.Teams.Add(teams[j + teamOffset], (Colour)((rawGame[j] + (i == 0 ? 0 : colourOffset) - 1) % rings + 1));

				games.Add(fg);
			}
			return games;
		}

		/// <summary>A list of ints representing colours we will use for players in a single game, indicating which ring they are playing in, with 0 indicating a bye.</summary>
		private class RawGame : List<int>
		{
			public static RawGame Blank(int length)
			{
				var x = new RawGame();

				for (int i = 0; i < length; i++)
					x.Add(0);

				return x;
			}

			/// <summary>Sets a range of elements to the value in ring.</summary>
			/// <param name="index">The zero-based starting index of the range of elements to set.</param>
			/// <param name="count">The number of elements to set.</param>
			/// <param name="ring">The value to set.</param>
			public void SetRange(int index, int count, int ring)
			{
				for (int i = 0; i < count; i++)
					this[(index + i) % Count] = ring;
			}

			public int Populate(ref int pointer, int teamsPerRing, int rings)
			{
				for (int ring = 1; ring <= rings; ring++)
				{
					SetRange(pointer, teamsPerRing, ring);
					pointer += teamsPerRing;
				}

				return teamsPerRing * rings;
			}
		}

		/// <summary>Each block is a compact group of players playing in games. Each row is a game; each column is a player.
		/// For example, the first block below for rings == 6 is 18 players in 6 games, with each player playing 6 times.</summary>
		private class RawBlock : List<RawGame>
		{
			public RawGame AddNew(int length)
			{
				var rawGame = RawGame.Blank(length);
				Add(rawGame);
				return rawGame;
			}
		}

		/// <summary>Key: number of teams/players in the block. Value: the block.</summary>
		readonly Dictionary<int, RawBlock> rawBlocks = new Dictionary<int, RawBlock>();

		/// <summary>Create the blocks that we will build the ring grid fixture out of.
		/// The number indicates which colour ring they are playing in, with 0 indicating a bye -- we'll convert the numbers to Colours in PopulateBlock.
		/// For most sizes, we just use GenerateBlock(), below, to generate a block with poor who-plays-who and then let GridFinder optimise it. But for small-ish numbers 
		/// of teams, particularly if referee generation is ticked, GridFinder has trouble. So we supply these blocks which already have perfect who-plays-who.</summary>
		private bool PopulateBlocks(int rings)
		{
			if (rings == 2)
			{
				PopulateBlock(13, new List<int>() {
				1,1,1,2,2,2,0,0,0,0,0,0,0,
				0,2,0,0,0,0,1,1,1,2,2,0,0,
				0,1,2,0,1,2,0,0,0,0,0,1,2,
				1,0,0,0,2,0,1,2,0,1,2,0,0,
				0,0,1,2,0,0,0,0,1,2,0,1,2,
				1,0,2,0,2,1,2,1,0,0,0,0,0,
				0,0,0,1,0,0,1,0,2,0,2,1,2,
				1,2,0,2,0,0,0,2,0,0,1,1,0,
				1,2,0,0,1,2,0,0,2,0,0,0,1,
				0,0,2,2,0,1,0,0,0,1,2,1,0,
				0,1,0,0,2,0,1,0,2,2,0,0,1,
				0,0,2,0,0,1,1,2,0,2,1,0,0,
				2,0,0,2,0,0,0,1,2,0,0,1,1 });

				PopulateBlock(14, new List<int>() {
				1,1,1,2,2,2,0,0,0,0,0,0,0,0,
				0,0,0,0,1,0,1,1,2,2,2,0,0,0,
				1,2,0,2,0,0,1,0,0,1,0,2,0,0,
				0,1,0,0,0,2,1,0,1,0,2,2,0,0,
				2,1,0,0,1,0,0,0,0,0,0,2,1,2,
				0,0,1,0,0,2,2,1,0,0,1,0,0,2,
				1,0,0,0,2,0,0,0,1,2,0,2,1,0,
				0,0,1,2,0,1,0,0,2,0,0,0,1,2,
				2,0,0,2,0,0,1,2,0,0,0,1,1,0,
				0,0,1,0,1,2,0,2,2,0,0,0,0,1,
				0,1,0,2,0,1,2,0,0,1,2,0,0,0,
				0,0,2,2,0,0,0,0,0,2,1,0,1,1,
				1,2,0,0,1,0,0,2,0,0,1,0,0,2,
				0,0,2,0,0,0,0,1,2,1,0,2,1,0 });

				PopulateBlock(15, new List<int>() {
				1,1,1,2,2,2,0,0,0,0,0,0,0,0,0,
				0,0,0,0,0,0,1,1,1,2,2,2,0,0,0,
				0,0,0,0,1,0,0,0,2,2,0,1,1,2,0,
				2,0,0,1,0,2,0,1,0,0,1,0,0,0,2,
				0,1,2,2,1,0,1,0,2,0,0,0,0,0,0,
				0,0,0,0,0,2,0,1,0,1,2,0,2,0,1,
				2,0,0,1,0,0,1,0,2,0,0,2,0,1,0,
				0,1,2,0,0,2,2,1,0,0,0,0,1,0,0,
				1,0,0,0,0,0,2,1,0,0,0,2,0,1,2,
				0,1,2,1,2,0,0,0,0,1,2,0,0,0,0,
				0,0,2,1,0,0,0,0,0,0,0,2,1,2,1,
				2,0,0,0,2,0,0,0,1,2,1,0,0,0,1,
				0,0,1,0,0,2,0,2,0,1,0,2,1,0,0,
				0,1,0,0,2,1,0,0,1,0,0,0,0,2,2,
				2,1,0,0,0,0,2,0,0,0,1,0,2,1,0 });
			}
			else if (rings == 3)
			{
				PopulateBlock(13, new List<int>() {
				1,1,1,2,2,2,3,3,3,0,0,0,0,
				1,2,0,0,1,3,0,0,3,1,2,2,3,
				0,0,1,3,2,0,1,2,3,1,3,2,0,
				2,3,1,1,3,2,2,0,0,0,0,1,3,
				3,2,0,0,0,0,1,3,2,2,3,1,1,
				3,1,2,3,2,1,0,1,2,0,0,0,3,
				0,0,1,0,2,3,2,1,0,3,2,3,1,
				3,2,0,2,0,0,2,0,3,1,1,3,1,
				0,0,1,2,0,1,0,2,0,2,1,0,0
				});

				PopulateBlock(14, new List<int>() {
				1,1,1,2,2,2,3,3,3,0,0,0,0,0,
				0,3,1,2,0,0,3,2,1,1,2,3,0,0,
				0,2,0,0,1,3,1,2,3,0,0,3,1,2,
				3,0,2,1,2,0,3,0,0,1,2,0,1,3,
				0,1,3,2,1,0,0,3,2,1,0,0,3,2,
				2,0,3,0,0,3,0,1,2,1,0,1,2,3,
				3,0,2,2,1,3,2,0,0,0,3,1,0,1,
				2,1,0,2,0,1,0,0,0,3,3,2,1,3,
				1,0,0,0,1,2,2,1,0,2,3,3,3,0,
				0,1,0,0,0,0,0,0,1,0,1,0,0,0 });

				PopulateBlock(15, new List<int>() {
				1,1,1,2,2,2,3,3,3,0,0,0,0,0,0,
				0,2,1,0,0,0,0,2,0,1,1,2,3,3,3,
				0,0,0,2,0,1,2,0,3,2,3,1,0,3,1,
				3,1,0,3,0,2,0,0,1,2,0,3,2,0,1,
				1,3,0,3,2,1,1,0,2,2,3,0,0,0,0,
				1,0,2,2,0,0,0,3,0,1,3,0,3,1,2,
				1,0,2,0,1,2,3,0,2,0,0,3,1,3,0,
				1,2,3,0,3,2,0,3,0,0,1,0,0,2,1,
				0,1,0,2,1,0,1,2,3,0,0,3,3,2,0,
				0,0,2,0,1,0,2,3,0,3,1,1,2,0,3 });
			}
			else if (rings == 4)
			{
				PopulateBlock(15, new List<int>() {
				1,1,1,2,2,2,3,3,3,4,4,4,0,0,0,
				3,4,1,0,2,3,4,2,1,0,4,2,1,3,0,
				1,4,0,3,1,4,1,2,3,4,3,0,2,0,2,
				2,1,4,0,4,3,3,2,1,4,2,0,0,1,3,
				3,2,4,1,2,0,1,4,0,1,0,3,2,4,3,
				4,0,1,1,0,2,3,0,4,4,2,3,2,3,1,
				0,4,3,4,2,3,0,4,2,1,0,3,1,1,2,
				0,0,0,1,0,0,0,0,0,0,2,1,1,2,2 });

				PopulateBlock(16, new List<int>() {
				1,1,1,2,2,2,3,3,3,4,4,4,0,0,0,0,
				0,4,2,0,1,2,0,0,3,1,2,3,1,3,4,4,
				3,4,0,3,0,2,4,1,2,2,4,1,3,0,0,1,
				0,3,4,2,3,1,4,3,0,4,0,1,1,2,2,0,
				2,1,3,1,3,2,4,0,1,0,0,4,0,3,4,2,
				2,0,0,4,0,3,2,3,1,4,1,0,1,2,3,4,
				3,2,4,0,1,0,1,4,0,2,3,0,4,2,3,1,
				4,0,3,2,4,0,0,2,3,0,2,4,1,1,3,1 });

				PopulateBlock(17, new List<int>() {
				1,1,1,2,2,2,3,3,3,4,4,4,0,0,0,0,0,
				2,0,0,1,3,4,1,3,4,0,0,3,1,2,2,4,0,
				0,2,1,3,4,2,4,1,0,0,4,3,1,0,3,0,2,
				1,4,3,3,0,2,0,2,0,1,0,0,4,2,4,3,1,
				2,0,4,0,0,3,2,0,1,3,1,4,3,0,1,2,4,
				3,0,0,1,4,3,0,0,1,2,3,0,4,4,2,2,1,
				0,3,2,0,2,0,3,4,0,2,1,3,0,1,4,1,4,
				1,3,0,3,1,0,4,0,1,0,3,2,2,4,0,2,4,
				0,1,2,0,0,0,0,1,2,1,0,0,0,2,0,0,0 });

				PopulateBlock(18, new List<int>() {
				1,1,1,2,2,2,3,3,3,4,4,4,0,0,0,0,0,0,
				0,1,3,1,3,4,0,0,0,0,1,2,0,2,2,3,4,4,
				1,0,0,3,4,2,0,1,2,4,2,0,0,3,4,1,0,3,
				0,0,0,3,4,0,1,3,2,0,4,1,2,0,3,1,2,4,
				2,1,3,0,0,0,4,1,3,2,0,0,4,4,3,0,2,1,
				1,0,4,3,0,1,4,0,0,4,2,3,2,1,0,2,3,0,
				1,2,0,0,0,4,0,3,1,4,0,3,3,2,0,4,2,1,
				2,1,3,0,4,1,2,3,4,0,3,0,1,4,2,0,0,0,
				0,4,3,1,4,0,4,0,0,1,0,3,1,0,2,2,2,3 });

				PopulateBlock(19, new List<int>() {
				1,1,1,2,2,2,3,3,3,4,4,4,0,0,0,0,0,0,0,
				4,0,2,2,0,3,0,0,0,0,0,1,1,1,2,3,3,4,4,
				0,1,0,1,4,2,1,4,3,3,2,0,2,0,0,3,4,0,0,
				3,1,4,3,0,0,2,0,3,4,0,1,0,2,1,0,0,4,2,
				2,1,0,0,4,2,0,0,0,0,1,2,4,3,0,4,3,3,1,
				0,0,2,1,0,0,4,3,2,3,4,0,0,0,3,1,4,1,2,
				3,2,0,0,4,2,3,0,4,0,0,4,1,2,1,3,0,0,1,
				3,0,1,4,1,0,2,3,0,4,1,2,0,3,0,0,4,2,0,
				0,1,0,0,0,2,0,3,4,2,3,0,1,4,4,3,1,0,2,
				0,0,1,0,2,0,0,1,0,0,0,0,1,0,2,0,0,2,0 });

				PopulateBlock(20, new List<int>() {
				1,1,1,2,2,2,3,3,3,4,4,4,0,0,0,0,0,0,0,0,
				1,2,3,0,1,0,0,0,4,0,0,0,1,2,2,3,3,4,4,0,
				0,0,2,1,0,2,3,1,4,1,3,4,3,0,2,0,0,0,0,4,
				2,3,0,0,1,0,4,1,3,0,0,0,3,0,0,1,2,2,4,4,
				0,0,4,4,2,3,0,0,0,2,3,1,0,3,1,0,0,1,4,2,
				1,3,0,4,0,0,4,3,1,0,0,3,2,1,0,4,2,0,0,2,
				0,0,1,4,0,2,3,0,0,3,1,0,0,4,3,2,4,1,2,0,
				2,4,0,0,4,0,0,1,0,0,4,3,3,3,2,2,0,1,0,1,
				2,0,1,3,0,4,1,4,3,2,3,1,0,0,0,0,4,0,2,0,
				0,2,0,0,3,2,0,0,0,4,0,0,4,3,1,4,1,3,1,2 });
			}
			else if (rings == 5)
			{
				PopulateBlock(16, new List<int>() {
				1,1,1,2,2,2,3,3,3,4,4,4,5,5,5,0,
				1,2,5,0,1,5,2,4,3,4,5,3,2,3,4,1,
				5,1,0,2,3,4,2,3,1,1,3,5,5,2,4,4,
				3,1,4,3,1,5,2,4,0,5,2,1,4,5,3,2,
				4,1,3,5,3,4,3,4,2,5,2,0,5,1,2,1,
				2,1,3,1,4,5,5,1,4,3,2,5,4,2,0,3,
				0,0,1,2,0,0,0,0,2,0,0,1,0,0,1,2 });

				PopulateBlock(17, new List<int>() {
				1,1,1,2,2,2,3,3,3,4,4,4,5,5,5,0,0,
				1,2,3,5,3,4,0,4,0,1,4,5,1,2,3,2,5,
				2,5,4,2,1,5,5,4,2,1,3,0,4,0,3,3,1,
				3,0,5,2,3,4,5,2,1,5,0,1,1,4,2,3,4,
				5,1,0,3,4,2,4,0,2,3,1,5,4,3,5,2,1,
				0,4,5,4,0,5,2,1,3,1,2,5,4,2,3,1,3,
				1,3,2,0,3,0,4,1,2,0,2,3,0,1,0,4,4 });

				PopulateBlock(18, new List<int>() {
				1,1,1,2,2,2,3,3,3,4,4,4,5,5,5,0,0,0,
				0,3,5,2,3,4,1,2,4,0,1,5,5,0,4,1,2,3,
				1,5,2,0,3,2,5,2,0,3,4,5,0,4,1,1,3,4,
				5,0,4,4,2,1,0,3,2,3,4,5,1,2,0,3,1,5,
				5,1,4,3,0,2,3,5,4,2,0,0,5,2,3,1,1,4,
				5,4,2,4,1,0,2,0,5,2,5,0,1,4,3,1,3,3,
				2,3,0,5,1,3,2,1,5,4,3,5,4,0,1,0,2,4,
				0,0,0,0,0,0,0,0,0,0,0,1,0,1,0,1,0,0 });

				PopulateBlock(19, new List<int>() {
				1,1,1,2,2,2,3,3,3,4,4,4,5,5,5,0,0,0,0,
				2,0,0,2,5,4,3,2,0,1,5,3,3,1,0,1,4,4,5,
				5,1,4,0,0,1,3,1,5,5,3,0,4,0,2,2,4,3,2,
				2,4,5,4,3,0,2,0,1,0,1,3,4,3,2,0,1,5,5,
				5,0,4,3,4,1,0,2,0,2,0,1,2,5,4,3,3,5,1,
				0,1,0,4,0,3,4,5,3,2,5,1,0,4,5,3,2,1,2,
				4,2,3,5,1,4,2,1,5,0,3,0,4,3,0,2,1,0,5,
				0,2,1,0,2,0,0,0,3,2,0,1,0,0,3,1,0,3,0 });

				PopulateBlock(20, new List<int>() {
				1,1,1,2,2,2,3,3,3,4,4,4,5,5,5,0,0,0,0,0,
				1,2,0,0,0,4,3,4,5,2,3,4,0,1,3,1,2,5,5,0,
				5,0,4,2,3,5,1,0,0,4,5,2,1,0,0,4,1,2,3,3,
				0,5,0,2,1,3,0,1,3,0,0,4,4,1,2,5,2,5,4,3,
				4,3,5,4,0,5,4,2,1,1,3,0,3,1,2,0,5,0,0,2,
				0,2,3,0,5,0,0,4,5,0,3,1,4,3,2,5,1,4,2,1,
				1,0,2,2,1,3,5,4,2,3,4,0,0,0,0,4,1,5,3,5,
				3,5,4,2,5,0,1,0,0,3,0,5,3,1,4,2,0,4,1,2 });

				PopulateBlock(21, new List<int>() {
				1,1,1,2,2,2,3,3,3,4,4,4,5,5,5,0,0,0,0,0,0,
				0,0,3,0,2,5,1,4,5,0,5,0,1,2,0,1,2,3,3,4,4,
				2,3,0,3,1,0,1,0,0,4,5,2,0,4,3,5,2,5,0,4,1,
				0,3,2,5,3,1,0,4,2,5,0,3,2,0,4,5,4,0,1,0,1,
				3,5,0,2,4,1,5,0,2,0,4,0,4,3,1,0,0,5,2,1,3,
				4,5,2,0,0,3,0,3,5,3,2,1,1,0,0,4,5,0,4,1,2,
				0,0,0,5,4,3,3,5,1,2,0,3,0,5,4,4,2,1,2,1,0,
				1,4,3,1,0,0,3,2,0,0,2,0,1,3,0,4,5,5,2,4,5,
				2,0,1,0,0,0,0,0,0,2,0,1,0,0,1,0,0,2,0,0,0 });

				PopulateBlock(22, new List<int>() {
				1,1,1,2,2,2,3,3,3,4,4,4,5,5,5,0,0,0,0,0,0,0,
				0,3,0,4,0,0,0,5,0,5,0,4,2,5,1,1,1,2,2,3,3,4,
				5,3,4,0,1,4,5,0,1,0,1,2,0,0,2,3,4,3,5,2,0,0,
				3,0,5,1,2,0,0,2,0,3,4,0,3,5,4,0,1,0,5,1,4,2,
				5,1,0,0,0,4,4,0,5,0,2,1,2,1,0,2,3,5,0,4,3,3,
				2,3,5,5,4,2,3,2,0,1,0,0,3,0,1,4,0,5,4,0,0,1,
				0,0,1,2,0,5,1,2,4,0,3,5,0,4,0,1,4,5,0,3,2,3,
				4,2,0,4,2,3,0,0,1,5,4,0,1,0,2,3,5,0,5,0,3,1,
				0,0,3,0,3,0,2,1,4,3,0,4,0,2,0,0,0,1,4,1,2,0 });
			}
			else if (rings == 6)
			{
				PopulateBlock(18, new List<int>() {
				1,1,1,2,2,2,3,3,3,4,4,4,5,5,5,6,6,6,
				1,3,4,1,2,3,3,5,6,2,4,5,1,4,6,2,5,6,
				2,3,4,1,2,6,5,1,3,6,2,5,4,1,5,3,6,4,
				5,3,4,2,3,6,4,1,6,1,6,2,1,3,5,4,5,2,
				1,5,2,5,2,3,4,6,1,5,4,3,3,1,6,6,2,4,
				2,3,5,6,1,5,2,1,6,2,3,4,3,4,5,4,6,1 });

				PopulateBlock(19, new List<int>() {
				1,1,1,2,2,2,3,3,3,4,4,4,5,5,5,6,6,6,0,
				1,2,5,1,3,4,0,4,5,4,6,5,1,2,6,2,3,6,3,
				5,2,6,4,2,0,1,6,3,2,3,4,1,5,6,1,3,5,4,
				5,1,6,1,4,3,5,4,0,6,2,5,3,2,1,6,3,4,2,
				5,6,3,2,3,1,4,5,2,4,1,6,3,2,0,1,6,4,5,
				2,1,5,4,2,6,5,4,3,3,4,6,3,5,6,2,0,1,1,
				0,0,0,0,0,2,1,0,2,0,0,0,0,0,1,0,1,0,2 });

				PopulateBlock(20, new List<int>() {
				1,1,1,2,2,2,3,3,3,4,4,4,5,5,5,6,6,6,0,0,
				2,3,6,1,3,5,3,4,6,1,4,5,1,2,4,0,0,5,2,6,
				2,1,0,1,3,5,4,2,5,2,5,3,0,6,4,1,6,4,3,6,
				2,3,6,0,1,4,2,5,1,4,6,5,1,0,3,2,3,6,4,5,
				0,1,3,5,2,3,6,1,0,2,6,5,3,1,5,2,6,4,4,4,
				6,5,2,3,0,6,4,3,1,1,5,4,4,2,0,2,3,1,5,6,
				1,0,3,2,1,0,0,0,2,0,0,0,4,2,3,4,1,0,3,4 });

				PopulateBlock(21, new List<int>() {
				1,1,1,2,2,2,3,3,3,4,4,4,5,5,5,6,6,6,0,0,0,
				2,3,4,3,4,5,0,1,4,3,5,6,2,5,6,0,0,2,1,1,6,
				0,3,6,4,3,2,1,2,0,2,0,1,3,4,6,5,6,1,4,5,5,
				6,3,0,1,2,5,2,1,6,0,6,3,4,2,0,1,3,5,4,5,4,
				5,0,4,1,3,4,5,0,2,6,1,3,1,0,2,3,6,2,4,5,6,
				6,2,3,0,0,0,4,1,5,3,2,6,5,3,4,4,5,1,6,2,1,
				1,6,4,1,3,2,5,4,2,5,4,0,0,6,3,2,1,0,5,3,6 });

				PopulateBlock(22, new List<int>() {
				1,1,1,2,2,2,3,3,3,4,4,4,5,5,5,6,6,6,0,0,0,0,
				0,5,6,1,4,6,2,4,5,0,2,3,0,1,2,0,3,6,1,3,4,5,
				1,6,0,6,3,0,3,2,1,6,5,2,2,4,0,1,5,4,5,3,0,4,
				4,0,5,0,6,1,4,0,0,4,1,2,1,6,5,3,6,2,5,3,2,3,
				4,1,6,4,3,1,6,1,3,5,3,2,4,0,2,2,0,0,5,5,6,0,
				6,5,1,3,0,2,0,4,3,4,6,0,0,5,3,5,1,4,2,6,2,1,
				6,5,0,4,6,1,2,4,3,0,0,5,2,3,1,4,2,6,5,0,3,1,
				0,0,1,0,0,0,0,0,0,2,0,0,1,0,0,0,0,0,0,1,2,2 });

				PopulateBlock(23, new List<int>() {
				1,1,1,2,2,2,3,3,3,4,4,4,5,5,5,6,6,6,0,0,0,0,0,
				1,3,6,0,1,0,4,2,5,0,0,2,5,6,2,6,0,4,1,3,3,4,5,
				4,0,2,5,6,3,5,2,0,1,6,4,3,0,1,0,5,2,3,1,4,6,0,
				4,3,1,0,0,6,0,5,1,3,2,0,4,2,0,3,4,6,5,6,1,5,2,
				0,2,1,6,2,3,4,3,2,1,5,6,0,4,5,5,1,0,0,6,4,0,3,
				4,2,0,5,3,2,3,0,1,6,2,3,5,1,0,5,0,6,1,0,6,4,4,
				3,0,5,1,2,0,4,6,3,1,3,0,4,0,5,0,6,2,1,4,6,5,2,
				0,1,0,2,0,3,0,0,0,0,0,3,0,1,2,3,4,0,4,4,0,1,2 });

				PopulateBlock(24, new List<int>() {
				1,1,1,2,2,2,3,3,3,4,4,4,5,5,5,6,6,6,0,0,0,0,0,0,
				0,2,3,0,0,5,0,0,1,4,3,1,1,5,0,2,3,6,2,4,4,5,6,6,
				1,0,5,3,6,1,6,5,3,3,0,0,4,2,1,4,0,0,2,4,0,6,2,5,
				4,2,5,3,6,0,4,0,6,1,6,2,0,5,3,0,1,4,0,3,5,2,1,0,
				4,5,2,6,2,1,0,1,0,0,5,4,2,0,5,3,0,6,6,0,1,3,3,4,
				2,5,0,6,3,0,5,3,1,4,2,0,0,3,0,4,6,0,1,1,6,2,5,4,
				5,3,6,0,0,3,6,5,3,5,0,2,4,2,1,0,1,2,1,6,4,0,0,4,
				0,0,0,4,2,3,4,6,0,0,3,6,1,0,5,3,1,2,6,2,5,1,5,4 });

				PopulateBlock(25, new List<int>() {
				1,1,1,2,2,2,3,3,3,4,4,4,5,5,5,6,6,6,0,0,0,0,0,0,0,
				2,3,0,6,3,4,6,1,0,0,0,0,5,6,0,5,2,0,1,1,2,3,4,4,5,
				0,4,1,6,0,5,0,0,5,3,4,1,3,0,2,4,2,5,3,0,0,6,6,2,1,
				1,0,0,4,2,0,1,5,4,1,4,3,0,5,2,0,3,2,0,6,6,3,0,6,5,
				2,3,5,0,0,4,3,0,2,0,6,2,1,5,0,4,6,1,4,6,1,5,3,0,0,
				0,1,5,5,3,6,6,2,0,4,0,2,0,0,4,2,0,1,3,6,0,4,3,5,1,
				4,0,3,0,1,0,1,4,6,5,2,0,2,6,0,1,3,5,3,5,6,0,4,2,0,
				4,3,0,6,2,3,0,1,0,3,5,2,2,4,6,0,0,0,4,0,5,1,5,1,6,
				0,0,2,0,0,0,0,0,1,0,0,0,0,0,2,0,0,0,0,1,2,0,0,0,1 });

				PopulateBlock(26, new List<int>() {
				1,1,1,2,2,2,3,3,3,4,4,4,5,5,5,6,6,6,0,0,0,0,0,0,0,0,
				5,3,4,6,0,0,1,0,0,0,6,0,1,3,0,4,0,6,1,2,2,2,3,4,5,5,
				0,3,0,0,2,1,4,1,2,6,5,4,3,4,5,0,2,0,6,1,0,5,0,0,3,6,
				2,0,1,3,4,0,0,2,0,3,0,2,0,0,4,5,3,1,5,4,6,0,6,6,5,1,
				0,6,3,0,6,1,4,5,1,2,6,3,2,1,0,0,4,0,5,3,5,2,4,0,0,0,
				2,6,1,5,0,4,4,0,1,3,0,6,0,0,3,2,0,6,0,0,1,2,3,5,4,5,
				5,0,0,4,2,5,0,2,6,0,1,0,2,4,0,4,1,0,5,6,0,3,3,6,1,3,
				0,2,5,0,5,1,3,0,0,2,1,6,6,4,3,0,6,4,5,0,1,0,0,2,4,3,
				2,0,0,2,0,0,0,4,1,0,0,0,0,0,3,1,0,3,0,2,3,4,1,4,0,0 });

				PopulateBlock(27, new List<int>() {
				1,1,1,2,2,2,3,3,3,4,4,4,5,5,5,6,6,6,0,0,0,0,0,0,0,0,0,
				0,5,6,3,2,6,6,0,0,5,0,0,0,0,0,2,0,1,1,1,2,3,3,4,4,4,5,
				2,0,0,0,3,5,0,6,3,0,5,4,2,3,1,0,4,0,2,6,6,0,1,5,1,0,4,
				0,2,4,2,0,6,3,6,1,1,2,5,0,0,4,6,3,5,0,0,0,4,0,1,5,3,0,
				2,0,0,0,3,0,6,1,0,6,4,0,1,4,2,2,0,5,4,3,6,0,3,0,1,5,5,
				4,5,1,1,0,2,0,4,2,0,0,4,3,1,0,3,5,0,2,6,5,6,0,6,0,0,3,
				3,2,0,4,3,0,2,0,0,4,1,5,0,0,4,0,6,1,5,0,5,6,1,2,6,3,0,
				0,3,4,5,0,1,0,6,2,0,2,1,5,3,0,3,0,0,0,5,2,6,1,0,4,6,4,
				2,0,5,0,3,0,2,0,1,4,0,0,6,4,3,0,5,1,5,4,0,1,6,3,0,6,2 });

				PopulateBlock(28, new List<int>() {
				1,1,1,2,2,2,3,3,3,4,4,4,5,5,5,6,6,6,0,0,0,0,0,0,0,0,0,0,
				1,0,6,0,0,6,2,0,0,0,0,0,0,3,1,0,4,5,1,2,2,3,3,4,4,5,5,6,
				0,4,0,3,6,0,5,4,1,6,4,2,3,5,0,5,0,0,0,0,6,1,2,1,2,0,3,0,
				1,0,3,0,1,2,0,4,0,6,0,0,2,1,6,4,2,3,3,6,5,0,0,0,0,5,4,5,
				5,1,0,5,0,6,3,4,1,0,6,3,0,0,2,0,3,0,0,0,5,4,1,2,6,4,2,0,
				0,2,3,4,6,0,0,0,1,2,6,0,1,5,3,6,0,4,1,5,0,0,3,4,0,0,5,2,
				6,1,0,0,1,4,4,3,6,0,5,6,3,0,0,0,5,2,4,0,2,1,0,0,2,5,0,3,
				5,0,4,1,0,0,5,4,0,5,0,2,0,3,1,1,0,0,0,2,4,2,6,3,0,3,6,6,
				0,6,5,5,3,2,0,0,2,1,5,3,4,0,0,6,1,3,1,2,0,0,4,4,6,0,0,0,
				0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,1,0,2,0,0,2,1,0,2 });

				PopulateBlock(29, new List<int>() {
				1,1,1,2,2,2,3,3,3,4,4,4,5,5,5,6,6,6,0,0,0,0,0,0,0,0,0,0,0,
				4,0,5,0,6,0,0,0,4,0,6,2,0,0,0,0,0,5,1,1,1,2,2,3,3,3,4,5,6,
				0,4,0,4,0,6,2,5,0,1,0,0,2,1,3,3,6,0,1,0,5,6,5,4,0,0,3,0,2,
				4,0,1,0,6,0,0,4,2,0,5,3,6,0,0,5,0,3,5,1,0,0,2,4,1,3,6,2,0,
				0,1,0,6,0,4,2,0,5,4,0,2,0,3,1,6,2,0,1,5,3,3,0,0,4,5,0,0,6,
				6,5,4,0,1,4,0,2,0,0,5,0,3,6,2,0,5,2,0,0,3,0,1,3,1,0,4,6,0,
				0,0,3,4,5,0,2,6,3,1,0,6,4,0,0,3,0,0,5,4,0,1,0,2,0,5,2,6,1,
				1,3,0,1,0,6,4,0,0,2,5,0,0,4,2,0,2,6,0,3,6,5,3,0,1,4,0,5,0,
				0,1,6,5,2,0,0,3,2,0,0,6,6,3,2,1,5,0,4,0,0,1,4,4,0,0,5,0,3,
				1,0,0,0,0,4,2,0,0,1,3,0,0,0,0,0,0,3,0,1,2,0,0,0,3,4,0,2,4 });

				PopulateBlock(30, new List<int>() {
				1,1,1,2,2,2,3,3,3,4,4,4,5,5,5,6,6,6,0,0,0,0,0,0,0,0,0,0,0,0,
				2,4,0,0,0,0,5,3,0,0,0,0,0,2,0,0,0,4,1,1,1,2,3,3,4,5,5,6,6,6,
				0,0,6,5,2,1,0,0,2,6,1,2,1,3,6,4,3,0,5,0,0,4,4,0,0,3,0,5,0,0,
				1,4,0,0,6,0,3,2,0,0,0,0,0,0,0,4,6,2,0,3,6,0,1,5,5,2,4,1,5,3,
				0,0,2,2,3,5,0,0,6,4,3,1,1,4,5,0,0,6,6,0,0,2,5,0,1,0,4,0,3,0,
				1,6,0,0,0,3,1,4,6,5,0,0,1,0,0,2,4,0,3,6,5,4,0,2,0,3,0,2,0,5,
				0,0,6,5,4,0,6,0,0,0,2,3,0,2,3,0,0,4,0,3,1,0,4,5,2,1,5,6,1,0,
				4,2,1,3,0,1,0,4,6,5,0,2,6,0,0,1,3,0,5,4,0,5,0,0,3,0,6,0,0,2,
				6,0,2,0,4,0,3,0,0,0,1,0,0,4,6,0,5,2,5,2,3,0,5,3,0,1,0,4,6,1,
				0,5,0,1,0,4,0,6,4,4,2,1,3,0,2,1,0,0,0,0,2,5,0,3,6,0,6,0,5,3 });
			}
			else
				return false;

			return true;
		}

		/// <summary>Create seeded blocks for 36 players and 4 games per team.</summary>
		private bool Round2Blocks(int rings)
		{
			if (rings == 3)
				PopulateBlock(36, new List<int>() {
				1,0,0,2,3,0,0,0,0,0,0,3,1,0,0,0,0,0,0,0,1,0,0,0,0,0,0,2,2,0,0,3,0,0,0,0,
				0,0,0,0,0,0,0,1,2,0,0,0,0,0,0,3,3,0,0,1,0,0,0,1,3,0,0,0,0,0,0,0,2,0,0,2,
				0,1,2,0,0,3,0,0,0,0,3,0,0,1,0,0,0,0,0,0,0,1,0,0,0,0,2,0,0,2,3,0,0,0,0,0,
				0,0,0,0,0,0,1,0,0,2,0,0,0,0,3,0,0,3,1,0,0,0,1,0,0,3,0,0,0,0,0,0,0,2,2,0,
				0,0,0,0,0,0,0,0,3,0,0,2,3,0,0,0,2,0,0,3,1,0,0,0,1,0,0,0,1,0,0,0,2,0,0,0,
				1,0,0,1,1,0,0,2,0,0,0,0,0,0,0,2,0,0,0,0,0,0,0,3,0,0,0,2,0,0,0,3,0,0,0,3,
				0,0,0,0,0,0,0,0,0,3,2,0,0,3,0,0,0,2,3,0,0,1,0,0,0,1,0,0,0,1,0,0,0,2,0,0,
				0,1,1,0,0,1,2,0,0,0,0,0,0,0,2,0,0,0,0,0,0,0,3,0,0,0,2,0,0,0,3,0,0,0,3,0,
				3,0,0,0,1,0,0,1,0,0,0,0,0,0,0,3,0,0,0,2,0,0,0,0,0,0,0,2,3,0,0,0,2,0,0,1,
				0,0,0,3,0,0,0,0,2,0,0,1,3,0,0,0,2,0,0,0,1,0,0,1,3,0,0,0,0,0,0,2,0,0,0,0,
				0,3,0,0,0,1,1,0,0,0,0,0,0,0,3,0,0,0,2,0,0,0,0,0,0,0,2,0,0,3,0,0,0,2,1,0,
				0,0,3,0,0,0,0,0,0,2,1,0,0,3,0,0,0,2,0,0,0,1,1,0,0,3,0,0,0,0,2,0,0,0,0,0,
				3,0,0,0,1,0,0,2,2,0,0,2,0,0,0,0,0,0,0,0,0,0,0,0,1,0,0,3,0,0,0,0,1,0,0,3,
				0,0,0,1,0,0,0,0,0,0,0,0,3,0,0,2,1,0,0,2,2,0,0,1,0,0,0,0,3,0,0,3,0,0,0,0,
				0,3,0,0,0,1,2,0,0,2,2,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,3,0,0,0,0,0,0,1,3,0,
				0,0,1,0,0,0,0,0,0,0,0,0,0,3,2,0,0,1,2,0,0,2,1,0,0,0,0,0,0,3,3,0,0,0,0,0 });
			else if (rings == 4)
				PopulateBlock(36, new List<int>() {
				1,0,1,0,0,0,0,2,3,0,3,2,0,4,0,4,2,0,0,0,0,4,0,0,0,0,0,3,0,0,0,0,0,0,0,1,
				0,0,0,1,0,1,0,0,0,0,0,0,0,0,0,0,0,3,0,1,0,0,0,4,4,3,3,0,4,2,0,2,0,0,2,0,
				1,0,0,0,4,0,1,0,0,3,0,0,3,0,4,0,4,0,3,0,2,0,0,0,0,0,0,0,0,0,0,0,2,1,0,2,
				0,2,0,0,0,0,0,1,4,0,0,0,0,1,0,0,0,0,0,4,0,0,1,0,3,0,2,3,2,0,3,4,0,0,0,0,
				0,0,1,2,1,4,3,0,0,0,0,2,4,0,0,0,0,0,0,0,3,0,0,0,0,3,0,0,0,1,0,0,4,0,2,0,
				4,4,0,0,0,0,0,0,0,2,4,0,0,0,2,1,0,2,0,1,0,0,1,3,0,0,0,0,0,0,0,0,0,3,0,3,
				0,0,3,0,2,1,0,4,1,0,0,0,0,2,0,0,0,0,3,0,1,0,0,0,0,2,4,0,3,0,4,0,0,0,0,0,
				0,0,0,0,0,0,2,0,0,3,0,3,2,0,0,0,4,0,0,4,0,2,1,0,0,0,0,1,0,1,0,0,3,0,0,4,
				0,0,3,3,0,0,0,0,0,0,2,0,0,0,4,1,0,0,1,0,0,0,0,3,0,0,1,0,4,0,2,0,0,4,2,0,
				1,4,0,0,0,2,0,4,0,0,0,3,0,0,0,0,0,3,0,0,1,2,3,0,4,0,0,0,0,1,0,2,0,0,0,0,
				0,0,0,0,4,0,2,0,2,4,0,0,3,3,0,0,1,0,1,0,0,0,0,2,0,0,0,0,0,0,4,0,0,1,3,0,
				0,2,0,1,0,0,0,0,0,0,4,0,0,0,4,2,0,1,0,0,0,3,0,0,4,3,0,2,0,0,0,1,3,0,0,0 });
			else if (rings == 5)
				PopulateBlock(36, new List<int>() {
				0,0,1,0,0,2,0,0,0,2,3,0,0,1,4,3,3,0,0,2,4,0,1,0,0,0,0,4,0,5,0,0,0,5,0,5,
				3,4,0,5,4,0,2,3,0,0,0,4,1,0,0,0,0,2,3,0,0,2,0,0,5,5,0,0,0,0,0,1,0,0,1,0,
				0,0,2,0,0,4,0,0,2,0,3,0,0,0,2,4,0,0,0,1,3,0,0,5,0,0,1,5,4,0,5,0,1,0,0,3,
				2,4,0,0,5,0,5,1,0,2,0,3,5,0,0,0,0,0,4,0,0,2,3,0,0,1,0,0,0,4,0,0,0,3,1,0,
				0,0,3,0,0,4,0,0,4,0,0,0,0,5,5,0,2,2,0,3,0,0,0,3,1,0,5,0,1,0,0,4,1,0,0,2,
				2,0,0,3,5,0,2,3,0,4,0,1,0,0,0,1,0,0,0,0,3,0,0,0,0,4,0,5,0,4,1,0,0,2,5,0,
				0,1,0,0,0,0,0,0,4,0,0,0,3,1,3,0,4,2,2,0,0,5,0,5,1,0,5,0,4,0,0,2,3,0,0,0,
				0,0,5,4,2,4,1,3,0,0,5,3,0,0,0,2,0,0,0,0,0,0,1,0,0,0,0,5,0,3,4,0,0,0,1,2,
				2,3,0,0,0,0,0,0,1,1,0,0,3,5,0,0,2,0,4,0,4,0,0,3,0,5,2,0,5,0,0,0,1,4,0,0,
				0,0,0,1,0,0,0,0,0,0,1,0,0,0,0,0,0,1,0,4,0,2,4,0,4,0,0,0,0,0,2,2,0,0,0,0 });
			else if (rings == 6)
				PopulateBlock(36, new List<int>() {
				1,0,0,2,3,0,0,4,5,0,0,3,1,0,0,6,6,0,0,4,1,0,0,4,6,0,0,2,2,0,0,3,5,0,0,5,
				0,1,2,0,0,3,4,0,0,5,3,0,0,1,6,0,0,6,4,0,0,1,4,0,0,6,2,0,0,2,3,0,0,5,5,0,
				4,0,0,4,4,0,0,5,3,0,0,2,3,0,0,5,2,0,0,3,1,0,0,6,1,0,0,5,1,0,0,6,2,0,0,6,
				0,4,4,0,0,4,5,0,0,3,2,0,0,3,5,0,0,2,3,0,0,1,6,0,0,1,5,0,0,1,6,0,0,2,6,0,
				3,0,0,6,1,0,0,1,5,0,0,4,6,0,0,3,5,0,0,2,4,0,0,4,6,0,0,2,3,0,0,5,2,0,0,1,
				0,3,6,0,0,1,1,0,0,5,4,0,0,6,3,0,0,5,2,0,0,4,4,0,0,6,2,0,0,3,5,0,0,2,1,0,
				3,0,0,4,1,0,0,2,2,0,0,2,6,0,0,5,4,0,0,5,5,0,0,4,1,0,0,3,6,0,0,6,1,0,0,3,
				0,3,4,0,0,1,2,0,0,2,2,0,0,6,5,0,0,4,5,0,0,5,4,0,0,1,3,0,0,6,6,0,0,1,3,0 });
			else
				return false;

			return true;
		}

		/// <summary>Create perfect round robin block for 9 players.</summary>
		private bool Round3Blocks(int rings)
		{
			if (rings >= 3)
			{
				PopulateBlock(9, new List<int>() {
				1,1,1,2,2,2,3,3,3,
				1,2,3,3,1,2,2,3,1,
				2,3,1,1,2,3,1,2,3,
				3,1,2,2,3,1,3,1,2 });
			}
			return rings >= 3;
		}

		/// <summary>Convert a list of integers to a two-dimensional grid of Colours, and add it to our dictionary of blocks.</summary>
		private void PopulateBlock(int players, List<int> plays)
		{
			var block = new RawBlock();
			int play = 0;
			int indexWithinGame = 0;
			int game = 0;

			block.Add(new RawGame());

			while (play < plays.Count)
			{
				if (indexWithinGame == players)
				{
					indexWithinGame = 0;
					game++;
					block.Add(new RawGame());
				}

				block[game].Add(plays[play]);

				play++;
				indexWithinGame++;
			}

			rawBlocks.Add(players, block);
		}

		private bool generated = false;

		private void GenerateBlock(int teams, int teamsPerRing, int gamesPerTeam, int rings)
		{
			if (rawBlocks.ContainsKey(teams))
				return;

			generated = true;
			var block = new RawBlock();
			int totalPlays = gamesPerTeam * teams;
			int plays = 0;
			int pointer = 0;

			while (plays < totalPlays)
				plays += block.AddNew(teams).Populate(ref pointer, teamsPerRing, (int)Math.Min(rings, Math.Ceiling((totalPlays - plays) * 1.0 / teamsPerRing)));

			rawBlocks.Add(teams, block);
		}
	}
}
