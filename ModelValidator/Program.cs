using Microsoft.Xna.Framework;
using EliteRetro.Core.Entities;
using XnaVector3 = Microsoft.Xna.Framework.Vector3;
using NumVector3 = System.Numerics.Vector3;

namespace ModelValidator;

/// <summary>
/// Standalone validator for ship model face normals and windings.
/// </summary>
class Program
{
    static void Main(string[] args)
    {
        var results = new List<ModelResult>();

        // Check all requested models
        results.Add(CheckModel("Viper", ViperModel.Create()));
        results.Add(CheckModel("Asp Mk II", AspMk2Model.Create()));
        results.Add(CheckModel("Cobra Mk I", CobraMk1Model.Create()));
        results.Add(CheckModel("Cobra Mk III", CobraMk3Model.Create()));
        results.Add(CheckModel("Sidewinder", SidewinderModel.Create()));
        results.Add(CheckModel("Python", PythonModel.Create()));
        results.Add(CheckModel("Fer-de-Lance", FerDeLanceModel.Create()));
        results.Add(CheckModel("Mamba", MambaModel.Create()));
        results.Add(CheckModel("Adder", AdderModel.Create()));
        results.Add(CheckModel("Boa", BoaModel.Create()));
        results.Add(CheckModel("Constrictor", ConstrictorModel.Create()));
        results.Add(CheckModel("Gecko", GeckoModel.Create()));
        results.Add(CheckModel("Krait", KraitModel.Create()));
        results.Add(CheckModel("Shuttle", ShuttleModel.Create()));
        results.Add(CheckModel("Transporter", TransporterModel.Create()));
        results.Add(CheckModel("Worm", WormModel.Create()));
        results.Add(CheckModel("Escape Pod", EscapePodModel.Create()));
        results.Add(CheckModel("Missile", MissileModel.Create()));
        results.Add(CheckModel("Thargon", ThargonModel.Create()));
        results.Add(CheckModel("Thargoid", ThargoidModel.Create()));
        results.Add(CheckModel("Cougar", CougarModel.Create()));
        results.Add(CheckModel("Coriolis Station", CoriolisStationModel.Create()));
        results.Add(CheckModel("Dodo Station", DodoStationModel.Create()));
        results.Add(CheckModel("Asteroid", AsteroidModel.Create()));
        results.Add(CheckModel("Cargo Canister", CanisterModel.Create()));
        results.Add(CheckModel("Rock Hermit", RockHermitModel.Create()));
        results.Add(CheckModel("Splinter", SplinterModel.Create()));

        // Print report
        Console.WriteLine("=== SHIP MODEL FACE NORMAL & WINDING VALIDATION REPORT ===\n");

        foreach (var r in results)
        {
            Console.WriteLine($"Model: {r.Name}");
            Console.WriteLine($"  Vertices: {r.VertexCount}, Edges: {r.EdgeCount}, Faces: {r.FaceCount}");

            if (r.InwardNormals.Count > 0)
            {
                Console.WriteLine($"  INWARD-POINTING NORMALS (need flipping): {string.Join(", ", r.InwardNormals)}");
            }

            if (r.IncorrectWindings.Count > 0)
            {
                Console.WriteLine($"  INCORRECT WINDINGS: {string.Join(", ", r.IncorrectWindings)}");
            }

            if (r.MissingEdges.Count > 0)
            {
                Console.WriteLine($"  MISSING EDGES FROM FACE BOUNDARIES: {string.Join(", ", r.MissingEdges)}");
            }

            if (r.NonPlanarQuads.Count > 0)
            {
                Console.WriteLine($"  NON-PLANAR QUADS (should split): {string.Join(", ", r.NonPlanarQuads)}");
            }

            if (r.InwardNormals.Count == 0 && r.IncorrectWindings.Count == 0 &&
                r.MissingEdges.Count == 0 && r.NonPlanarQuads.Count == 0)
            {
                Console.WriteLine("  ALL CHECKS PASSED");
            }

            Console.WriteLine();
        }

        // Summary
        var totalIssues = results.Count(r => r.InwardNormals.Count > 0 || r.IncorrectWindings.Count > 0 ||
                                              r.MissingEdges.Count > 0 || r.NonPlanarQuads.Count > 0);
        Console.WriteLine($"=== SUMMARY: {totalIssues} model(s) with issues out of {results.Count} ===");
    }

    static ModelResult CheckModel(string name, ShipModel model)
    {
        var result = new ModelResult
        {
            Name = name,
            VertexCount = model.Vertices.Count,
            EdgeCount = model.Edges.Count,
            FaceCount = model.Faces.Count
        };

        var verts = model.Vertices;
        var edges = model.Edges;
        var faces = model.Faces;

        // Build edge set for quick lookup (normalized: smaller index first)
        var edgeSet = new HashSet<(int, int)>();
        foreach (var e in edges)
        {
            var a = Math.Min(e.Start, e.End);
            var b = Math.Max(e.Start, e.End);
            edgeSet.Add((a, b));
        }

        for (int f = 0; f < faces.Count; f++)
        {
            var face = faces[f];
            var indices = face.VertexIndices;

            if (indices.Length < 3) continue;

            // Get first 3 vertices for normal computation
            var v0 = ToNum(verts[indices[0]]);
            var v1 = ToNum(verts[indices[1]]);
            var v2 = ToNum(verts[indices[2]]);

            // Compute normal: cross(v1-v0, v2-v0)
            var edge1 = v1 - v0;
            var edge2 = v2 - v0;
            var normal = NumVector3.Cross(edge1, edge2);
            normal = NumVector3.Normalize(normal);

            // Compute face centroid
            var centroid = NumVector3.Zero;
            foreach (var idx in indices)
            {
                centroid += ToNum(verts[idx]);
            }
            centroid /= indices.Length;

            // Check if normal points outward: dot(normal, centroid) > 0
            var dot = NumVector3.Dot(normal, centroid);

            if (dot < 0)
            {
                result.InwardNormals.Add($"Face {f} (dot={dot:F3})");
            }

            // If face has a pre-computed normal, check winding consistency
            if (face.Normal.HasValue && face.Normal.Value.LengthSquared() > 0.001f)
            {
                var providedNormal = NumVector3.Normalize(ToNum(face.Normal.Value));
                var dotWithProvided = NumVector3.Dot(normal, providedNormal);
                if (dotWithProvided < -0.5f)
                {
                    result.IncorrectWindings.Add($"Face {f} (computed vs provided normal mismatch)");
                }
            }

            // Check edges in face boundary
            for (int i = 0; i < indices.Length; i++)
            {
                var a = indices[i];
                var b = indices[(i + 1) % indices.Length];
                var min = Math.Min(a, b);
                var max = Math.Max(a, b);
                if (!edgeSet.Contains((min, max)))
                {
                    result.MissingEdges.Add($"Face {f}: edge ({a},{b})");
                }
            }

            // Check non-planar quads (and higher polygons)
            if (indices.Length == 4)
            {
                var v3 = ToNum(verts[indices[3]]);
                // Check if v3 lies on the plane defined by v0, v1, v2
                var v0v3 = v3 - v0;
                var cross = NumVector3.Cross(edge1, edge2);
                var dist = Math.Abs(NumVector3.Dot(cross, v0v3));
                // Allow small tolerance (scaled by model size)
                var tolerance = 0.5f; // in model space units
                if (dist > tolerance)
                {
                    result.NonPlanarQuads.Add($"Face {f} (deviation={dist:F2})");
                }
            }
            else if (indices.Length > 4)
            {
                // Check all vertices against the plane of first 3
                var planeNormal = NumVector3.Cross(edge1, edge2);
                var planeNormalLen = planeNormal.Length();
                if (planeNormalLen > 0.001f)
                {
                    planeNormal /= planeNormalLen;
                    for (int i = 3; i < indices.Length; i++)
                    {
                        var vi = ToNum(verts[indices[i]]);
                        var viVec = vi - v0;
                        var dist = Math.Abs(NumVector3.Dot(planeNormal, viVec));
                        if (dist > 0.5f)
                        {
                            result.NonPlanarQuads.Add($"Face {f} (vertex {i} deviation={dist:F2})");
                            break; // Report once per face
                        }
                    }
                }
            }
        }

        return result;
    }

    static NumVector3 ToNum(Vertex3 v) => new NumVector3(v.X, v.Y, v.Z);
    static NumVector3 ToNum(XnaVector3 v) => new NumVector3(v.X, v.Y, v.Z);

    class ModelResult
    {
        public string Name { get; set; } = "";
        public int VertexCount { get; set; }
        public int EdgeCount { get; set; }
        public int FaceCount { get; set; }
        public List<string> InwardNormals { get; set; } = new();
        public List<string> IncorrectWindings { get; set; } = new();
        public List<string> MissingEdges { get; set; } = new();
        public List<string> NonPlanarQuads { get; set; } = new();
    }
}
