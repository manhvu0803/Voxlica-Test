using System;
using System.Runtime.CompilerServices;
using Unity.IL2CPP.CompilerServices;
using static Fraktalia.Core.Mathematics.Fmath;

namespace Fraktalia.Core.Mathematics.Geometry
{
    /// <summary>
    /// Axis aligned bounding box (AABB) stored in min and max form.
    /// </summary>
    /// <remarks>
    /// Axis aligned bounding boxes (AABB) are boxes where each side is parallel with one of the Cartesian coordinate axes
    /// X, Y, and Z. AABBs are useful for approximating the region an object (or collection of objects) occupies and quickly
    /// testing whether or not that object (or collection of objects) is relevant. Because they are axis aligned, they
    /// are very cheap to construct and perform overlap tests with them.
    /// </remarks>
    [System.Serializable]
    [FIl2CppEagerStaticClassConstruction]
    internal struct FMinMaxAABB : IEquatable<FMinMaxAABB>
    {
        /// <summary>
        /// The minimum point contained by the AABB.
        /// </summary>
        /// <remarks>
        /// If any component of <see cref="Min"/> is greater than <see cref="Max"/> then this AABB is invalid.
        /// </remarks>
        /// <seealso cref="IsValid"/>
        public Ffloat3 Min;

        /// <summary>
        /// The maximum point contained by the AABB.
        /// </summary>
        /// <remarks>
        /// If any component of <see cref="Max"/> is less than <see cref="Min"/> then this AABB is invalid.
        /// </remarks>
        /// <seealso cref="IsValid"/>
        public Ffloat3 Max;

        /// <summary>
        /// Constructs the AABB with the given minimum and maximum.
        /// </summary>
        /// <remarks>
        /// If you have a center and extents, you can call <see cref="CreateFromCenterAndExtents"/> or <see cref="CreateFromCenterAndHalfExtents"/>
        /// to create the AABB.
        /// </remarks>
        /// <param name="min">Minimum point inside AABB.</param>
        /// <param name="max">Maximum point inside AABB.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FMinMaxAABB(Ffloat3 min, Ffloat3 max)
        {
            Min = min;
            Max = max;
        }

        /// <summary>
        /// Creates the AABB from a center and extents.
        /// </summary>
        /// <remarks>
        /// This function takes full extents. It is the distance between <see cref="Min"/> and <see cref="Max"/>.
        /// If you have half extents, you can call <see cref="CreateFromCenterAndHalfExtents"/>.
        /// </remarks>
        /// <param name="center">Center of AABB.</param>
        /// <param name="extents">Full extents of AABB.</param>
        /// <returns>AABB created from inputs.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FMinMaxAABB CreateFromCenterAndExtents(Ffloat3 center, Ffloat3 extents)
        {
            return CreateFromCenterAndHalfExtents(center, extents * 0.5f);
        }

        /// <summary>
        /// Creates the AABB from a center and half extents.
        /// </summary>
        /// <remarks>
        /// This function takes half extents. It is half the distance between <see cref="Min"/> and <see cref="Max"/>.
        /// If you have full extents, you can call <see cref="CreateFromCenterAndExtents"/>.
        /// </remarks>
        /// <param name="center">Center of AABB.</param>
        /// <param name="halfExtents">Half extents of AABB.</param>
        /// <returns>AABB created from inputs.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FMinMaxAABB CreateFromCenterAndHalfExtents(Ffloat3 center, Ffloat3 halfExtents)
        {
            return new FMinMaxAABB(center - halfExtents, center + halfExtents);
        }

        /// <summary>
        /// Computes the extents of the AABB.
        /// </summary>
        /// <remarks>
        /// Extents is the componentwise distance between min and max.
        /// </remarks>
        public Ffloat3 Extents => Max - Min;

        /// <summary>
        /// Computes the half extents of the AABB.
        /// </summary>
        /// <remarks>
        /// HalfExtents is half of the componentwise distance between min and max. Subtracting HalfExtents from Center
        /// gives Min and adding HalfExtents to Center gives Max.
        /// </remarks>
        public Ffloat3 HalfExtents => (Max - Min) * 0.5f;

        /// <summary>
        /// Computes the center of the AABB.
        /// </summary>
        public Ffloat3 Center => (Max + Min) * 0.5f;

        /// <summary>
        /// Check if the AABB is valid.
        /// </summary>
        /// <remarks>
        /// An AABB is considered valid if <see cref="Min"/> is componentwise less than or equal to <see cref="Max"/>.
        /// </remarks>
        /// <returns>True if <see cref="Min"/> is componentwise less than or equal to <see cref="Max"/>.</returns>
        public bool IsValid => Fmath.all(Min <= Max);

        /// <summary>
        /// Computes the surface area for this axis aligned bounding box.
        /// </summary>
        public float SurfaceArea
        {
            get
            {
                Ffloat3 diff = Max - Min;
                return 2 * Fmath.dot(diff, diff.yzx);
            }
        }

        /// <summary>
        /// Tests if the input point is contained by the AABB.
        /// </summary>
        /// <param name="point">Point to test.</param>
        /// <returns>True if AABB contains the input point.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(Ffloat3 point) => Fmath.all(point >= Min & point <= Max);

        /// <summary>
        /// Tests if the input AABB is contained entirely by this AABB.
        /// </summary>
        /// <param name="aabb">AABB to test.</param>
        /// <returns>True if input AABB is contained entirely by this AABB.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(FMinMaxAABB aabb) => Fmath.all((Min <= aabb.Min) & (Max >= aabb.Max));

        /// <summary>
        /// Tests if the input AABB overlaps this AABB.
        /// </summary>
        /// <param name="aabb">AABB to test.</param>
        /// <returns>True if input AABB overlaps with this AABB.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Overlaps(FMinMaxAABB aabb)
        {
            return Fmath.all(Max >= aabb.Min & Min <= aabb.Max);
        }

        /// <summary>
        /// Expands the AABB by the given signed distance.
        /// </summary>
        /// <remarks>
        /// Positive distance expands the AABB while negative distance shrinks the AABB.
        /// </remarks>
        /// <param name="signedDistance">Signed distance to expand the AABB with.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Expand(float signedDistance)
        {
            Min -= signedDistance;
            Max += signedDistance;
        }

        /// <summary>
        /// Encapsulates the given AABB.
        /// </summary>
        /// <remarks>
        /// Modifies this AABB so that it contains the given AABB. If the given AABB is already contained by this AABB,
        /// then this AABB doesn't change.
        /// </remarks>
        /// <seealso cref="Contains(Fraktalia.Core.Mathematics.Geometry.FMinMaxAABB)"/>
        /// <param name="aabb">AABB to encapsulate.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Encapsulate(FMinMaxAABB aabb)
        {
            Min = Fmath.min(Min, aabb.Min);
            Max = Fmath.max(Max, aabb.Max);
        }

        /// <summary>
        /// Encapsulate the given point.
        /// </summary>
        /// <remarks>
        /// Modifies this AABB so that it contains the given point. If the given point is already contained by this AABB,
        /// then this AABB doesn't change.
        /// </remarks>
        /// <seealso cref="Contains(Fraktalia.Core.Mathematics.Ffloat3)"/>
        /// <param name="point">Point to encapsulate.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Encapsulate(Ffloat3 point)
        {
            Min = Fmath.min(Min, point);
            Max = Fmath.max(Max, point);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(FMinMaxAABB other)
        {
            return Min.Equals(other.Min) && Max.Equals(other.Max);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString()
        {
            return string.Format("MinMaxAABB({0}, {1})", Min, Max);
        }
    }

    internal static partial class FMath
    {
        /// <summary>
        /// Transforms the AABB with the given transform.
        /// </summary>
        /// <remarks>
        /// The resulting AABB encapsulates the transformed AABB which may not be axis aligned after the transformation.
        /// </remarks>
        /// <param name="transform">Transform to apply to AABB.</param>
        /// <param name="aabb">AABB to be transformed.</param>
        /// <returns>Transformed AABB.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FMinMaxAABB Transform(FRigidTransform transform, FMinMaxAABB aabb)
        {
            Ffloat3 halfExtentsInA = aabb.HalfExtents;

            // Rotate each axis individually and find their new positions in the rotated space.
            Ffloat3 x = Fmath.rotate(transform.rot, new Ffloat3(halfExtentsInA.x, 0, 0));
            Ffloat3 y = Fmath.rotate(transform.rot, new Ffloat3(0, halfExtentsInA.y, 0));
            Ffloat3 z = Fmath.rotate(transform.rot, new Ffloat3(0, 0, halfExtentsInA.z));

            // Find the new max corner by summing the rotated axes.  Absolute value of each axis
            // since we are trying to find the max corner.
            Ffloat3 halfExtentsInB = Fmath.abs(x) + Fmath.abs(y) + Fmath.abs(z);
            Ffloat3 centerInB = Fmath.transform(transform, aabb.Center);

            return new FMinMaxAABB(centerInB - halfExtentsInB, centerInB + halfExtentsInB);
        }

        /// <summary>
        /// Transforms the AABB with the given transform.
        /// </summary>
        /// <remarks>
        /// The resulting AABB encapsulates the transformed AABB which may not be axis aligned after the transformation.
        /// </remarks>
        /// <param name="transform">Transform to apply to AABB.</param>
        /// <param name="aabb">AABB to be transformed.</param>
        /// <returns>Transformed AABB.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FMinMaxAABB Transform(Ffloat4x4 transform, FMinMaxAABB aabb)
        {
            var transformed = Transform(new Ffloat3x3(transform), aabb);
            transformed.Min += transform.c3.xyz;
            transformed.Max += transform.c3.xyz;
            return transformed;
        }

        /// <summary>
        /// Transforms the AABB with the given transform.
        /// </summary>
        /// <remarks>
        /// The resulting AABB encapsulates the transformed AABB which may not be axis aligned after the transformation.
        /// </remarks>
        /// <param name="transform">Transform to apply to AABB.</param>
        /// <param name="aabb">AABB to be transformed.</param>
        /// <returns>Transformed AABB.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FMinMaxAABB Transform(Ffloat3x3 transform, FMinMaxAABB aabb)
        {
            // From Christer Ericson's Real-Time Collision Detection on page 86 and 87.
            // We want the transformed minimum and maximums of the AABB. Multiplying a 3x3 matrix on the left of a
            // column vector looks like so:
            //
            // [ c0.x c1.x c2.x ] [ x ]   [ c0.x * x + c1.x * y + c2.x * z ]
            // [ c0.y c1.y c2.y ] [ y ] = [ c0.y * x + c1.y * y + c2.y * z ]
            // [ c0.z c1.z c2.z ] [ z ]   [ c0.z * x + c1.z * y + c2.z * z ]
            //
            // The column vectors we will use are the input AABB's min and max. Simply multiplying those two vectors
            // with the transformation matrix won't guarantee we get the new min and max since those are only two
            // points out of eight in the AABB and one of the other six may set the new min or max.
            //
            // To ensure we get the correct min and max, we must transform all eight points. But it's not necessary
            // to actually perform eight matrix multiplies to get our final result. Instead, we can build the min and
            // max incrementally by computing each term in the above matrix multiply separately then summing the min
            // (or max). For instance, to find the new minimum contributed by the original min and max x component, we
            // compute this:
            //
            // newMin.x = min(c0.x * Min.x, c0.x * Max.x);
            // newMin.y = min(c0.y * Min.x, c0.y * Max.x);
            // newMin.z = min(c0.z * Min.x, c0.z * Max.x);
            //
            // Then we add minimum contributed by the original min and max y components:
            //
            // newMin.x += min(c1.x * Min.y, c1.x * Max.y);
            // newMin.y += min(c1.y * Min.y, c1.y * Max.y);
            // newMin.z += min(c1.z * Min.y, c1.z * Max.y);
            //
            // And so on. Translation can be handled by simply initializing the new min and max with the translation
            // amount since it does not affect the min and max bounds in local space.
            var t1 = transform.c0.xyz * aabb.Min.xxx;
            var t2 = transform.c0.xyz * aabb.Max.xxx;
            var minMask = t1 < t2;
            var transformed = new FMinMaxAABB(select(t2, t1, minMask), select(t2, t1, !minMask));
            t1 = transform.c1.xyz * aabb.Min.yyy;
            t2 = transform.c1.xyz * aabb.Max.yyy;
            minMask = t1 < t2;
            transformed.Min += select(t2, t1, minMask);
            transformed.Max += select(t2, t1, !minMask);
            t1 = transform.c2.xyz * aabb.Min.zzz;
            t2 = transform.c2.xyz * aabb.Max.zzz;
            minMask = t1 < t2;
            transformed.Min += select(t2, t1, minMask);
            transformed.Max += select(t2, t1, !minMask);
            return transformed;
        }
    }
}
