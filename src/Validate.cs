using System;
using OsmSharp.API;
using OsmSharp.Tags;

namespace OsmSharp.IO.API
{
	internal static class Validate
	{
		internal static void BoundLimits(Bounds bounds)
		{
			if (bounds.MinLongitude + bounds.MinLatitude + bounds.MaxLongitude + bounds.MaxLatitude == null)
			{
				throw new Exception("No Bound may be null.");
			}

			if (bounds.MinLongitude < -180 || bounds.MinLongitude > 180
				|| bounds.MinLatitude < -180 || bounds.MinLatitude > 180
				|| bounds.MaxLongitude < -180 || bounds.MaxLongitude > 180
				|| bounds.MaxLatitude < -180 || bounds.MaxLatitude > 180
				|| bounds.MinLatitude >= bounds.MaxLatitude)
			{
				throw new Exception("Those Bounds are not valid.");
			}
		}

		internal static void ContainsTags(TagsCollectionBase tags, params string[] keys)
		{
			foreach (var key in keys)
			{
				if (!tags.TryGetValue(key, out string value)
					|| string.IsNullOrEmpty(value))
				{
					throw new Exception($"TagCollection is missing the required key: {key}");
				}
			}
		}

		internal static void ElementHasAVersion(OsmGeo osmGeo)
		{
			if (osmGeo.Version == null) throw new Exception("Element must have a version");
		}
	}
}
