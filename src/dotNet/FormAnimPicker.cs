using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace DesktopPet
{
    /// <summary>
    /// Easter egg window: every animation of the current pet as a tile; click a tile to play it.
    /// Opened by middle-clicking the pet. It stays open so several animations can be tried in a row;
    /// Esc, the close button or clicking elsewhere closes it.
    /// </summary>
    sealed class FormAnimPicker : Form
    {
        // ---------------------------------------------------------------- data
        readonly Action<int> play;
        readonly Action<int> playSpawn;
        readonly List<(Rectangle rect, string text)> headers = new List<(Rectangle, string)>();
        readonly Image petIcon;
        readonly string petName;
        readonly List<Tile> allTiles = new List<Tile>();
        List<Tile> tiles = new List<Tile>();
        int playingId = -1;
        Tile playingTile;

        sealed class Tile
        {
            public int Id;
            public string Name;
            public string Emoji;
            public Image Pic;           // a frame of the animation itself (the emoji when there is none)
            public int Spawn = -1;      // an entrance: index of the spawn to play (instead of an animation)
            public string Original;     // the name in the pet's file (the search finds both)
            public Rectangle Rect;      // in content coordinates (before scrolling)
        }

        // ---------------------------------------------------------------- layout (96 dpi units, scaled by S)
        const int WIDTH = 468, PAD = 16, HEADER_H = 68, SEARCH_H = 34, FOOTER_H = 34;
        const int COLS = 3, TILE_H = 44, GAP = 8, MAX_ROWS = 7, SECTION_H = 24;

        readonly float scale;
        int S(int v) => (int)Math.Round(v * scale);

        Rectangle closeRect, searchRect, listRect;
        int contentHeight, scrollY;
        int hoverTile = -1;
        bool hoverClose;

        readonly TextBox search;
        readonly ToolTip tip = new ToolTip { InitialDelay = 400, ReshowDelay = 100 };   // the whole name when the tile cuts it
        readonly Font fontTitle, fontSub, fontTile, fontSmall, fontEmoji, fontSearch;

        static readonly Dictionary<string, string> Emojis = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["walk"] = "🚶", ["run"] = "🏃", ["turn"] = "↩", ["jump"] = "⤴", ["leap"] = "🦘",
            ["fall"] = "⬇", ["fall soft"] = "🪶", ["fall hard"] = "💥", ["fall fast"] = "💨",
            ["climb"] = "🧗", ["drag"] = "✋", ["toss"] = "🤾", ["blink"] = "😑", ["sleep"] = "💤",
            ["wake up"] = "☀", ["sit"] = "🪑", ["wave"] = "👋", ["dance"] = "💃", ["spin"] = "🌀",
            ["eat"] = "🍪", ["flower"] = "🌸", ["think"] = "💭", ["surprise"] = "😲", ["scared"] = "😱",
            ["angry"] = "😡", ["sneak"] = "🤫", ["stretch"] = "🤸", ["point"] = "☝", ["look around"] = "👀",
            ["look down"] = "🔽", ["lean look"] = "🫣", ["sync"] = "🎉", ["kill"] = "💀", ["flag"] = "🚩",
            ["confetti"] = "🎊", ["gym"] = "🏋", ["laptop"] = "💻", ["top hat"] = "🎩", ["coffee"] = "☕",
            ["music"] = "🎵", ["chef"] = "🍳", ["skate"] = "🛹", ["builder"] = "🔨", ["detective"] = "🔍",
            ["idea"] = "💡", ["spell"] = "🪄", ["magic"] = "🪄", ["teleport"] = "✨", ["book"] = "📖",
            ["read"] = "📖", ["crystal ball"] = "🔮", ["type"] = "⌨", ["typing"] = "⌨", ["code"] = "⌨",
            ["loading"] = "⏳", ["error"] = "⚠", ["talk"] = "💬", ["rain"] = "🌧", ["nap"] = "💤",
        };

        // ---------------------------------------------------------------- construction
        public FormAnimPicker(Animations animations, string currentPetName, Image icon, Action<int> onPlay, Action<int> onSpawn = null, Func<int, Image> spriteOf = null)
        {
            play = onPlay;
            playSpawn = onSpawn;
            petIcon = icon;
            petName = string.IsNullOrWhiteSpace(currentPetName) ? "Pet" : currentPetName;

            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            ShowInTaskbar = false;
            TopMost = true;
            KeyPreview = true;
            BackColor = Theme.Background;
            AutoScaleMode = AutoScaleMode.None;
            Text = Lang.T("picker.title", petName);

            using (var g = CreateGraphics()) scale = g.DpiX / 96f;
            fontTitle = Theme.CreateFont(16 * scale, FontStyle.Bold);
            fontSub = Theme.CreateFont(12 * scale);
            fontTile = Theme.CreateFont(13 * scale);
            fontSmall = Theme.CreateFont(11 * scale);
            fontSearch = Theme.CreateFont(13 * scale);
            fontEmoji = new Font("Segoe UI Emoji", 15 * scale, FontStyle.Regular, GraphicsUnit.Pixel);

            var hidden = HiddenAnimations(animations);
            // the entrances (spawns), as the program lists them: how the pet comes onto the screen, with their chance
            var sceneStarts = new HashSet<int>();
            if (onSpawn != null && animations?.SheepSpawn != null && animations.SheepAnimations != null)
            {
                var targets = animations.SheepAnimations.Values.SelectMany(a => (a.EndAnimation ?? new List<TNextAnimation>())
                    .Concat(a.EndBorder ?? new List<TNextAnimation>()).Concat(a.EndGravity ?? new List<TNextAnimation>())
                    .Where(n => n.ID != a.ID).Select(n => n.ID)).ToHashSet();
                int index = 0;
                foreach (var sp in animations.SheepSpawn.Values)
                {
                    // two entrances that start with the same animation (from either side) are one tile: the first one
                    if (animations.SheepAnimations.TryGetValue(sp.Next, out var first) && !allTiles.Any(x => x.Spawn >= 0 && x.Id == sp.Next))
                    {
                        string name = string.IsNullOrWhiteSpace(first.Name) ? "#" + sp.Next : first.Name;
                        Emojis.TryGetValue(name, out string emoji);
                        allTiles.Add(new Tile { Id = sp.Next, Name = AnimNames.Show(name), Original = name, Emoji = emoji ?? "🚪", Spawn = index });
                        if (!targets.Contains(sp.Next)) sceneStarts.Add(sp.Next);   // only an entrance starts it: not repeated below
                    }
                    index++;
                }
            }
            if (animations?.SheepAnimations != null)
                foreach (var kv in animations.SheepAnimations.OrderBy(kv => kv.Key))
                {
                    // a child window's animation, the middle of a scene, or an entrance (listed above)
                    if (hidden.Contains(kv.Key) || sceneStarts.Contains(kv.Key)) continue;
                    string name = string.IsNullOrWhiteSpace(kv.Value.Name) ? "#" + kv.Key : kv.Value.Name;
                    Emojis.TryGetValue(name, out string emoji);
                    allTiles.Add(new Tile { Id = kv.Key, Name = AnimNames.Show(name), Original = name, Emoji = emoji ?? "✨" });
                }

            // each tile shows the pet doing it: the middle frame of the animation (it shows the action better than the
            // first one, often the pet just standing), or the nearest frame that is not empty
            if (spriteOf != null && animations?.SheepAnimations != null)
                foreach (var t in allTiles)
                    if (animations.SheepAnimations.TryGetValue(t.Id, out var a) && a.Sequence.Frames != null && a.Sequence.Frames.Count > 0)
                    {
                        var frames = a.Sequence.Frames;
                        int mid = frames.Count / 2;
                        foreach (int i in Enumerable.Range(0, frames.Count).OrderBy(i => Math.Abs(i - mid)))
                        {
                            var img = spriteOf(frames[i]);
                            if (img != null && !IsEmpty(img)) { t.Pic = img; break; }
                        }
                    }

            search = new TextBox
            {
                BorderStyle = BorderStyle.None,
                Font = fontSearch,
                BackColor = Theme.Surface,
                ForeColor = Theme.Text,
                PlaceholderText = Lang.T("picker.search"),
            };
            search.TextChanged += (s, e) => { scrollY = 0; Filter(); };
            search.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter && tiles.Count > 0) { Play(tiles[0]); e.SuppressKeyPress = true; }
                else if (e.KeyCode == Keys.Down) { ScrollBy(S(TILE_H + GAP)); e.SuppressKeyPress = true; }
                else if (e.KeyCode == Keys.Up) { ScrollBy(-S(TILE_H + GAP)); e.SuppressKeyPress = true; }
            };
            search.GotFocus += (s, e) => Invalidate();
            search.LostFocus += (s, e) => Invalidate();
            Controls.Add(search);

            Theme.ApplyWindowChrome(this);
            Filter();

            MouseMove += OnMouseMove;
            MouseDown += OnMouseDown;
            MouseWheel += (s, e) => ScrollBy(-e.Delta / 120 * S(TILE_H + GAP));
            MouseLeave += (s, e) => { hoverTile = -1; hoverClose = false; Invalidate(); };
            KeyDown += (s, e) => { if (e.KeyCode == Keys.Escape) Close(); };
            // clicking another program closes it; a window of the pet itself (a child window like the tub, a bubble)
            // that takes the focus when an animation starts does not
            Deactivate += (s, e) => { if (!IsDisposed) BeginInvoke(new Action(CloseIfAnotherProgram)); };
            // The search box is not focused at first, so its placeholder stays visible;
            // typing anywhere in the window starts the search.
            Shown += (s, e) => ActiveControl = null;
            KeyPress += (s, e) =>
            {
                if (search.Focused || char.IsControl(e.KeyChar)) return;
                search.Focus();
                search.AppendText(e.KeyChar.ToString());
                e.Handled = true;
            };
        }

        /// <summary>
        /// Animations left out of the picker, so that every tile plays a whole scene:
        /// - what only a child window plays (the tub, a released balloon, the king of the gSheep…): played on the
        ///   pet itself it would turn the pet into the object;
        /// - the middle of a scene: an animation that comes only as the one possible continuation of another
        ///   (the splash after the dive, the head in the water, getting out of the tub). The tile of the start plays it.
        /// </summary>
        internal static HashSet<int> HiddenAnimations(Animations animations)
        {
            var hidden = new HashSet<int>();
            var all = animations?.SheepAnimations;
            if (all == null) return hidden;

            var links = new List<(int from, int to, List<TNextAnimation> list, TNextAnimation n, bool gravity)>();
            foreach (var kv in all)
            {
                foreach (var n in kv.Value.EndAnimation ?? new List<TNextAnimation>()) links.Add((kv.Key, n.ID, kv.Value.EndAnimation, n, false));
                foreach (var n in kv.Value.EndBorder ?? new List<TNextAnimation>()) links.Add((kv.Key, n.ID, kv.Value.EndBorder, n, false));
                foreach (var n in kv.Value.EndGravity ?? new List<TNextAnimation>()) links.Add((kv.Key, n.ID, kv.Value.EndGravity, n, true));
            }
            var nexts = links.Where(l => l.n.Probability > 0 && all.ContainsKey(l.to)).ToLookup(l => l.from, l => l.to);
            var preds = links.Where(l => l.from != l.to).ToLookup(l => l.to);

            HashSet<int> Reach(IEnumerable<int> roots)
            {
                var seen = new HashSet<int>();
                var stack = new Stack<int>(roots.Where(all.ContainsKey));
                while (stack.Count > 0)
                {
                    int id = stack.Pop();
                    if (!seen.Add(id)) continue;
                    foreach (int n in nexts[id]) stack.Push(n);
                }
                return seen;
            }

            var childRoots = (animations.SheepChild?.Values.SelectMany(l => l).Select(c => c.Next) ?? Enumerable.Empty<int>()).ToList();
            var childReach = Reach(childRoots);
            var spawnFirsts = (animations.SheepSpawn?.Values.Select(s => s.Next) ?? Enumerable.Empty<int>()).ToHashSet();
            var special = new[] { animations.AnimationDrag, animations.AnimationFall, animations.AnimationKill, animations.AnimationSync,
                animations.AnimationToss, animations.AnimationFallSoft, animations.AnimationFallHard };
            // the pet's own roots: spawns, the special animations, and the ones nothing leads to (played by name, like
            // the reactions of the menu) — unless such an orphan only goes on into what a child plays
            var roots = spawnFirsts.Concat(special).ToList();
            foreach (var id in all.Keys)
                if (!preds[id].Any() && !childRoots.Contains(id))
                {
                    var to = nexts[id].ToList();
                    if (to.Count == 0 || !to.All(childReach.Contains)) roots.Add(id);
                }
            if (spawnFirsts.Count == 0 && all.Count > 0) roots.Add(all.Keys.Min());
            var mine = Reach(roots);
            foreach (var id in all.Keys) if (!mine.Contains(id)) hidden.Add(id);

            // the middle of a scene: every way in (from the pet's own animations, not counting falls) is the only
            // animation that can follow in that situation
            string[] seqWhere = { "none", "taskbar", "window" };
            string[] borderWhere = { "vertical", "windowSide", "window", "taskbar", "horizontal", "petSide" };
            bool Forced((int from, int to, List<TNextAnimation> list, TNextAnimation n, bool gravity) l, bool border)
            {
                bool any = false;
                foreach (var where in border ? borderWhere : seqWhere)
                {
                    if (!Eligible(l.n.only, where)) continue;
                    any = true;
                    if (l.list.Where(o => o.Probability > 0 && Eligible(o.only, where)).Any(o => o.ID != l.to)) return false;
                }
                return any;
            }
            foreach (var id in mine)
            {
                if (spawnFirsts.Contains(id) || special.Contains(id)) continue;
                var ways = preds[id].Where(l => !l.gravity && mine.Contains(l.from)).ToList();
                if (ways.Count > 0 && ways.All(l => Forced(l, ReferenceEquals(l.list, all[l.from].EndBorder)))) hidden.Add(id);
            }

            // the phases of a scene: everything that can only be reached through its first animation ("pissa" →
            // "pissb" or "pissb_2"), when that first animation always goes on into them and they never come back to it
            // (walking goes everywhere and everything comes back to it: it is not a scene)
            HashSet<int> ReachAvoiding(Func<int, bool> blocked)
            {
                var seen = new HashSet<int>();
                var stack = new Stack<int>(roots.Where(r => all.ContainsKey(r) && !blocked(r)));
                while (stack.Count > 0)
                {
                    int id = stack.Pop();
                    if (blocked(id) || !seen.Add(id)) continue;
                    foreach (int n in nexts[id]) stack.Push(n);
                }
                return seen;
            }
            foreach (var head in mine)
            {
                var goesOn = (all[head].EndAnimation ?? new List<TNextAnimation>())
                    .Where(n => n.Probability > 0 && n.ID != head && all.ContainsKey(n.ID)).Select(n => n.ID).Distinct().ToList();
                if (goesOn.Count == 0) continue;
                var without = ReachAvoiding(id => id == head);
                var scene = mine.Where(id => id != head && !without.Contains(id)).ToHashSet();
                if (scene.Count == 0 || !goesOn.All(scene.Contains) || scene.Any(id => nexts[id].Contains(head))) continue;
                foreach (var id in scene) if (!spawnFirsts.Contains(id)) hidden.Add(id);
            }

            // the same idea by name: "run_begin" → "run_end", "chasew1" → "chasew2", "fall_wina" → "fall_winb": a phase
            // that can only be reached through another animation of its family. An animation that branches into many
            // others (a walk) is a behaviour of its own, not a phase.
            var stems = all.ToDictionary(kv => kv.Key, kv => Stem(kv.Value.Name));
            foreach (var id in mine)
            {
                if (hidden.Contains(id) || spawnFirsts.Contains(id) || special.Contains(id) || stems[id].Length == 0) continue;
                if ((all[id].EndAnimation ?? new List<TNextAnimation>()).Select(n => n.ID).Distinct().Count() >= 5) continue;
                var family = all.Keys.Where(k => k != id && stems[k] == stems[id]).ToHashSet();
                if (family.Count == 0 || !preds[id].Any(l => family.Contains(l.from))) continue;
                if (!ReachAvoiding(family.Contains).Contains(id)) hidden.Add(id);
            }

            // the engine's own states (held, falling, landing, the farewell) and the reactions that only happen when the
            // pet bumps into something or falls (never chosen when an animation ends) are not scenes to start
            // (an engine state that the file does not name defaults to animation 1: only the named ones count)
            var states = special.Where(id => all.TryGetValue(id, out var s)
                && new[] { "drag", "fall", "kill", "sync", "toss", "fall soft", "fall hard" }.Contains(s.Name)).ToHashSet();
            hidden.UnionWith(states);
            foreach (var id in mine)
            {
                var ways = preds[id].Where(l => mine.Contains(l.from)).ToList();
                if (ways.Count > 0 && !ways.Any(l => ReferenceEquals(l.list, all[l.from].EndAnimation) && !states.Contains(l.from)))
                    hidden.Add(id);
            }
            // and what the pet's file marks as the continuation of a scene
            foreach (var kv in all) if (kv.Value.HideInPicker) hidden.Add(kv.Key);
            return hidden;
        }

        /// <summary>The family part of a name: "pissa"/"pissb" → "piss", "run_begin"/"run_end" → "run", "chasew2" → "chasew".</summary>
        static string Stem(string name)
        {
            string n = (name ?? "").Trim().ToLowerInvariant();
            n = System.Text.RegularExpressions.Regex.Replace(n, "(_start|_begin|_end|_stop)$", "");
            n = System.Text.RegularExpressions.Regex.Replace(n, "[ _]?[0-9]+$", "");
            if (n.Length > 3) n = System.Text.RegularExpressions.Regex.Replace(n, "(?<=[a-z])[a-e]$", "");
            return n.TrimEnd(' ', '_');
        }

        static readonly Dictionary<Image, bool> emptyFrames = new Dictionary<Image, bool>();

        /// <summary>A blank frame (some scenes start with one, the pet is invisible): nothing to show on a tile.</summary>
        static bool IsEmpty(Image img)
        {
            if (emptyFrames.TryGetValue(img, out bool empty)) return empty;
            empty = true;
            try
            {
                if (img is Bitmap bmp)
                    for (int y = 0; y < bmp.Height && empty; y += 2)
                        for (int x = 0; x < bmp.Width; x += 2)
                        {
                            var c = bmp.GetPixel(x, y);
                            if (c.A > 0 && !(c.R == 255 && c.G == 0 && c.B == 255)) { empty = false; break; }
                        }
                else empty = false;
            }
            catch (Exception) { empty = false; }
            emptyFrames[img] = empty;
            return empty;
        }

        /// <summary>The engine's rule (Animations.SetNextGeneralAnimation): may this "only" be chosen in that situation?</summary>
        static bool Eligible(TNextAnimation.TOnly only, string where)
        {
            switch (only)
            {
                case TNextAnimation.TOnly.NONE: return true;
                case TNextAnimation.TOnly.HORIZONTAL_: return where == "horizontal" || where == "window" || where == "taskbar";
                case TNextAnimation.TOnly.VERTICAL_: return where == "vertical" || where == "windowSide" || where == "petSide";
                case TNextAnimation.TOnly.TASKBAR: return where == "taskbar";
                case TNextAnimation.TOnly.WINDOW: return where == "window";
                case TNextAnimation.TOnly.WINDOWSIDE: return where == "windowSide";
                case TNextAnimation.TOnly.HORIZONTAL: return where == "horizontal";
                case TNextAnimation.TOnly.VERTICAL: return where == "vertical";
                case TNextAnimation.TOnly.PETSIDE: return where == "petSide";
                default: return true;
            }
        }

        // ---------------------------------------------------------------- layout
        void Filter()
        {
            string q = search.Text.Trim();
            tiles = q.Length == 0 ? allTiles.ToList()
                : allTiles.Where(t => t.Name.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0 || (t.Original ?? "").IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0 || t.Id.ToString() == q).ToList();
            Layout_();
            Invalidate();
        }

        void Layout_()
        {
            int w = S(WIDTH);
            int tileW = (w - S(PAD) * 2 - S(GAP) * (COLS - 1)) / COLS;
            // two sections when the pet has entrances: "Entrances" (spawns) first, then "Animations"
            headers.Clear();
            bool sections = tiles.Any(t => t.Spawn >= 0) && tiles.Any(t => t.Spawn < 0);
            int y = 0, col = 0;
            bool? spawnPart = null;
            foreach (var t in tiles)
            {
                bool isSpawn = t.Spawn >= 0;
                if (spawnPart != isSpawn)
                {
                    if (spawnPart != null) { y += S(TILE_H) + S(GAP); col = 0; }
                    if (sections)
                    {
                        headers.Add((new Rectangle(0, y, w - S(PAD) * 2, S(SECTION_H)), Lang.T(isSpawn ? "picker.spawns" : "picker.anims")));
                        y += S(SECTION_H);
                    }
                    spawnPart = isSpawn;
                }
                else if (col == COLS) { col = 0; y += S(TILE_H) + S(GAP); }
                t.Rect = new Rectangle(col * (tileW + S(GAP)), y, tileW, S(TILE_H));
                col++;
            }

            // the window keeps the height of the full list so it does not jump while searching
            int rowsAll = Math.Max(1, Math.Min(MAX_ROWS, (allTiles.Count + COLS - 1) / COLS));
            contentHeight = tiles.Count == 0 ? 0 : y + S(TILE_H);
            int listH = rowsAll * (S(TILE_H) + S(GAP)) - S(GAP);

            closeRect = new Rectangle(w - S(PAD) - S(28), S(18), S(28), S(28));
            searchRect = new Rectangle(S(PAD), S(HEADER_H), w - S(PAD) * 2, S(SEARCH_H));
            listRect = new Rectangle(S(PAD), searchRect.Bottom + S(12), w - S(PAD) * 2, listH);
            ClientSize = new Size(w, listRect.Bottom + S(10) + S(FOOTER_H));

            int textH = search.PreferredHeight;
            search.SetBounds(searchRect.X + S(34), searchRect.Y + (searchRect.Height - textH) / 2, searchRect.Width - S(44), textH);
            scrollY = Math.Max(0, Math.Min(scrollY, MaxScroll));
        }

        int MaxScroll => Math.Max(0, contentHeight - listRect.Height);

        void ScrollBy(int delta)
        {
            int old = scrollY;
            scrollY = Math.Max(0, Math.Min(MaxScroll, scrollY + delta));
            if (old != scrollY) { UpdateHover(PointToClient(Cursor.Position)); Invalidate(); }
        }

        Rectangle ScreenRect(Tile t) => new Rectangle(listRect.X + t.Rect.X, listRect.Y + t.Rect.Y - scrollY, t.Rect.Width, t.Rect.Height);

        // ---------------------------------------------------------------- paint
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            Theme.Prepare(g);
            g.Clear(Theme.Background);

            // header: pet icon, title, counter, close button
            var iconBox = new Rectangle(S(PAD), S(14), S(40), S(40));
            Theme.FillRounded(g, Theme.AccentSoft, iconBox, S(10));
            if (petIcon != null)
            {
                var inner = Rectangle.Inflate(iconBox, -S(5), -S(5));
                Theme.DrawPixelImage(g, petIcon, inner);
            }
            else
            {
                Theme.DrawText(g, "🎬", fontEmoji, iconBox, Theme.Accent, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
            int tx = iconBox.Right + S(12);
            Theme.DrawText(g, Lang.T("picker.title", petName), fontTitle, new Rectangle(tx, S(13), closeRect.Left - tx - S(8), S(24)),
                Theme.Text, TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter);
            Theme.DrawText(g, Lang.T("picker.count", allTiles.Count), fontSub, new Rectangle(tx, S(37), closeRect.Left - tx - S(8), S(18)),
                Theme.TextMuted, TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter);

            if (hoverClose) Theme.FillRounded(g, Theme.Danger, closeRect, S(8));
            Theme.DrawText(g, "✕", fontTile, closeRect, hoverClose ? Color.White : Theme.TextMuted,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);

            // search field
            Theme.FillRounded(g, Theme.Surface, searchRect, S(8));
            Theme.DrawRounded(g, search.Focused ? Theme.Accent : Theme.Border, searchRect, S(8));
            Theme.DrawText(g, "🔍", fontSmall, new Rectangle(searchRect.X + S(10), searchRect.Y, S(20), searchRect.Height), Theme.TextMuted,
                TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);

            // tiles
            var oldClip = g.Clip;
            g.SetClip(Rectangle.Inflate(listRect, 2, 2));
            foreach (var (hr, text) in headers)
            {
                var hrect = new Rectangle(listRect.X + hr.X, listRect.Y + hr.Y - scrollY, hr.Width, hr.Height);
                if (hrect.Bottom < listRect.Top || hrect.Top > listRect.Bottom) continue;
                Theme.DrawText(g, text, fontSub, hrect, Theme.TextMuted, TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            }
            for (int i = 0; i < tiles.Count; i++)
            {
                var t = tiles[i];
                var r = ScreenRect(t);
                if (r.Bottom < listRect.Top || r.Top > listRect.Bottom) continue;
                bool hover = i == hoverTile, active = t == playingTile;

                Theme.FillRounded(g, active ? Theme.AccentSoft : hover ? Theme.Hover : Theme.Surface, r, S(10));
                Theme.DrawRounded(g, active || hover ? Theme.Accent : Theme.Border, r, S(10), active ? 1.6f : 1f);

                var em = new Rectangle(r.X + S(6), r.Y + S(6), r.Height - S(12), r.Height - S(12));
                Theme.FillRounded(g, active ? Theme.Accent : Theme.SurfaceAlt, em, S(8));
                if (t.Pic != null) Theme.DrawPixelImage(g, t.Pic, Rectangle.Inflate(em, -S(1), -S(1)));
                else Theme.DrawText(g, t.Emoji, fontEmoji, em, active ? Theme.AccentText : Theme.Text,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);

                var nameRect = new Rectangle(em.Right + S(8), r.Y, r.Right - em.Right - S(12), r.Height);
                Theme.DrawText(g, t.Name, fontTile, new Rectangle(nameRect.X, r.Y + S(5), nameRect.Width, S(20)),
                    Theme.Text, TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter);
                Theme.DrawText(g, active ? "▶ " + Lang.T("picker.playing") : "#" + t.Id, fontSmall,
                    new Rectangle(nameRect.X, r.Y + S(23), nameRect.Width, S(16)),
                    active ? Theme.Accent : Theme.TextMuted, TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter);
            }
            g.Clip = oldClip;

            if (tiles.Count == 0)
                Theme.DrawText(g, Lang.T("picker.empty"), fontSub, listRect, Theme.TextMuted,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);

            // thin scrollbar
            if (MaxScroll > 0)
            {
                int trackH = listRect.Height;
                int thumbH = Math.Max(S(24), trackH * listRect.Height / contentHeight);
                int thumbY = listRect.Y + (trackH - thumbH) * scrollY / MaxScroll;
                Theme.FillRounded(g, Theme.Border, new Rectangle(listRect.Right + S(5), thumbY, S(4), thumbH), S(2));
            }

            // fade at the bottom when more tiles are below
            if (scrollY < MaxScroll)
            {
                var fade = new Rectangle(listRect.X, listRect.Bottom - S(18), listRect.Width, S(18) + 1);
                using (var b = new LinearGradientBrush(fade, Color.FromArgb(0, Theme.Background), Theme.Background, LinearGradientMode.Vertical))
                    g.FillRectangle(b, fade);
            }

            // footer hint
            int fy = ClientSize.Height - S(FOOTER_H);
            using (var pen = new Pen(Theme.Border)) g.DrawLine(pen, 0, fy, ClientSize.Width, fy);
            Theme.DrawText(g, Lang.T("picker.hint"), fontSmall, new Rectangle(S(PAD), fy, ClientSize.Width - S(PAD) * 2, S(FOOTER_H)),
                Theme.TextMuted, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
        }

        // ---------------------------------------------------------------- interaction
        void UpdateHover(Point p)
        {
            int prev = hoverTile; bool prevC = hoverClose;
            hoverClose = closeRect.Contains(p);
            hoverTile = -1;
            if (listRect.Contains(p))
                for (int i = 0; i < tiles.Count; i++)
                    if (ScreenRect(tiles[i]).Contains(p)) { hoverTile = i; break; }
            Cursor = hoverTile >= 0 || hoverClose ? Cursors.Hand : Cursors.Default;
            if (prev != hoverTile || prevC != hoverClose) Invalidate();
            if (prev != hoverTile)
                tip.SetToolTip(this, hoverTile >= 0 ? tiles[hoverTile].Name + (tiles[hoverTile].Original != null && tiles[hoverTile].Original != tiles[hoverTile].Name ? "  (" + tiles[hoverTile].Original + ")" : "") + "  ·  #" + tiles[hoverTile].Id : null);
        }

        void OnMouseMove(object sender, MouseEventArgs e) => UpdateHover(e.Location);

        void OnMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            if (closeRect.Contains(e.Location)) { Close(); return; }
            if (hoverTile >= 0 && hoverTile < tiles.Count) Play(tiles[hoverTile]);
        }

        void Play(Tile t)
        {
            playingTile = t;
            playingId = t.Id;
            Invalidate();
            if (t.Spawn >= 0 && playSpawn != null) playSpawn(t.Spawn);
            else play(t.Id);
            // the animation may have opened a window of the pet (tub, balloon…) that took the focus: take it back
            var back = new Timer { Interval = 250 };
            back.Tick += (s, e) => { back.Stop(); back.Dispose(); if (!IsDisposed && Visible) Activate(); };
            back.Start();
        }

        /// <summary>Self-test: click the tile of an animation.</summary>
        internal void TestPlay(int id) { var t = allTiles.FirstOrDefault(x => x.Id == id && x.Spawn < 0) ?? allTiles.FirstOrDefault(x => x.Id == id); if (t != null) Play(t); }
        /// <summary>Self-test: how many entrances (spawns) are offered, and the animations offered.</summary>
        internal int TestSpawnCount => allTiles.Count(x => x.Spawn >= 0);
        internal List<int> TestAnimationIds => allTiles.Where(x => x.Spawn < 0).Select(x => x.Id).ToList();

        [System.Runtime.InteropServices.DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
        [System.Runtime.InteropServices.DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint pid);

        /// <summary>Close when the focus went to another program (not when it went to a window of this one).</summary>
        void CloseIfAnotherProgram()
        {
            if (IsDisposed) return;
            GetWindowThreadProcessId(GetForegroundWindow(), out uint pid);
            if (pid == (uint)Environment.ProcessId) return;
            Close();
        }

        // drag the window by its header
        protected override void WndProc(ref Message m)
        {
            const int WM_NCHITTEST = 0x84, HTCLIENT = 1, HTCAPTION = 2;
            base.WndProc(ref m);
            if (m.Msg == WM_NCHITTEST && (int)m.Result == HTCLIENT)
            {
                var pt = PointToClient(new Point(unchecked((short)(long)m.LParam), unchecked((short)((long)m.LParam >> 16))));
                if (pt.Y < S(HEADER_H) - S(8) && !closeRect.Contains(pt)) m.Result = (IntPtr)HTCAPTION;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                fontTitle?.Dispose(); fontSub?.Dispose(); fontTile?.Dispose();
                fontSmall?.Dispose(); fontEmoji?.Dispose(); fontSearch?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
