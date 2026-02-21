using GLTFast;
using GLTFast.Materials;

namespace YummyVerse.Scripts.Model
{
    public static class GltfImportFactory
    {
        public static GltfImport Create()
        {
            return new GltfImport(materialGenerator: MaterialGenerator.GetDefaultMaterialGenerator());
        }
    }
}
