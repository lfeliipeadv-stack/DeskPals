using System;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;

namespace DesktopPet
{
	internal static partial class SelfTest
	{
		/// <summary>
		/// desktopPet 2.1 "interact": the &lt;interact&gt; next animations are read (trigger, direction, place, a missing
		/// trigger never matches), and the choice follows the other pet's animation, its side and the place. Then the
		/// children of a pet leave its list when they close, and no child is made beyond the limit of pet windows.
		/// </summary>
		static async Task InteractCheck()
		{
			// the eSheep that comes with the program, with an <interact> in its first animation
			var doc = new XmlDocument();
			doc.LoadXml(BuiltInPets.GetXml(BuiltInPets.Folders.First()));
			var ns = new XmlNamespaceManager(doc.NameTable);
			ns.AddNamespace("p", doc.DocumentElement.NamespaceURI);
			var anims = doc.SelectNodes("//p:animations/p:animation", ns).Cast<XmlElement>().ToList();
			var first = anims[0];
			int a1 = int.Parse(first.GetAttribute("id")), a2 = int.Parse(anims[1].GetAttribute("id")), a3 = int.Parse(anims[2].GetAttribute("id"));
			string uri = doc.DocumentElement.NamespaceURI;
			var interact = doc.CreateElement("interact", uri);
			XmlElement Next(int value, int probability, string only, string trigger, string direction)
			{
				var n = doc.CreateElement("next", uri);
				n.SetAttribute("probability", probability.ToString());
				if (only != null) n.SetAttribute("only", only);
				if (trigger != null) n.SetAttribute("trigger", trigger);
				if (direction != null) n.SetAttribute("direction", direction);
				n.InnerText = value.ToString();
				return n;
			}
			interact.AppendChild(Next(a2, 10, "none", a3.ToString(), "infront"));   // when the other walks in front: a2
			interact.AppendChild(Next(a3, 10, "taskbar", a2.ToString(), null));      // when the other plays a2, only on the taskbar: a3
			interact.AppendChild(Next(a3, 0, "none", a3.ToString(), "any"));         // probability 0: never
			interact.AppendChild(Next(a2, 10, "none", null, null));                  // no trigger: never
			first.AppendChild(interact);

			var xml = new Xml(1) { AnimationXML = XmlData.AnimationXML.ParseXML(doc.OuterXml) };
			var animations = new Animations(xml);
			xml.LoadAnimations(animations);
			var me = animations.SheepAnimations[a1];
			Check("interact: the <interact> lines are read", me.EndInteract.Count == 4, me.EndInteract.Count.ToString());
			Check("interact: trigger and direction are read", me.EndInteract[0].TriggerID == a3 && me.EndInteract[0].Direction == TNextAnimation.TDirection.INFRONT
				&& me.EndInteract[1].Direction == TNextAnimation.TDirection.ANY && me.EndInteract[1].only == TNextAnimation.TOnly.TASKBAR);
			Check("interact: no trigger never matches", me.EndInteract[3].TriggerID == -1);

			var T = TNextAnimation.TOnly.NONE; var IN = TNextAnimation.TDirection.INFRONT; var BH = TNextAnimation.TDirection.BEHIND;
			var v1 = FormPet.ValidInteract(me, T, IN, a3);
			Check("interact: the other's animation in front triggers it", v1.Count == 1 && v1[0].ID == a2, string.Join(",", v1.Select(x => x.ID)));
			Check("interact: behind, an \"in front\" line does not play", FormPet.ValidInteract(me, T, BH, a3).Count == 0);
			Check("interact: a \"taskbar\" line only on the taskbar", FormPet.ValidInteract(me, T, IN, a2).Count == 0 && FormPet.ValidInteract(me, TNextAnimation.TOnly.TASKBAR, BH, a2).Count == 1);
			Check("interact: an animation that triggers nothing", FormPet.ValidInteract(me, T, IN, 9999).Count == 0);
			int chosen = animations.SetNextInteractAnimation(FormPet.ValidInteract(me, TNextAnimation.TOnly.TASKBAR, BH, a2));
			Check("interact: the chosen one plays even with its place (taskbar)", chosen == a3, chosen.ToString());
			Check("interact: the normal next animations are not changed", animations.SheepAnimations[a1].EndAnimation.Count == XmlData.AnimationXML.ParseXML(BuiltInPets.GetXml(BuiltInPets.Folders.First())).Animations.Animation[0].Sequence.Next.Length);

			// children: closed ones leave the list; the limit of pet windows
			var pet = System.Windows.Forms.Application.OpenForms.OfType<FormPet>().FirstOrDefault(p => p.Visible && !p.IsChildWindow);
			int withChild = pet == null ? -1 : pet.TestAnimationWithChild();
			if (pet == null || withChild <= 0) { lines.Add("SKIP  interact: the pet on screen has no animation with a child"); return; }
			int before = pet.ChildCount;
			pet.TestPlay(withChild);
			await Task.Delay(800);
			int made = pet.ChildCount - before;
			Check("children: the child is made", made > 0, made.ToString());
			foreach (var c in pet.LiveChildren) c.Close();
			await Task.Delay(500);
			Check("children: a closed child leaves the list", pet.ChildCount == 0, pet.ChildCount.ToString());
			int max = FormPet.MaxPets;
			try
			{
				FormPet.MaxPets = System.Windows.Forms.Application.OpenForms.OfType<FormPet>().Count();
				pet.TestPlay(withChild);
				await Task.Delay(500);
				Check("children: no child beyond the limit of pet windows", pet.ChildCount == 0, pet.ChildCount.ToString());
			}
			finally { FormPet.MaxPets = max; }
		}
	}
}
