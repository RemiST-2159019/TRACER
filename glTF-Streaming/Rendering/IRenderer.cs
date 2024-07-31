using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Scripts.Rendering
{
    /// <summary>
    /// An IRenderer provides an interface to render binary glTF (GLB) scenes using any importer.
    /// </summary>
    public interface IRenderer
    {
        Task RenderAsync(byte[] glbData);
    }
}
