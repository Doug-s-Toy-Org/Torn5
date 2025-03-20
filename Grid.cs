﻿using System;
using System.Collections.Generic;
using System.Linq;
using Torn;

namespace Torn5
{
	class RingGrid
	{
		public void GenerateRingGrid(League league, Fixture fixture, List<LeagueTeam> teams, int numRings, int gamesPerTeam, DateTime firstGameDateTime, int minutesBetweenGames)
		{
			fixture.Games.Clear();

			if (numRings < 5 || numRings > 6)
				return;

			PopulateBlocks(numRings);

			int smallestBlockSize = rawBlocks.Keys.Min();
			int largestBlockSize = rawBlocks.Keys.Max();

			if ((smallestBlockSize <= teams.Count && teams.Count <= largestBlockSize) || smallestBlockSize * 2 <= teams.Count)  // We can handle if teams.Count is the size of a single block, or if teams.Count is large enough that we can use two or more blocks.
			{
				fixture.Teams.Clear();
				fixture.Teams.Populate(teams);

				int remainingPlayers = teams.Count;
				int remainingBlocks = teams.Count / smallestBlockSize;
				var blockSizes = new List<int>();

				int niceMultiple = numRings % 2 == 0 ? numRings / 2 : numRings;  // If there are _n_ rings, then it's nice to choose blocks that have a multiple of _n_ players, because each game is perfectly full. Special case: if _n_ is even, then half _n_ works fine, because each player plays 6 times, and, um, maths.
				int firstBlock = ((remainingPlayers / remainingBlocks / niceMultiple) * niceMultiple) + remainingPlayers % niceMultiple;  // Put all the non-multiple-of-_n_-ness in one block, so every other block can be nice.
				blockSizes.Add(firstBlock);
				remainingPlayers -= firstBlock;
				remainingBlocks--;

				// Figure out the sizes we will use for the remaining blocks.
				while (remainingPlayers > 0)
				{
					int nextBlock = (remainingPlayers / remainingBlocks / niceMultiple) * niceMultiple;
					blockSizes.Add(nextBlock);
					remainingPlayers -= nextBlock;
					remainingBlocks--;
				}

				if (blockSizes.Any(b => !rawBlocks.ContainsKey(b)))
				{
					// We have failed, because the cleverness to make most block sizes nice multiples of _n_ has caused us to have at least one block that is not in our dictionary of blocks available to use.
					// So try again, without the cleverness.

					remainingPlayers = teams.Count;
					remainingBlocks = teams.Count / smallestBlockSize;
					blockSizes.Clear();

					while (remainingPlayers > 0)
					{
						int nextBlock = remainingPlayers / remainingBlocks;
						blockSizes.Add(nextBlock);
						remainingPlayers -= nextBlock;
						remainingBlocks--;
					}
				}

				if (blockSizes.Any(b => !rawBlocks.ContainsKey(b)))
				{
					Console.WriteLine("Could not generate ring grid for {0} teams, {1} rings, [2} games each.", teams.Count, numRings, gamesPerTeam);
					return;
				}

				blockSizes.Sort((x, y) => y - x);  // Largest first.

				// Build the blocks of FixtureGames.
				var fixtureBlocks = new List<FixtureGames>();
				int teamOffset = 0;
				foreach (var blockSize in blockSizes)
				{
					fixtureBlocks.Add(RawToFixture(teams, blockSize, teamOffset));
					teamOffset += blockSize;
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
			}
		}

		/// <summary>Convert a RawBlock to a FixtureGame with actual teams filled in.</summary>
		private FixtureGames RawToFixture(List<LeagueTeam> teams, int blockSize, int teamOffset)
		{
			var games = new FixtureGames();
			var block = rawBlocks[blockSize];
			foreach (var game in block)
			{
				var fg = new FixtureGame();

				for (int i = 0; i < game.Count; i++)
				{
					if (game[i] != Colour.None)  // Colour.None is a bye for this player.
						fg.Teams.Add(teams[i + teamOffset], game[i]);
				}

				games.Add(fg);
			}
			return games;
		}

		/// <summary>A list of colours we will use for players in a single game, indicating which colour ring they are playing in, with Colour.None indicating a bye.</summary>
		class RawGame : List<Colour> { }

		/// <summary>Each block is a compact group of players playing in games. Each row is a game; each column is a player.
		/// For example, the first block below is 18 players in 6 games, with each player playing 6 times.</summary>
		class RawBlock : List<RawGame> { }

		readonly Dictionary<int, RawBlock> rawBlocks = new Dictionary<int, RawBlock>();

		/// <summary>Create the blocks that we will build the ring grid fixture out of.
		/// The number indicates which colour ring they are playing in, with 0 indicating a bye -- we'll convert the numbers to Colours in PopulateBlock.</summary>
		void PopulateBlocks(int rings)
		{
			if (rings == 5)
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

				PopulateBlock(23, new List<int>() {
				1,1,1,2,2,2,3,3,3,4,4,4,5,5,5,0,0,0,0,0,0,0,0,
				0,1,4,5,3,0,3,4,0,0,0,2,0,0,5,1,1,2,2,3,4,5,0,
				5,1,0,2,0,1,0,0,2,4,3,0,4,1,0,5,3,2,0,0,3,4,5,
				0,2,5,0,2,1,4,2,3,5,1,3,1,4,5,0,0,0,4,3,0,0,0,
				4,0,2,3,1,0,0,0,1,5,3,0,0,5,0,1,4,2,0,3,5,4,2,
				0,2,0,0,0,4,4,1,2,0,0,5,0,1,3,4,5,0,2,3,3,1,5,
				1,0,4,5,4,1,3,2,0,2,4,0,5,0,3,0,0,1,0,2,5,0,3,
				4,3,5,0,2,0,0,0,4,4,1,2,3,0,2,1,5,3,5,0,0,1,0,
				2,0,0,3,0,5,1,4,0,0,0,3,4,3,0,4,1,1,2,2,5,0,5,
				0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,0,0,1,1 });

				PopulateBlock(24, new List<int>() {
				1,1,1,2,2,2,3,3,3,4,4,4,5,5,5,0,0,0,0,0,0,0,0,0,
				0,0,0,0,2,4,0,5,0,0,1,5,1,0,0,1,2,2,3,3,3,4,4,5,
				5,2,1,1,0,0,2,0,3,5,0,0,0,4,5,3,4,0,3,1,0,4,0,2,
				1,0,5,0,5,3,1,4,0,0,3,2,5,0,0,4,0,2,0,4,1,0,2,3,
				4,1,2,5,0,0,0,0,1,3,0,4,0,4,5,0,2,3,2,0,5,3,1,0,
				5,4,0,0,3,4,2,0,0,0,2,0,4,0,3,1,0,5,5,2,1,1,0,3,
				0,5,1,5,4,2,0,4,2,2,0,3,0,1,3,3,5,0,0,0,4,0,1,0,
				1,0,0,4,0,0,3,2,0,5,2,0,1,3,0,0,2,4,3,5,0,1,5,4,
				0,3,2,0,3,5,0,0,1,0,0,1,4,0,2,5,1,5,0,3,4,2,0,4,
				0,0,0,1,0,0,1,2,3,2,3,0,0,3,0,0,0,0,2,0,0,0,1,0 });
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
			}
		}

		/// <summary>Convert a list of integers to a two-dimensional grid of Colours, and add it to our dictionary of blocks.</summary>
		void PopulateBlock(int players, List<int> plays)
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

				block[game].Add((Colour)plays[play]);

				play++;
				indexWithinGame++;
			}

			rawBlocks.Add(players, block);
		}
	}
}
