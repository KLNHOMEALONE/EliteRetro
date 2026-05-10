using EliteRetro.Core;
using EliteRetro.Core.Entities;
using Microsoft.Xna.Framework;
using Xunit;

namespace EliteRetro.Tests;

public class OrientationMatrixTests
{
    [Fact]
    public void Identity_ShouldMatchClassicElite()
    {
        var m = OrientationMatrix.Identity;
        
        // Classic Elite cockpit orientation: 
        // Nosev faces -Z (forward), Roofv is +Y (up), Sidev is +X (right)
        Assert.Equal(new Vector3(0, 0, -1), m.Nosev);
        Assert.Equal(new Vector3(0, 1, 0), m.Roofv);
        Assert.Equal(new Vector3(1, 0, 0), m.Sidev);
    }

    [Fact]
    public void ApplyUniverseRotation_Roll_ShouldPreserveOrthonormality()
    {
        var m = OrientationMatrix.Identity;
        float alpha = 0.1f; // roll right
        
        m.ApplyUniverseRotation(alpha, 0);
        
        // Check dot products (should be zero/perpendicular)
        Assert.InRange(Vector3.Dot(m.Nosev, m.Roofv), -0.01f, 0.01f);
        Assert.InRange(Vector3.Dot(m.Nosev, m.Sidev), -0.01f, 0.01f);
        Assert.InRange(Vector3.Dot(m.Roofv, m.Sidev), -0.01f, 0.01f);
    }

    [Fact]
    public void Tidy_ShouldCorrectDrift()
    {
        var m = new OrientationMatrix
        {
            Nosev = new Vector3(0, 0, -1.1f), // scaled slightly off
            Roofv = new Vector3(0.1f, 1.0f, 0.1f), // not quite perpendicular
            Sidev = new Vector3(1.0f, 0, 0)
        };
        
        m.Tidy();
        
        // Should be normalized
        Assert.Equal(1.0f, m.Nosev.Length(), 5);
        Assert.Equal(1.0f, m.Roofv.Length(), 5);
        Assert.Equal(1.0f, m.Sidev.Length(), 5);
        
        // Should be orthogonal
        Assert.Equal(0, Vector3.Dot(m.Nosev, m.Roofv), 5);
        Assert.Equal(0, Vector3.Dot(m.Nosev, m.Sidev), 5);
        Assert.Equal(0, Vector3.Dot(m.Roofv, m.Sidev), 5);
    }

    [Fact]
    public void ApplyUniverseRotation_Stability_1000Iterations()
    {
        var m = OrientationMatrix.Identity;
        float alpha = 0.01f;
        float beta = 0.005f;

        // Run 1000 iterations of rotation
        for (int i = 0; i < 1000; i++)
        {
            m.ApplyUniverseRotation(alpha, beta);
        }

        // Check if basis vectors are still reasonably normalized
        // Minsky algorithm is area-preserving but not perfectly distance-preserving in floating point
        Assert.InRange(m.Nosev.Length(), 0.9f, 1.1f);
        Assert.InRange(m.Roofv.Length(), 0.9f, 1.1f);
        Assert.InRange(m.Sidev.Length(), 0.9f, 1.1f);

        // Check if they are still reasonably orthogonal
        Assert.InRange(Vector3.Dot(m.Nosev, m.Roofv), -0.1f, 0.1f);
        Assert.InRange(Vector3.Dot(m.Nosev, m.Sidev), -0.1f, 0.1f);
        Assert.InRange(Vector3.Dot(m.Roofv, m.Sidev), -0.1f, 0.1f);

        // Applying Tidy should restore perfect orthonormality
        m.Tidy();
        Assert.Equal(1.0f, m.Nosev.Length(), 5);
        Assert.Equal(1.0f, m.Roofv.Length(), 5);
        Assert.Equal(1.0f, m.Sidev.Length(), 5);
        Assert.Equal(0, Vector3.Dot(m.Nosev, m.Roofv), 5);
        Assert.Equal(0, Vector3.Dot(m.Nosev, m.Sidev), 5);
        Assert.Equal(0, Vector3.Dot(m.Roofv, m.Sidev), 5);
    }

    [Fact]
    public void ApplyUniverseRotation_Roll_Behavior()
    {
        var m = OrientationMatrix.Identity;
        // Identity: Nose=(0,0,-1), Roof=(0,1,0), Side=(1,0,0)
        
        // Roll right (clockwise) by 90 degrees (pi/2)
        // In Minsky with small steps:
        float step = 0.01f;
        int steps = (int)(MathHelper.PiOver2 / step);
        for (int i = 0; i < steps; i++) m.ApplyUniverseRotation(step, 0);
        m.Tidy();

        // After 90 deg roll right:
        // Roof should be where Side was (+X)
        // Side should be where -Roof was (-Y)
        Assert.InRange(m.Roofv.X, 0.99f, 1.01f);
        Assert.InRange(m.Sidev.Y, -1.01f, -0.99f);
        Assert.Equal(new Vector3(0, 0, -1), m.Nosev); // Nose should be unchanged
    }

    [Fact]
    public void ApplyUniverseRotation_Pitch_Behavior()
    {
        var m = OrientationMatrix.Identity;
        // Identity: Nose=(0,0,-1), Roof=(0,1,0), Side=(1,0,0)

        // Pitch up (nose up) by 90 degrees (pi/2)
        float step = 0.01f;
        int steps = (int)(MathHelper.PiOver2 / step);
        for (int i = 0; i < steps; i++) m.ApplyUniverseRotation(0, step);
        m.Tidy();

        // After 90 deg pitch up:
        // Nose should be where Roof was (+Y)
        // Roof should be where -Nose was (+Z)
        Assert.InRange(m.Nosev.Y, 0.99f, 1.01f);
        Assert.InRange(m.Roofv.Z, 0.99f, 1.01f);
        
        // Side should be unchanged (with tolerance)
        Assert.Equal(1.0f, m.Sidev.X, 5);
        Assert.Equal(0.0f, m.Sidev.Y, 5);
        Assert.Equal(0.0f, m.Sidev.Z, 5);
    }

    [Fact]
    public void ApplyMinskyRotation_Stability()
    {
        var m = OrientationMatrix.Identity;
        float alpha = 0.01f;
        float beta = 0.005f;

        // Per-component Minsky (authentic Elite MVS4)
        for (int i = 0; i < 1000; i++)
        {
            m.ApplyMinskyRotation(alpha, beta);
        }

        // Check stability
        Assert.InRange(m.Nosev.Length(), 0.9f, 1.1f);
        Assert.InRange(m.Roofv.Length(), 0.9f, 1.1f);
        Assert.InRange(m.Sidev.Length(), 0.9f, 1.1f);

        m.Tidy();
        Assert.Equal(1.0f, m.Nosev.Length(), 5);
        Assert.Equal(0, Vector3.Dot(m.Nosev, m.Roofv), 5);
    }

    [Fact]
    public void Transform_InverseTransform_ShouldBeReversible()
    {
        var m = OrientationMatrix.Identity;
        m.ApplyUniverseRotation(0.5f, 0.3f);
        m.Tidy();

        Vector3 original = new Vector3(10, 20, 30);
        Vector3 transformed = m.Transform(original);
        Vector3 reversed = m.InverseTransform(transformed);

        Assert.Equal(original.X, reversed.X, 4);
        Assert.Equal(original.Y, reversed.Y, 4);
        Assert.Equal(original.Z, reversed.Z, 4);
    }
}
