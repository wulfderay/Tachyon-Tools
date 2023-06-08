using System;
using System.IO;
using System.Text;

namespace TachyonPak
{
    public static class _3DObjectConverter
    {
        public static void ConvertToObj(_3DObject obj, string outputPath)
        {
            StringBuilder sb = new StringBuilder();

            // Write header
            sb.AppendLine("# Converted from _3DObject to OBJ");
            sb.AppendLine();

            double conversionFactor = 1.525879e-05; //taken from space.exe
            int otherConversionFactor = 32764;
            // Write vertices
            foreach (Vertex vertex in obj.vertices)
            {
                sb.AppendLine($"v {vertex.coordinates[0] * conversionFactor} {vertex.coordinates[1] * conversionFactor} {vertex.coordinates[2] * conversionFactor }");
            }
            sb.AppendLine();

            // Write texture coordinates
            foreach (Vertex vertex in obj.vertices)
            {
                sb.AppendLine($"vt {vertex.texturecoordsmaybe[0] / otherConversionFactor} {vertex.texturecoordsmaybe[1] / otherConversionFactor}");
            }
            sb.AppendLine();

            // Write normals
            foreach (Normal normal in obj.normals_or_whatever)
            {
                sb.AppendLine($"vn {normal.coordinates[0]/ otherConversionFactor} {normal.coordinates[1] / otherConversionFactor} {normal.coordinates[2] / otherConversionFactor}");
            }
            sb.AppendLine();

            // Write faces (triangles)
            foreach (Triangle triangle in obj.tris)
            {
                int vertexIndex1 = (int)Math.Floor((decimal)triangle.vertexOffsets[0] / 44) + 1;
                int vertexIndex2 = (int)Math.Floor((decimal)triangle.vertexOffsets[1] / 44) + 1;
                int vertexIndex3 = (int)Math.Floor((decimal)triangle.vertexOffsets[2] / 44) + 1;
                int textureIndex1 = (int)Math.Floor((decimal)triangle.NormalsOffsets[0] / 16) + 1;
                int textureIndex2 = (int)Math.Floor((decimal)triangle.NormalsOffsets[1] / 16) + 1;
                int textureIndex3 = (int)Math.Floor((decimal)triangle.NormalsOffsets[2] / 16) + 1;
                int normalIndex1 = (int)Math.Floor((decimal)triangle.NormalsOffsets[0] / 16) + 1;
                int normalIndex2 = (int)Math.Floor((decimal)triangle.NormalsOffsets[1] / 16) + 1;
                int normalIndex3 = (int)Math.Floor((decimal)triangle.NormalsOffsets[2] / 16) + 1;

                sb.AppendLine($"f {vertexIndex1} {vertexIndex2} {vertexIndex3}");
            }

            // Write to file
            File.WriteAllText(outputPath, sb.ToString());
        }
    }
}
