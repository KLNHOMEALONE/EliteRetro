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
    /// Apply body-axis rotation to the camera orientation.
    /// Roll (alpha) rotates around the nose axis (forward), affecting roofv and sidev.
    /// Pitch (beta) rotates around the side axis (right), affecting nosev and roofv.
    ///
    /// Uses the Minsky circle algorithm (reusing updated intermediate values)
    /// applied to whole vectors rather than per-component, because in this
    /// codebase entity positions are NOT rotated — only the camera orientation
    /// changes. This gives correct body-axis flight behavior where pitch always
    /// moves the view up/down relative to the ship's current orientation.
    ///
    /// Note: The original BBC Elite applies per-component Minsky rotation to
    /// both positions AND orientations (the MVS4 routine). That approach is
    /// equivalent when both are rotated together. Here we only rotate the
    /// camera, so we must use body-axis (whole-vector) rotation instead.
    /// </summary>
    public void ApplyUniverseRotation(float alpha, float beta)
    {
        // Roll: rotate roofv and sidev around nosev (forward axis)
        // Positive alpha = roll right (clockwise from pilot's view)
        Roofv += alpha * Sidev;
        Sidev -= alpha * Roofv;   // Minsky: uses updated Roofv

        // Pitch: rotate nosev and roofv around sidev (right axis)
        // Positive beta = nose up (objects move down on screen)
        Nosev += beta * Roofv;
        Roofv -= beta * Nosev;    // Minsky: uses updated Nosev
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
    /// Convert to a 4x4 rotation matrix (rotation only, no translation).
    /// The basis vectors form the upper-left 3x3 block.
    /// Basis vectors are placed in ROWS for a Model-to-World transformation (V * M).
    /// Row 0 = Sidev (Right), Row 1 = Roofv (Up), Row 2 = Nosev (Forward).
    /// </summary>
    public Matrix ToMatrix4x4()
    {
        return new Matrix(
            Sidev.X, Sidev.Y, Sidev.Z, 0,
            Roofv.X, Roofv.Y, Roofv.Z, 0,
            Nosev.X, Nosev.Y, Nosev.Z, 0,
            0, 0, 0, 1);
    }

    /// <summary>
    /// Rotate a position vector by this orientation matrix.
    /// Maps position (X, Y, Z) to (Sidev, Roofv, Nosev) components.
    /// </summary>
    public Vector3 Transform(Vector3 position)
    {
        return new Vector3(
            Vector3.Dot(position, Sidev),
            Vector3.Dot(position, Roofv),
            Vector3.Dot(position, Nosev)
        );
    }

    /// <summary>
    /// Rotate a position vector by the inverse (transpose) of this orientation matrix.
    /// Reconstructs world position from local (Side, Roof, Nose) components.
    /// </summary>
    public Vector3 InverseTransform(Vector3 position)
    {
        return position.X * Sidev + position.Y * Roofv + position.Z * Nosev;
    }

    /// <summary>
    /// Apply per-component Minsky rotation to all orientation vectors.
    /// This is the authentic Elite MVS4 algorithm where each vector's
    /// (x, y, z) components are rotated through the same (alpha, beta) angles.
    /// Used for rotating entity orientations when entity positions are also
    /// being rotated by the same per-component Minsky transform.
    ///
    /// Combined roll-then-pitch per component:
    ///   K2 = v.y - alpha * v.x
    ///   v.z = v.z + beta * K2
    ///   v.y = K2 - beta * v.z       (uses updated z)
    ///   v.x = v.x + alpha * v.y     (uses updated y)
    ///
    /// See: https://elite.bbcelite.com/deep_dives/pitching_and_rolling.html
    /// </summary>
    public void ApplyMinskyRotation(float alpha, float beta)
    {
        RotateVector(ref Nosev, alpha, beta);
        RotateVector(ref Roofv, alpha, beta);
        RotateVector(ref Sidev, alpha, beta);
    }

    private static void RotateVector(ref Vector3 v, float alpha, float beta)
    {
        float k2 = v.Y - alpha * v.X;
        float z = v.Z + beta * k2;
        float y = k2 - beta * z;
        float x = v.X + alpha * y;
        v.X = x;
        v.Y = y;
        v.Z = z;
    }

    /// <summary>
    /// Apply a fixed-angle rotation to the orientation (for AI ship turning).
    /// Uses the authentic Elite angle of 1/16 rad (~3.6 degrees).
    /// Rotates the nose vector toward the target direction using pitch and roll.
    /// This is a body-axis rotation (ship rotates relative to itself).
    /// </summary>
    /// <param name="pitchAmount">Pitch increment (-1 to 1, scaled by AiRotationAngle).</param>
    /// <param name="rollAmount">Roll increment (-1 to 1, scaled by AiRotationAngle).</param>
    public void ApplyOwnRotation(float pitchAmount, float rollAmount)
    {
        float alpha = rollAmount * GameConstants.AiRotationAngle;
        float beta = pitchAmount * GameConstants.AiRotationAngle;
        ApplyUniverseRotation(alpha, beta);
    }
}
