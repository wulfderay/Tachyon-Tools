using Collada141;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace TachyonPak
{
    public static class _3DObjectConverter
    {
        public const double conversionFactor = 1.525879e-05; //taken from space.exe. They use it when outputting coords to logs.
        public static void ConvertToObj(_3DObject obj, string outputPath)
        {
            StringBuilder sb = new StringBuilder();

            // Write header
            sb.AppendLine("# Converted from _3DObject to OBJ");
            sb.AppendLine();

            
            // Write vertices
            foreach (Vertex vertex in obj.vertices)
            {
                sb.AppendLine($"v {vertex.coordinates[0] * conversionFactor} {vertex.coordinates[1] * conversionFactor} {vertex.coordinates[2] * conversionFactor }");
            }
            sb.AppendLine();

            // Write texture coordinates
            foreach (Vertex vertex in obj.vertices)
            {
                sb.AppendLine($"vt {vertex.texturecoordsmaybe[0] * conversionFactor} {vertex.texturecoordsmaybe[1] * conversionFactor}");
            }
            sb.AppendLine();

            // Write normals
            foreach (Normal normal in obj.normals_or_whatever)
            {
                sb.AppendLine($"vn {normal.coordinates[0]* conversionFactor} {normal.coordinates[1] * conversionFactor} {normal.coordinates[2] * conversionFactor}");
            }
            sb.AppendLine();

            var vertSize = Marshal.SizeOf<Vertex>(); // The 3do1 files index the verticies by their offset in the file, so we have to divide out the size to get the index.
            var normSize = Marshal.SizeOf<Normal>(); // similar with normals
            // Write faces (triangles)
            foreach (Triangle triangle in obj.tris)
            {
                int vertexIndex1 = (int)Math.Floor((decimal)triangle.vertexOffsets[0] / vertSize) + 1;
                int vertexIndex2 = (int)Math.Floor((decimal)triangle.vertexOffsets[1] / vertSize) + 1;
                int vertexIndex3 = (int)Math.Floor((decimal)triangle.vertexOffsets[2] / vertSize) + 1;
                int textureIndex1 = (int)Math.Floor((decimal)triangle.vertexOffsets[0] / vertSize) + 1;
                int textureIndex2 = (int)Math.Floor((decimal)triangle.vertexOffsets[1] / vertSize) + 1;
                int textureIndex3 = (int)Math.Floor((decimal)triangle.vertexOffsets[2] / vertSize) + 1;
                int normalIndex1 = (int)Math.Floor((decimal)triangle.NormalsOffsets[0] / normSize) + 1;
                int normalIndex2 = (int)Math.Floor((decimal)triangle.NormalsOffsets[1] / normSize) + 1;
                int normalIndex3 = (int)Math.Floor((decimal)triangle.NormalsOffsets[2] / normSize) + 1;

                sb.AppendLine($"f {vertexIndex1}/{textureIndex1}/{normalIndex1} {vertexIndex2}/{textureIndex2}/{normalIndex2} {vertexIndex3}/{textureIndex3}/{normalIndex3}");
            }

            // Write to file
            File.WriteAllText(outputPath, sb.ToString());
        }


        public static void ConvertToCOLLADA(_3DObject obj, string outputPath)
        {
            COLLADA collada = new COLLADA();

            // Populate the COLLADA classes with data from _3DObject
            // Convert _3DOHeader
            collada.asset = new asset();
            collada.asset.title = obj.header.Name;
            collada.asset.created = DateTime.Now;
            collada.asset.modified = DateTime.Now;
            collada.asset.unit = new assetUnit();
            collada.asset.unit.meter = 1;
            collada.asset.unit.name = "meter";
            collada.asset.up_axis = UpAxisType.Z_UP;

            collada.asset.contributor = new assetContributor[1];
            collada.asset.contributor[0] = new assetContributor { author = "TachyonPAK User", authoring_tool = "TachyonPAK" };
            // TODO Populate other asset properties...

            // Convert textures
            foreach (TextureEntry textureEntry in obj.textures)
            {
                // Create image and texture elements and populate them with textureEntry data
                // Add them to the appropriate COLLADA elements (e.g., <library_images>, <library_materials>, etc.)
                // Adjust the mapping according to your COLLADA class structure
            }
            

            // Convert vertices
            geometry geometry = new geometry { id = SanitizeFileName(obj.header.Name)+"-mesh", name = SanitizeFileName(obj.header.Name) };
            mesh mesh = new mesh();

            // Populate mesh with vertex data from obj.vertices...
            var postions = new float_array();
            postions.id = "mesh-positions-array";
            postions.Values = ConvertVertexArray(obj.vertices);
            postions.count =(ulong) postions.Values.Length;

            var normals = new float_array();
            normals.id = "mesh-normals-array";
            normals.Values = ConvertNormals(obj.normals_or_whatever);
            normals.count = (ulong)normals.Values.Length;

            var texcoords = new float_array();
            texcoords.id = "mesh-texture-coords-array";
            texcoords.Values = ConvertTexCoords(obj.vertices);
            texcoords.count = (ulong)texcoords.Values.Length;

            mesh.source = new source[3];
            mesh.source[0] = new source();
            mesh.source[0].id = "mesh-positions";
            mesh.source[0].Item = postions;
            mesh.source[0].technique_common = GetTechnique3d(mesh.source[0]);
            mesh.source[1] = new source();
            mesh.source[1].id = "mesh-normals";
            mesh.source[1].Item = normals;
            mesh.source[1].technique_common = GetTechnique3d(mesh.source[1]);
            mesh.source[2] = new source();
            mesh.source[2].id = "mesh-texture-coords";
            mesh.source[2].Item = texcoords;
            mesh.source[2].technique_common = GetTechnique2d(mesh.source[2]);
            mesh.vertices = new vertices();
            mesh.vertices.id = "mesh-vertices";
            mesh.vertices.input = new InputLocal[1];
            mesh.vertices.input[0] = new InputLocal { semantic = "POSITION", source = "#mesh-positions" };

            geometry.Item = mesh;

            // Convert triangles
            triangles triangles = new triangles();
            triangles.input = new InputLocalOffset[3];
            triangles.input[0] = new InputLocalOffset { semantic = "VERTEX", source = "#mesh-vertices", offset = 0 };
            triangles.input[1] = new InputLocalOffset { semantic = "NORMAL", source = "#mesh-normals", offset = 1 };
            triangles.input[2] = new InputLocalOffset { semantic = "TEXCOORD", source = "#mesh-texture-coords", offset = 2, set = 0 };
            triangles.p = ConvertTriangles(obj);
            triangles.count = (ulong)obj.tris.Length;
            // Populate triangles with face data from obj.tris...
            mesh.Items = new object[] { triangles };


            var library_scene = new library_visual_scenes();
            library_scene.visual_scene = new visual_scene[] {
                new visual_scene {
                    id = "Scene", 
                    name = "Scene",
                    node = new node[] {
                        new node {
                            id= SanitizeFileName(obj.header.Name),
                            name = SanitizeFileName(obj.header.Name),
                            type = NodeType.NODE,
                            instance_geometry = new instance_geometry[]
                            {
                                new instance_geometry
                                {
                                    url="#"+geometry.id,
                                    name = geometry.name
                                }
                            }
                            
                        }
                    }
                } 
            };

            collada.scene = new COLLADAScene
            {
                instance_visual_scene = new InstanceWithExtra { url ="#Scene"}
            };
            

            // Add the geometry to the COLLADA document
            collada.Items = new object[] { new library_geometries() { geometry = new geometry[] { geometry } } , library_scene };

            // add the mesh to the scene.

            

            // Serialize the COLLADA document to an XML file
            collada.Save(outputPath);
        }

        private static string ConvertTriangles(_3DObject obj)
        {
            var vertSize = Marshal.SizeOf<Vertex>(); // The 3do1 files index the verticies by their offset in the file, so we have to divide out the size to get the index.
            var normSize = Marshal.SizeOf<Normal>(); // similar with normals
            var floatarray = new List<float>();
            foreach(var tri in obj.tris)
            {
                floatarray.Add(tri.vertexOffsets[0] / vertSize); // vertex
                floatarray.Add(tri.NormalsOffsets[0] / normSize); // normal
                floatarray.Add(tri.vertexOffsets[0] / vertSize); // tex coord

                floatarray.Add(tri.vertexOffsets[1] / vertSize); // vertex
                floatarray.Add(tri.NormalsOffsets[1] / normSize); // normal
                floatarray.Add(tri.vertexOffsets[1] / vertSize); // tex coord

                floatarray.Add(tri.vertexOffsets[2] / vertSize); // vertex
                floatarray.Add(tri.NormalsOffsets[2] / normSize); // normal
                floatarray.Add(tri.vertexOffsets[2] / vertSize); // tex coord


            }
            return string.Join(" ", floatarray);
        }

        private static sourceTechnique_common GetTechnique3d(source source)
        {
            var floatArray = ((float_array)(source.Item));
            sourceTechnique_common sourceTechnique = new sourceTechnique_common();
            var accessor_params = new param[3];
            accessor_params[0] = new param { name = "X", type = "float" };
            accessor_params[1] = new param { name = "Y", type = "float" };
            accessor_params[2] = new param { name = "Z", type = "float" };
            sourceTechnique.accessor = new accessor { source = "#" + floatArray.id, count = floatArray.count / 3, stride = 3, param = accessor_params };
            return sourceTechnique;
        }

        private static sourceTechnique_common GetTechnique2d(source source)
        {
            var floatArray = ((float_array)(source.Item));
            sourceTechnique_common sourceTechnique = new sourceTechnique_common();
            var accessor_params = new param[2];
            accessor_params[0] = new param { name = "S", type = "float" };
            accessor_params[1] = new param { name = "T", type = "float" };
            sourceTechnique.accessor = new accessor { source = "#"+ floatArray.id, count = floatArray.count/3, stride = 2, param = accessor_params};
            return sourceTechnique;
        }

        private static double[] ConvertTexCoords(Vertex[] vertices)
        {
            var doubles = new List<double>();

            foreach (var vertex in vertices)
            {
                doubles.Add(vertex.texturecoordsmaybe[0] * conversionFactor);
                doubles.Add(vertex.texturecoordsmaybe[1] * conversionFactor);
            }

            return doubles.ToArray();
        }

        static double[] ConvertVertexArray(Vertex[] vertices)
        {
            var doubles = new List<double>();

            foreach(var vertex in vertices)
            {
                doubles.Add(vertex.coordinates[0] * conversionFactor);
                doubles.Add(vertex.coordinates[1] * conversionFactor);
                doubles.Add(vertex.coordinates[2] * conversionFactor);
            }

            return doubles.ToArray();
        }

        static double[] ConvertNormals(Normal [] normals)
        {
            var doubles = new List<double>();

            foreach (var vertex in normals)
            {
                doubles.Add(vertex.coordinates[0] * conversionFactor);
                doubles.Add(vertex.coordinates[1] * conversionFactor);
                doubles.Add(vertex.coordinates[2] * conversionFactor);
            }

            return doubles.ToArray();
        }

        public static string SanitizeFileName(string fileName)
        {
            // Remove any characters that are not allowed in directory names
            string sanitized = Regex.Replace(fileName, @"[^\w\-\.]", "");

            // Remove any leading or trailing periods or spaces
            sanitized = sanitized.Trim('.', ' ');
            return sanitized;
        }
    }
}
