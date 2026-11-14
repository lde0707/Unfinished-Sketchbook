using UnityEngine;
namespace Unfin
{
    // Shared ink colours for the minimap and journal. Separate from material tints:
    // small map silhouettes need more hue separation than the world artwork.
    public static class MapZonePalette
    {
        static readonly Color[] Inks={
            new Color(.73f,.64f,.46f), // A: parchment
            new Color(.43f,.65f,.77f), // B: slate blue
            new Color(.54f,.70f,.39f), // C: moss
            new Color(.66f,.47f,.74f), // D: muted violet
            new Color(.83f,.47f,.29f), // E: sunset clay
            new Color(.84f,.72f,.30f), // F: wax gold
            new Color(.40f,.49f,.85f), // G: indigo
            new Color(.78f,.39f,.51f)  // H: wine rose
        };
        public static Color Ink(int zone)=>Inks[Mathf.Clamp(zone,0,7)];
        public static Color Fill(int zone){var c=Ink(zone);c.a=.90f;return c;}
        public static Color Outline(int zone){var c=Color.Lerp(Ink(zone),new Color(.94f,.89f,.77f),.37f);c.a=.96f;return c;}
        public static Color Rock(int zone){var c=Ink(zone)*.16f;c.a=.45f;return c;}
    }
}
