using System;
using System.Collections.Generic;
using System.Linq;

namespace DesktopPet
{
	/// <summary>
	/// Which animation each pet uses for the virtual pet actions (feed, pet, play, sleep), chosen by looking at the
	/// animations every pet really has. Pets not listed (gallery pets of other authors) use the common names.
	/// The first name a pet has wins.
	/// </summary>
	public static class PetActions
	{
		public enum Act { Feed, Pet, Play, Sleep }

		static readonly Dictionary<Act, string[]> Bat = new Dictionary<Act, string[]>
		{
			[Act.Feed] = new[] { "eat", "favourite food", "juice", "drink" },
			[Act.Pet] = new[] { "hug", "hearts", "love" },
			[Act.Play] = new[] { "loop the loop", "dance", "candy rain", "fireflies" },
			[Act.Sleep] = new[] { "sleep", "fly to the top" },
		};

		static readonly Dictionary<string, Dictionary<Act, string[]>> ByPet = new Dictionary<string, Dictionary<Act, string[]>>(StringComparer.OrdinalIgnoreCase)
		{
			["rex"] = new Dictionary<Act, string[]>
			{
				[Act.Feed] = new[] { "eat meat", "pizza", "popcorn", "drink" },
				[Act.Pet] = new[] { "in love", "love", "tail wag", "wink" },
				[Act.Play] = new[] { "soccer", "dance", "juggle", "basketball", "guitar", "party" },
				[Act.Sleep] = new[] { "sleep" },
			},
			["pingo"] = new Dictionary<Act, string[]>
			{
				[Act.Feed] = new[] { "eat fish", "hold fish" },
				[Act.Pet] = new[] { "love", "blow kiss", "giggle" },
				[Act.Play] = new[] { "dance", "ice skate", "juggle fish", "belly drum", "hop hop" },
				[Act.Sleep] = new[] { "sleep" },
			},
			["boo"] = new Dictionary<Act, string[]>
			{
				[Act.Feed] = new[] { "ice cream", "bubble gum", "jelly" },
				[Act.Pet] = new[] { "hearts", "wink kiss", "blow kiss", "giggle" },
				[Act.Play] = new[] { "yo-yo", "dance", "juggle", "spin", "bounce" },
				[Act.Sleep] = new[] { "sleep" },
			},
			["mago"] = new Dictionary<Act, string[]>
			{
				[Act.Feed] = new[] { "potion", "brew" },
				[Act.Pet] = new[] { "rabbit", "owl", "bow" },
				[Act.Play] = new[] { "juggle", "fireworks", "dance", "bubbles" },
				[Act.Sleep] = new[] { "sleep" },
			},
			["nuvi"] = new Dictionary<Act, string[]>
			{
				[Act.Feed] = new[] { "coffee", "download" },
				[Act.Pet] = new[] { "love", "happy", "wink" },
				[Act.Play] = new[] { "dance", "matrix", "happy" },
				[Act.Sleep] = new[] { "sleep" },
			},
			["claude"] = new Dictionary<Act, string[]>
			{
				[Act.Feed] = new[] { "eat", "coffee", "chef" },
				[Act.Pet] = new[] { "flower", "wave" },
				[Act.Play] = new[] { "dance", "skate", "music", "confetti", "spin" },
				[Act.Sleep] = new[] { "sleep" },
			},
			["arara"] = new Dictionary<Act, string[]>
			{
				[Act.Feed] = new[] { "mango", "brazil nut", "eat", "drink" },
				[Act.Pet] = new[] { "hug", "hearts", "love", "blow kiss" },
				[Act.Play] = new[] { "soccer ball", "samba", "pandeiro", "kite", "dance" },
				[Act.Sleep] = new[] { "sleep" },
			},
			["arara_azul"] = new Dictionary<Act, string[]>
			{
				[Act.Feed] = new[] { "mango", "brazil nut", "eat", "drink" },
				[Act.Pet] = new[] { "hug", "hearts", "love", "blow kiss" },
				[Act.Play] = new[] { "soccer ball", "samba", "pandeiro", "kite", "dance" },
				[Act.Sleep] = new[] { "sleep" },
			},
			["pipoca"] = new Dictionary<Act, string[]>
			{
				[Act.Feed] = new[] { "eat", "corn kernel", "drink" },
				[Act.Pet] = new[] { "hug", "hearts", "love" },
				[Act.Play] = new[] { "balloon", "dandelion", "dance", "chase butterfly" },
				[Act.Sleep] = new[] { "sleep", "nap in shell" },
			},
			["sapo"] = new Dictionary<Act, string[]>
			{
				[Act.Feed] = new[] { "eat", "catch fly", "catch mosquito", "drink" },
				[Act.Pet] = new[] { "hug", "hearts", "frog prince", "love" },
				[Act.Play] = new[] { "bubbles", "long jump", "rain dance", "dance" },
				[Act.Sleep] = new[] { "sleep" },
			},
			// the three bats (Morceguinho) share their animations
			["morcego_roxo"] = Bat, ["morcego_vampiro"] = Bat, ["morcego_frutas"] = Bat,
			["cacto_vaso"] = new Dictionary<Act, string[]>
			{
				[Act.Feed] = new[] { "eat", "drink", "cookie", "ice cream" },
				[Act.Pet] = new[] { "hug", "hearts", "love", "watering can" },
				[Act.Play] = new[] { "balloon", "bubbles", "dance", "roll" },
				[Act.Sleep] = new[] { "sleep" },
			},
			["mandacaru"] = new Dictionary<Act, string[]>
			{
				[Act.Feed] = new[] { "cuscuz", "eat", "tapioca", "rapadura", "drink", "cookie" },
				[Act.Pet] = new[] { "hug", "hearts", "love", "tip hat" },
				[Act.Play] = new[] { "forró", "dance", "triangle", "hat trick", "spinning top", "music" },
				[Act.Sleep] = new[] { "sleep" },
			},
			["esheep64"] = new Dictionary<Act, string[]>
			{
				[Act.Feed] = new[] { "eat" },
				[Act.Pet] = new[] { "flower", "boing" },
				[Act.Play] = new[] { "jump", "boing", "run" },
				[Act.Sleep] = new[] { "sleep1a", "sleep2a", "sleep3a" },
			},
		};

		/// <summary>For the pets of other authors: the usual names.</summary>
		static readonly Dictionary<Act, string[]> Common = new Dictionary<Act, string[]>
		{
			[Act.Feed] = new[] { "eat", "eating", "drink", "food", "eat fish", "eat meat", "pizza", "cookie" },
			[Act.Pet] = new[] { "love", "in love", "hearts", "happy", "purr", "wave", "hello" },
			[Act.Play] = new[] { "play", "dance", "jump", "spin", "juggle", "run", "cheer" },
			[Act.Sleep] = new[] { "sleep", "sleeping", "nap", "sleep1a", "rest", "yawn" },
		};

		/// <summary>The animation names to try for an action of the pet on screen (its own first, then the usual ones).</summary>
		public static string[] Names(Act act, string folder = null)
		{
			folder = folder ?? Program.MyData.CurrentPetKey();
			var own = ByPet.TryGetValue(folder, out var map) && map.TryGetValue(act, out var names) ? names : new string[0];
			return own.Concat(Common[act]).Distinct().ToArray();
		}

		/// <summary>Pets that have their own list (the self-test checks that every name exists).</summary>
		public static IEnumerable<string> KnownPets => ByPet.Keys;

		public static string[] Own(string folder, Act act) => ByPet.TryGetValue(folder, out var m) && m.TryGetValue(act, out var n) ? n : new string[0];
	}
}
