namespace EliteRetro.Core;

/// <summary>
/// 3×3 orthonormal rotation matrix using Elite's nosev/roofv/sidev basis vectors.
/// Supports Minsky circle algorithm rotation and periodic TIDY orthonormalization.
/// </summary>
public struct OrientationMatrix
{
    /// <summary>Nose vector (forward direction, row 0).</summary>
    public Vector3 Nosev;
    /// <summary>Roof vector (up direction, row 1).</summary>
    public Vector3 Roofv;
    /// <summary>Side vector (right direction, row 2).</summary>
    public Vector3 Sidev;

    /// <summary>Identity orientation: nose=-Z, roof=+Y, side=+X.</summary>
    public static OrientationMatrix Identity => new()
    {
        Nosev = new Vector3(0, 0, -1),
        Roofv = new Vector3(0, 1, 0),
        Sidev = new Vector3(1, 0, 0)
    };

    /// <summary>
    /// Apply Minsky circle algorithm rotation.
    /// Rotates all three basis vectors using small-angle approximation.
    /// alpha = roll, beta = pitch.
    /// </summary>
    public void ApplyUniverseRotation(float alpha, float beta)
    {
        RotateVector(ref Nosev, alpha, beta);
        RotateVector(ref Roofv, alpha, beta);
        RotateVector(ref Sidev, alpha, beta);
    }

    private static void RotateVector(ref Vector3 v, float alpha, float beta)
    {
        // Minsky circle algorithm (Elite's small-angle rotation):
        // K2 = y - α·x;  z = z + β·K2;  y = K2 - β·z;  x = x + α·y
        float k2 = v.Y - alpha * v.X;
        float z = v.Z + beta * k2;
        float y = k2 - beta * z;
        float x = v.X + alpha * y;
        v.X = x;
        v.Y = y;
        v.Z = z;
    }

    /// <summary>
    /// TIDY routine: re-orthonormalize the basis vectors.
    /// Normalizes nosev, orthogonalizes roofv against nosev,
    /// recomputes sidev via cross product. Call periodically to
    /// correct floating-point drift from Minsky rotation.
    /// </summary>
    public void Tidy()
    {
        // Normalize nosev
        Nosev = Vector3.Normalize(Nosev);

        // Orthogonalize roofv against nosev
        float dot = Vector3.Dot(Roofv, Nosev);
        Roofv -= dot * Nosev;
        Roofv = Vector3.Normalize(Roofv);

        // Recompute sidev as cross(nosev, roofv)
        Sidev = Vector3.Cross(Nosev, Roofv);
    }

    /// <summary>
    /// Rotate a position vector by this orientation matrix.
    /// </summary>
    public Vector3 Transform(Vector3 position)
    {
        return new Vector3(
            Vector3.Dot(position, Nosev),
            Vector3.Dot(position, Roofv),
            Vector3.Dot(position, Sidev)
        );
    }

    /// <summary>
    /// Rotate a position vector by the inverse (transpose) of this orientation matrix.
    /// </summary>
    public Vector3 InverseTransform(Vector3 position)
    {
        return position.X * Nosev + position.Y * Roofv + position.Z * Sidev;
    }
}
