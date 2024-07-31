using GLTF.Schema;
using GLTFast.Schema;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Scripts
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
