using GLTFast;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.Rendering
{
    public class GLTFastRenderer : IRenderer
    {
        private Transform _transform;
        public GLTFastRenderer(Transform rootTransform)
        {
            _transform = rootTransform;
        }

        public async Task RenderAsync(byte[] glbData)
        {
            var gltf = new GltfImport();

            bool success = await gltf.LoadGltfBinary(glbData);
            if (success)
                success = await gltf.InstantiateMainSceneAsync(_transform);
        }
    }
}

