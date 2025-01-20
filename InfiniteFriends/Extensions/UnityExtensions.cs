using UnityEngine;

namespace InfiniteFriends.Extensions;

internal static class UnityExtensions
{
    public static bool Contains(this LayerMask mask, int layer) => mask == (mask | (1 << layer));
}
