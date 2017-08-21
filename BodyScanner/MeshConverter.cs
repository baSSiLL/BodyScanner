using Microsoft.Kinect.Fusion;
using System.Diagnostics.Contracts;
using System.Windows.Media.Media3D;

namespace BodyScanner
{
    static class MeshConverter
    {
        public static MeshGeometry3D Convert(Mesh mesh)
        {
            Contract.Requires(mesh != null);
            Contract.Ensures(Contract.Result<MeshGeometry3D>() != null);

            var result = new MeshGeometry3D();

            var vertices = mesh.GetVertices();
            foreach (var v in vertices)
            {
                result.Positions.Add(ConvertToPoint(v));
            }

            var normals = mesh.GetNormals();
            foreach (var normal in normals)
            {
                result.Normals.Add(ConvertToVector(normal));
            }

            var triangles = mesh.GetTriangleIndexes();
            foreach (var index in triangles)
            {
                result.TriangleIndices.Add(index);
            }

            result.Freeze();
            return result;
        }

        private static Point3D ConvertToPoint(Vector3 v)
        {
            return new Point3D(v.X, v.Y, v.Z);
        }

        private static Vector3D ConvertToVector(Vector3 v)
        {
            return new Vector3D(v.X, v.Y, v.Z);
        }
    }
}
