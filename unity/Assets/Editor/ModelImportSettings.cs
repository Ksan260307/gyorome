using UnityEditor;

namespace Gyorome.EditorTools
{
    /// <summary>Blender から出力した FBX（Assets/Art/Models）の取り込み設定。頂点カラーと UV をそのまま使う。</summary>
    public class ModelImportSettings : AssetPostprocessor
    {
        const string ModelFolder = "Assets/Art/Models";

        // 取り込み設定を変えたら、数字を上げる（モデルが取り込みなおされる）
        public override uint GetVersion() => 2;

        void OnPreprocessModel()
        {
            if (!assetPath.StartsWith(ModelFolder)) return;
            var mi = (ModelImporter)assetImporter;
            mi.globalScale = 1f;
            mi.useFileScale = true;
            mi.isReadable = true;
            mi.importNormals = ModelImporterNormals.Import;
            mi.importTangents = ModelImporterTangents.None;
            mi.materialImportMode = ModelImporterMaterialImportMode.None;
            mi.importAnimation = false;
            mi.animationType = ModelImporterAnimationType.None;
            mi.importCameras = false;
            mi.importLights = false;
            mi.importBlendShapes = false;
            mi.importVisibility = false;
            mi.meshCompression = ModelImporterMeshCompression.Off;
            mi.generateSecondaryUV = false;
            mi.weldVertices = true;
            mi.indexFormat = ModelImporterIndexFormat.Auto;
            mi.generateMeshLods = false;
        }
    }
}
