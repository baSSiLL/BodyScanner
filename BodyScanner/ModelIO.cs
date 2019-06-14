// Based on code from KinectFusionHelper.cs, copyright (c) Microsoft Corporation
using System;
using System.Globalization;
using System.IO;
using Microsoft.Kinect.Fusion;

namespace BodyScanner
{
    /// <summary>
    /// A helper class for common operations.
    /// </summary>
    public static class ModelIO
    {
        /// <summary>
        /// Save mesh in binary .STL file
        /// </summary>
        /// <param name="mesh">Calculated mesh object</param>
        /// <param name="writer">Binary file writer</param>
        /// <param name="flipAxes">Flag to determine whether the Y and Z values are flipped on save,
        /// default should be true.</param>
        public static void SaveBinaryStlMesh(Mesh mesh, BinaryWriter writer, bool flipAxes)
        {
            if (null == mesh || null == writer)
            {
                return;
            }

            var vertices = mesh.GetVertices();
            var normals = mesh.GetNormals();
            var indices = mesh.GetTriangleIndexes();

            // Check mesh arguments
            if (0 == vertices.Count || 0 != vertices.Count % 3 || vertices.Count != indices.Count)
            {
                throw new ArgumentException(Properties.Resources.InvalidMeshArgument);
            }

            char[] header = new char[80];
            writer.Write(header);

            // Write number of triangles
            int triangles = vertices.Count / 3;
            writer.Write(triangles);

            // Sequentially write the normal, 3 vertices of the triangle and attribute, for each triangle
            for (int i = 0; i < triangles; i++)
            {
                // Write normal
                var normal = normals[i * 3];
                writer.Write(normal.X);
                writer.Write(flipAxes ? -normal.Y : normal.Y);
                writer.Write(flipAxes ? -normal.Z : normal.Z);

                // Write vertices
                for (int j = 0; j < 3; j++)
                {
                    var vertex = vertices[(i * 3) + j];
                    writer.Write(vertex.X);
                    writer.Write(flipAxes ? -vertex.Y : vertex.Y);
                    writer.Write(flipAxes ? -vertex.Z : vertex.Z);
                }

                ushort attribute = 0;
                writer.Write(attribute);
            }
        }

        /// <summary>
        /// Save mesh in ASCII WaveFront .OBJ file
        /// </summary>
        /// <param name="mesh">Calculated mesh object</param>
        /// <param name="writer">The text writer</param>
        /// <param name="flipAxes">Flag to determine whether the Y and Z values are flipped on save,
        /// default should be true.</param>
        public static void SaveAsciiObjMesh(Mesh mesh, TextWriter writer, bool flipAxes)
        {
            if (null == mesh || null == writer)
            {
                return;
            }

            var vertices = mesh.GetVertices();
            var normals = mesh.GetNormals();
            var indices = mesh.GetTriangleIndexes();

            // Check mesh arguments
            if (0 == vertices.Count || 0 != vertices.Count % 3 || vertices.Count != indices.Count)
            {
                throw new ArgumentException(Properties.Resources.InvalidMeshArgument);
            }

            // Write the header lines
            writer.WriteLine("#");
            writer.WriteLine("# OBJ file created by Microsoft Kinect Fusion");
            writer.WriteLine("#");

            // Sequentially write the 3 vertices of the triangle, for each triangle
            for (int i = 0; i < vertices.Count; i++)
            {
                var vertex = vertices[i];

                string vertexString = "v " + vertex.X.ToString(CultureInfo.InvariantCulture) + " ";

                if (flipAxes)
                {
                    vertexString += (-vertex.Y).ToString(CultureInfo.InvariantCulture) + " " + (-vertex.Z).ToString(CultureInfo.InvariantCulture);
                }
                else
                {
                    vertexString += vertex.Y.ToString(CultureInfo.InvariantCulture) + " " + vertex.Z.ToString(CultureInfo.InvariantCulture);
                }

                writer.WriteLine(vertexString);
            }

            // Sequentially write the 3 normals of the triangle, for each triangle
            for (int i = 0; i < normals.Count; i++)
            {
                var normal = normals[i];

                string normalString = "vn " + normal.X.ToString(CultureInfo.InvariantCulture) + " ";

                if (flipAxes)
                {
                    normalString += (-normal.Y).ToString(CultureInfo.InvariantCulture) + " " + (-normal.Z).ToString(CultureInfo.InvariantCulture);
                }
                else
                {
                    normalString += normal.Y.ToString(CultureInfo.InvariantCulture) + " " + normal.Z.ToString(CultureInfo.InvariantCulture);
                }

                writer.WriteLine(normalString);
            }

            // Sequentially write the 3 vertex indices of the triangle face, for each triangle
            // Note this is typically 1-indexed in an OBJ file when using absolute referencing!
            for (int i = 0; i < vertices.Count / 3; i++)
            {
                string baseIndex0 = ((i * 3) + 1).ToString(CultureInfo.InvariantCulture);
                string baseIndex1 = ((i * 3) + 2).ToString(CultureInfo.InvariantCulture);
                string baseIndex2 = ((i * 3) + 3).ToString(CultureInfo.InvariantCulture);

                string faceString = "f " + baseIndex0 + "//" + baseIndex0 + " " + baseIndex1 + "//" + baseIndex1 + " " + baseIndex2 + "//" + baseIndex2;
                writer.WriteLine(faceString);
            }
        }

        /// <summary>
        /// Save mesh in ASCII .PLY file with per-vertex color
        /// </summary>
        /// <param name="mesh">Calculated mesh object</param>
        /// <param name="writer">The text writer</param>
        /// <param name="flipAxes">Flag to determine whether the Y and Z values are flipped on save,
        /// default should be true.</param>
        public static void SaveAsciiPlyMesh(Mesh mesh, TextWriter writer, bool flipAxes)
        {
            if (null == mesh || null == writer)
            {
                return;
            }

            var vertices = mesh.GetVertices();
            var indices = mesh.GetTriangleIndexes();

            // Check mesh arguments
            if (0 == vertices.Count || 0 != vertices.Count % 3 || vertices.Count != indices.Count)
            {
                throw new ArgumentException(Properties.Resources.InvalidMeshArgument);
            }

            int faces = indices.Count / 3;

            // Write the PLY header lines
            writer.WriteLine("ply");
            writer.WriteLine("format ascii 1.0");
            writer.WriteLine("comment file created by Microsoft Kinect Fusion");

            writer.WriteLine("element vertex " + vertices.Count.ToString(CultureInfo.InvariantCulture));
            writer.WriteLine("property float x");
            writer.WriteLine("property float y");
            writer.WriteLine("property float z");

            writer.WriteLine("element face " + faces.ToString(CultureInfo.InvariantCulture));
            writer.WriteLine("property list uchar int vertex_index");
            writer.WriteLine("end_header");

            // Sequentially write the 3 vertices of the triangle, for each triangle
            for (int i = 0; i < vertices.Count; i++)
            {
                var vertex = vertices[i];

                string vertexString = vertex.X.ToString(CultureInfo.InvariantCulture) + " ";

                if (flipAxes)
                {
                    vertexString += (-vertex.Y).ToString(CultureInfo.InvariantCulture) + " " + (-vertex.Z).ToString(CultureInfo.InvariantCulture);
                }
                else
                {
                    vertexString += vertex.Y.ToString(CultureInfo.InvariantCulture) + " " + vertex.Z.ToString(CultureInfo.InvariantCulture);
                }

                writer.WriteLine(vertexString);
            }

            // Sequentially write the 3 vertex indices of the triangle face, for each triangle, 0-referenced in PLY files
            for (int i = 0; i < faces; i++)
            {
                string baseIndex0 = (i * 3).ToString(CultureInfo.InvariantCulture);
                string baseIndex1 = ((i * 3) + 1).ToString(CultureInfo.InvariantCulture);
                string baseIndex2 = ((i * 3) + 2).ToString(CultureInfo.InvariantCulture);

                string faceString = "3 " + baseIndex0 + " " + baseIndex1 + " " + baseIndex2;
                writer.WriteLine(faceString);
            }
        }
    }
}