using GLTF.Schema;
using System.IO;
using System.Threading.Tasks;

namespace tracer
{
    public class JsonUtils
    {
        public static async Task<GLTFRoot> DeserializeAsync(string json)
        {
            return await Task.Run(() =>
            {
                using (TextReader sr = new StringReader(json))
                {
                    return GLTFRoot.Deserialize(sr);
                }
            });
        }

        public static async Task<string> SerializeAsync(GLTFRoot gltfRoot)
        {
            return await Task.Run(() =>
            {
                using (TextWriter tr = new StringWriter())
                {
                    gltfRoot.Serialize(tr, true);
                    return tr.ToString();
                }
            });
        }
    }
}
