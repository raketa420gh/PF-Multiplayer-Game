using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace Game.Scripts.Editor.Dungeon
{
    /// Detail pieces of a catacomb room (written by CatacombDetailBuilder) become its combined meshes on import: the repository
    /// keeps the half-megabyte list of pieces, not hundreds of megabytes of mesh data. Bump the version when the meshing changes.
    [ScriptedImporter(2, Extension)]
    internal sealed class CatacombDetailImporter : ScriptedImporter
    {
        public const string Extension = "catdetail";

        public override void OnImportAsset(AssetImportContext ctx)
        {
            List<Mesh> meshes = CatacombDetailBuilder.Meshes(File.ReadAllBytes(ctx.assetPath));

            foreach (Mesh mesh in meshes)
            {
                // Drawn only: no CPU copy kept in the game.
                SerializedObject serialized = new SerializedObject(mesh);
                serialized.FindProperty("m_IsReadable").boolValue = false;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                ctx.AddObjectToAsset(mesh.name, mesh);
            }

            // The main object takes the file name: an empty one, the chunks keep theirs.
            Mesh main = new Mesh();
            ctx.AddObjectToAsset("Main", main);
            ctx.SetMainObject(main);
        }
    }
}
