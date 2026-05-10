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
}
