//! DeskPals: Snowhush running as an extension of DeskPals (a desktop pets program) instead of on its own.
//! Started with `--hosted [--parent <pid>]`: no tray icon, no welcome window, no update checks; its settings live
//! under `HKCU\Software\DeskPals\Snowhush`, written by DeskPals, which posts the registered message
//! `DeskPalsSnowhushReload` to the controller window when they change (and `DeskPalsSnowhushClear` to clear the snow).
//! It quits with DeskPals. Everything else (the snow, its physics and drawing) is Snowhush's own.

use std::sync::atomic::{AtomicBool, AtomicU32, Ordering};

static HOSTED: AtomicBool = AtomicBool::new(false);
static PARENT: AtomicU32 = AtomicU32::new(0);

pub fn is() -> bool {
    HOSTED.load(Ordering::Relaxed)
}

/// Reads `--hosted` and `--parent <pid>` from the command line.
pub fn read_arguments() {
    let args: Vec<String> = std::env::args().skip(1).collect();
    if args.iter().any(|a| a == "--hosted") {
        HOSTED.store(true, Ordering::Relaxed);
    }
    if let Some(i) = args.iter().position(|a| a == "--parent")
        && let Some(pid) = args.get(i + 1).and_then(|p| p.parse::<u32>().ok())
    {
        PARENT.store(pid, Ordering::Relaxed);
    }
}

/// The process of DeskPals, 0 when not given.
pub fn parent() -> u32 {
    PARENT.load(Ordering::Relaxed)
}

/// DeskPals: how big the flakes are, in hundredths (100 = Snowhush's own; DeskPals Weather offers 100, 160, 230).
static SCALE: AtomicU32 = AtomicU32::new(100);

/// Makes what falls bigger (or keeps it), easier to see over a busy screen; the ghosts of Halloween keep their size.
pub fn scale_sprites(sprites: &mut [snowhush_core::simulation::Sprite]) {
    let scale = SCALE.load(Ordering::Relaxed);
    if scale == 100 {
        return;
    }
    let k = scale as f32 / 100.0;
    for sprite in sprites.iter_mut().filter(|s| s.ghost[0] <= 0.0) {
        sprite.size *= k;
    }
}

/// DeskPals: a colour for what falls (DeskPals Weather's "colour per effect"), 0 = Snowhush's own; else 0x01RRGGBB.
static TINT: AtomicU32 = AtomicU32::new(0);

/// Reads the colour again (at the start and with every `DeskPalsSnowhushReload`).
pub fn load_tint() {
    if is() {
        TINT.store(crate::prefs::tint(), Ordering::Relaxed);
        SCALE.store(crate::prefs::flake_scale(), Ordering::Relaxed);
    }
}

/// The light on what falls, times the colour chosen: white snow turns that colour, leaves and petals take its hue.
pub fn tinted(light: [f32; 3]) -> [f32; 3] {
    let tint = TINT.load(Ordering::Relaxed);
    if tint & 0x0100_0000 == 0 {
        return light;
    }
    let channel = |shift: u32| ((tint >> shift) & 0xff) as f32 / 255.0;
    let color = [channel(16), channel(8), channel(0)];
    std::array::from_fn(|i| light[i] * color[i])
}
