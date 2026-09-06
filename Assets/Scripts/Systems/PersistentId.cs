using System.Text;
using UnityEngine;

namespace Ashfall.Systems
{
    // Gives a placed object a stable identity across sessions so its state can be
    // written to the save file and found again.
    //
    // The id is the object's hierarchy path plus its sibling index, e.g.
    // "Enemys/Warrior#2". That means no component has to be added to hundreds of
    // objects and no scene has to be edited to make persistence work.
    //
    // The trade-off: renaming or reordering an object in the editor changes its id,
    // so an older save no longer matches it. That fails safe - an id that is not
    // found simply leaves the object exactly as the scene authored it.
    public static class PersistentId
    {
        public static string For(Component component)
        {
            return component == null ? string.Empty : For(component.transform);
        }

        public static string For(Transform transform)
        {
            if (transform == null) return string.Empty;

            var builder = new StringBuilder();
            Append(transform, builder);
            return builder.ToString();
        }

        // Walks up to the root first, then writes back down, so the path reads
        // top-down without having to reverse a string afterwards.
        static void Append(Transform transform, StringBuilder builder)
        {
            if (transform.parent != null)
            {
                Append(transform.parent, builder);
                builder.Append('/');
            }

            builder.Append(transform.name);

            // Sibling index disambiguates identically named siblings, which is
            // common for level scenery ("Steps", "Steps (1)" are fine, but two
            // objects genuinely named the same would otherwise collide).
            builder.Append('#');
            builder.Append(transform.GetSiblingIndex());
        }
    }
}
