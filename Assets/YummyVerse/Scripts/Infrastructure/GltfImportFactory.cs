using GLTFast;
using GLTFast.Materials;

namespace YummyVerse.Scripts.Infrastructure
{
    public static class GltfImportFactory
    {
        public static GltfImport Create()
        {
            return new GltfImport(materialGenerator: MaterialGenerator.GetDefaultMaterialGenerator());
        }
    }
}
